using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using PartyMakerBot.Model;
using PartyMakerBot.Service;
using ReactiveUI;

namespace PartyMakerBot.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly QueueManager _queueManager;

    public ObservableCollection<QueueItemViewModel> QueueItems { get; } = new();

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    public MainWindowViewModel(QueueManager queueManager)
    {
        _queueManager = queueManager;
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshQueueAsync);
        
        _queueManager.QueueChanged += async () => await RefreshQueueAsync();

        // Начальная загрузка
        _ = RefreshQueueAsync();
    }

    private Task RefreshQueueAsync()
    {
        // Обновление коллекции делаем строго в UI-потоке
        return Dispatcher.UIThread.InvokeAsync(() =>
        {
            QueueItems.Clear();
            var snapshot = _queueManager.GetSnapshot();
            foreach (var item in snapshot)
            {
                QueueItems.Add(new QueueItemViewModel(item));
            }
        }).GetTask();
    }
}