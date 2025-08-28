using System.Collections.Concurrent;
using PartyMakerBot.Data;
using PartyMakerBot.Model;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace PartyMakerBot.Service;

public class DownloadService
{
    private readonly QueueManager _queueManager;
    private readonly AppDbContext _db;
    private readonly ConcurrentDictionary<string, DownloadedUrl> _cache = new(); // url -> file path
    private readonly SemaphoreSlim _signal = new(0);
    private readonly object _snapshotLock = new();

    private readonly CancellationTokenSource _cts = new();

    public DownloadService(QueueManager queueManager, AppDbContext dbContext)
    {
        _queueManager = queueManager;
        _queueManager.ItemEnqueued += () => _signal.Release();
        
        _db = dbContext;
        EnsureDatabaseAndLoadCache();
    }

    private void EnsureDatabaseAndLoadCache()
    {
        lock (_snapshotLock)
        {
            _db.Database.EnsureCreated();
        
            var urls = _db.DownloadedUrls.ToList();
            
            foreach (var item in urls)
                if (item.LocalPath != null) _cache[item.Url] = item;
        }
    }

    public void Start()
    {
        Task.Run(ProcessLoopAsync);
    }

    private async Task ProcessLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            await _signal.WaitAsync(_cts.Token);

            var snapshot = _queueManager.GetSnapshot();
            foreach (var item in snapshot.Where(i => !i.IsDownloaded))
            {
                if (_cache.TryGetValue(item.Url, out var downloadedUrl))
                {
                    item.DownloadedUrl = downloadedUrl;
                }
                else
                {
                    try
                    {
                        var path = await DownloadFileAsync(item.Url);
                        if (path != null)
                        {
                            var url = new DownloadedUrl
                            {
                                LocalPath = path,
                                DownloadedAt = DateTimeOffset.Now,
                                Url = item.Url,
                            };

                            lock (_snapshotLock)
                            {
                                _db.DownloadedUrls.Add(url);
                                _db.SaveChanges();
                            }
                            
                            item.DownloadedUrl = url;
                            _cache[item.Url] = url;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Ошибка скачивания {item.Url}: {ex.Message}");
                    }
                }
            }
        }
    }

    private async Task<string?> DownloadFileAsync(string url)
    {
        var ytdl = new YoutubeDL();

        ytdl.YoutubeDLPath = "/opt/homebrew/bin/yt-dlp";
        ytdl.FFmpegPath = "/opt/homebrew/bin/ffmpeg";

        var dir = Path.Combine(AppContext.BaseDirectory, "downloads");
        Directory.CreateDirectory(dir);
        ytdl.OutputFolder = dir;

        var res = await ytdl.RunAudioDownload(
            url,
            AudioConversionFormat.Mp3
        );

        if (!res.Success)
        {
            Console.Error.WriteLine("yt-dlp failed to download. Error output:");
            foreach (var line in res.ErrorOutput)
            {
                Console.Error.WriteLine(line);
            }
            
            return null;
        }
        
        string path = res.Data;
        
        Console.WriteLine($"Файл {url} скачан в {path}");
        return path;
    }

    public void Stop() => _cts.Cancel();
}