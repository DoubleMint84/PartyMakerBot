using PartyMakerBot.Model;
using ReactiveUI;

namespace PartyMakerBot.UI.ViewModels;

public class QueueItemViewModel(QueueItem model) : ViewModelBase
{
    public int Index { get; } = model.Index;
    public string Url { get; } = model.Url;
    public string OwnerDisplayName { get; } = model.Owner.DisplayName;
    public string AddedAt { get; } = model.AddedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public string FilePath { get; } = model.FilePath ?? "No";
}