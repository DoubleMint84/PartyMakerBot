using System.Diagnostics;
using System.Globalization;
using PartyMakerBot.Model;

namespace PartyMakerBot.Service;

public class PlayerService
{
    QueueManager _queueManager;
    private readonly SemaphoreSlim _signal = new(0);

    private readonly CancellationTokenSource _cts = new();

    public PlayerService(QueueManager manager)
    {
        _queueManager = manager;
    }
    
    public void Start()
    {
        Task.Run(ProcessLoopAsync);
    }

    private async Task ProcessLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("Trying to find available element...");
            if (_queueManager.GetNext(out var queueItem))
            {
                Console.WriteLine($"Playing {Path.GetFileName(queueItem.FilePath)}");
                using (Process process = new())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "vlc",
                        ArgumentList = { "--play-and-exit", queueItem.FilePath! },
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };
                    process.EnableRaisingEvents = true; // Для корректной работы события Exited
                    process.Exited += (o, e) => Console.WriteLine("vlc exited");
                    process.Start();
    
                    // Чтение потоков асинхронно
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
    
                    process.WaitForExit();
                    Console.WriteLine($"""
                                       Process exited with code {process.ExitCode}.
                                       Output: {output}
                                       Error: {error}
                                       """);
                }
            }
            else
            {
                await Task.Delay(5000, _cts.Token);
            }
        }
    }

    public void Stop()
    {
        _cts.Cancel();
    }
    
}