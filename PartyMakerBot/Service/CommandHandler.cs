using PartyMakerBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PartyMakerBot.Service;

public class CommandHandler(ITelegramBotClient client, QueueManager queueManager)
{
    public async Task ProcessMessageAsync(Message message)
    {
        var chatId = message.Chat.Id;
        var user = TelegramUser.FromTelegramBotUser(message.From);

        var text = message.Text!.Trim();
        if (!text.StartsWith("/"))
        {
            await client.SendMessage(chatId, "Команды начинаются с '/'. Введите /help для списка команд.");
            return;
        }

        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var arg = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        switch (cmd)
        {
            case "/start":
                await client.SendMessage(chatId,
                    $"""
                     Привет, {user.DisplayName}!
                     Меня зовут PartyMakerBot, но пока что я умею только пушить ссылки в очередь...
                     Добавьте ссылку командой: /add https://example.com
                     Просмотреть очередь: /queue
                     Для справки: /help
                     """);
                break;

            case "/help":
                await client.SendMessage(chatId,
                    """
                    Команды:
                    /add <url> - добавить URL в очередь
                    /queue - показать очередь
                    /remove <index> - удалить свой элемент по индексу (если ваш)
                    /help - показать эту подсказку
                    """);
                break;

            case "/add":
                await HandleAdd(chatId, user, arg);
                break;

            case "/queue":
                await HandleQueue(chatId);
                break;

            case "/remove":
                await HandleRemove(chatId, user, arg);
                break;

            default:
                await client.SendMessage(chatId, $"Неизвестная команда {cmd}. Введите /help.");
                break;
        }
    }

    private async Task HandleAdd(long chatId, TelegramUser user, string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            await client.SendMessage(chatId, "Использование: /add <url>");
            return;
        }

        if (!Uri.TryCreate(arg, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            await client.SendMessage(chatId, "Неверный URL. Поддерживаются только http/https.");
            return;
        }

        var item = queueManager.Enqueue(uriResult.ToString(), user);
        var linkPreviewOptions = new LinkPreviewOptions { IsDisabled = true };
        await client.SendMessage(chatId, $"Добавлено в очередь под индексом #{item.Index} — {item.Url}", 
            linkPreviewOptions: linkPreviewOptions);
    }

    private async Task HandleQueue(long chatId)
    {
        var snapshot = queueManager.GetSnapshot();

        if (!snapshot.Any())
        {
            await client.SendMessage(chatId, "Очередь пуста.");
            return;
        }
        
        var lines = new List<string>();
        foreach (var item in snapshot)
        {
            var userNameEscaped = HtmlEscape(item.Owner.DisplayName);
            var addedAt = item.AddedAt.ToString("yyyy-MM-dd HH:mm:ss");
            lines.Add(
                $"<b>#{item.Index}</b> {addedAt}\n<a href=\"{item.Url}\">{Path.GetFileName(item.FilePath)}</a>\n— {userNameEscaped} (id {item.Owner.Id})");
        }

        var message = string.Join("\n\n", lines);
        var linkPreviewOptions = new LinkPreviewOptions { IsDisabled = true };
        await client.SendMessage(chatId, message, ParseMode.Html, linkPreviewOptions: linkPreviewOptions);
    }

    private async Task HandleRemove(long chatId, TelegramUser user, string arg)
    {
        if (string.IsNullOrWhiteSpace(arg) || !int.TryParse(arg, out var index))
        {
            await client.SendMessage(chatId, "Использование: /remove <index>");
            return;
        }

        var result = queueManager.TryRemoveByIndex(index, user);
        if (result.Success)
        {
            await client.SendMessage(chatId, $"Элемент #{index} удалён.");
        }
        else
        {
            await client.SendMessage(chatId, $"Не удалось удалить #{index}: {result.ErrorMessage}");
        }
    }

    private static string ShortenUrl(string url, int maxLen = 60)
    {
        if (url.Length <= maxLen) return url;
        return url.Substring(0, maxLen - 3) + "...";
    }
    
    private static string HtmlEscape(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}