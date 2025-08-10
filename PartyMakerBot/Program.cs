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
            
            var botClient = new TelegramBotClient(token);
            var queueManager = new QueueManager();
            var commandHandler = new CommandHandler(botClient, queueManager);
            var botService = new BotService(botClient, commandHandler);

            Console.WriteLine("Запуск бота...");
            await botService.StartAsync();
        }
    }
}