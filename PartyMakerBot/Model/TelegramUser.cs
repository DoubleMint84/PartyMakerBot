namespace PartyMakerBot.Model;

public class TelegramUser(long id, string? username, string displayName)
{
    public long Id { get; } = id;
    public string? Username { get; } = username;
    public string DisplayName { get; } = displayName;

    public static TelegramUser FromTelegramBotUser(Telegram.Bot.Types.User? user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var display = !string.IsNullOrWhiteSpace(user.Username)
            ? "@" + user.Username
            : $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(display)) display = $"user{user.Id}";
        return new TelegramUser(user.Id, user.Username, display);
    }
}