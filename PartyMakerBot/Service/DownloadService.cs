using System.Collections.Concurrent;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace PartyMakerBot.Service;

public class DownloadService
{
    private readonly QueueManager _queueManager;
    private readonly ConcurrentDictionary<string, string> _cache = new(); // url -> file path
    private readonly SemaphoreSlim _signal = new(0);

    private readonly CancellationTokenSource _cts = new();

    public DownloadService(QueueManager queueManager)
    {
        _queueManager = queueManager;
        _queueManager.ItemEnqueued += () => _signal.Release();
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
                if (_cache.TryGetValue(item.Url, out var existingPath))
                {
                    item.MarkDownloaded(existingPath);
                }
                else
                {
                    try
                    {
                        var path = await DownloadFileAsync(item.Url);
                        if (path != null)
                        {
                            item.MarkDownloaded(path);
                            _cache[item.Url] = path;
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
            Console.Error.WriteLine(res.ErrorOutput);
            return null;
        }
        
        string path = res.Data;
        
        Console.WriteLine($"Файл {url} скачан в {path}");
        return path;
    }

    public void Stop() => _cts.Cancel();
}