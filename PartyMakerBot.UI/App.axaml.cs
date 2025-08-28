using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PartyMakerBot.Data;
using PartyMakerBot.Service;
using PartyMakerBot.UI.ViewModels;
using PartyMakerBot.UI.Views;
using Telegram.Bot;

namespace PartyMakerBot.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var token = Environment.GetEnvironmentVariable("BOT_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Установите переменную окружения BOT_TOKEN с токеном вашего бота.");
                Environment.Exit(-1);
            }
            
            var dbPath = "Data Source=queue.db"; 
            var db = new AppDbContext(dbPath);
            var queueManager = new QueueManager(db);
            var downloader = new DownloadService(queueManager, db);
            var player = new PlayerService(queueManager);
            
            var mainWindowViewModel = new MainWindowViewModel(queueManager, player);
            
            Console.WriteLine("Запуск бота...");
            
            Task.Run(async () =>
            {
                var botClient = new TelegramBotClient(token);
                var commandHandler = new CommandHandler(botClient, queueManager, player);
                var botService = new BotService(botClient, commandHandler);
                await botService.StartAsync();
            });
            
            Console.WriteLine("Запуск службы загрузки...");
            downloader.Start();
            
            Console.WriteLine("Запуск службы проигрывателя...");
            player.Start();

            Console.WriteLine("Инициализация завершена.");
            
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}