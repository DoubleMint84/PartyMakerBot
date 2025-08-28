using PartyMakerBot.Data;
using PartyMakerBot.Service;
using Telegram.Bot;

namespace PartyMakerBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var token = Environment.GetEnvironmentVariable("BOT_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Установите переменную окружения BOT_TOKEN с токеном вашего бота.");
                return;
            }
            
            var dbPath = "Data Source=queue.db";
            var db = new AppDbContext(dbPath);
            var queueManager = new QueueManager(db);
            var downloader = new DownloadService(queueManager, db);
            var player = new PlayerService(queueManager);
            
            Console.WriteLine("Запуск бота...");
            
            var botServiceTask = Task.Run(async () =>
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
            await botServiceTask;
        }
    }
}