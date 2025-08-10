using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PartyMakerBot.Service;

public class BotService
{
    private readonly ITelegramBotClient _client;
    private readonly CommandHandler _handler;

    public BotService(ITelegramBotClient client, CommandHandler handler)
    {
        _client = client;
        _handler = handler;
    }
    
    public async Task StartAsync()
    {
        using var cts = new CancellationTokenSource();

        var me = await _client.GetMe();
        Console.WriteLine($"Бот запущен: @{me.Username} (id={me.Id})");
        
        _client.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [] }, 
            cancellationToken: cts.Token
        );

        Console.WriteLine("Нажмите Enter для остановки...");
        Console.ReadLine();
        cts.Cancel();
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
            {
                await _handler.ProcessMessageAsync(update.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в обработчике обновлений: {ex}");
        }
    }
    
    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken ct)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiEx => $"Telegram API Error:\n[{apiEx.ErrorCode}] {apiEx.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}