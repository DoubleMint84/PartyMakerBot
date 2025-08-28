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
    private readonly PlayerService _playerService;

    public ObservableCollection<QueueItemViewModel> QueueItems { get; } = new();
    private string _nowPlaying = "";

    public string NowPlaying
    {
        get => _nowPlaying;
        private set
        {
            string status = value != "" ? $"Now Playing: {value}" : "Ready To Play"; ;
            this.RaiseAndSetIfChanged(ref _nowPlaying, status);
        }
    }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<QueueItemViewModel, Unit> RemoveItemCommand { get; }

    public MainWindowViewModel(QueueManager queueManager, PlayerService playerService)
    {
        _queueManager = queueManager;
        _playerService = playerService;
        
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshQueueAsync);
        RemoveItemCommand = ReactiveCommand.Create<QueueItemViewModel>(RemoveItemAsync);
        
        _queueManager.QueueChanged += async () => await RefreshQueueAsync();
        _playerService.NowPlayingChanged += () => NowPlaying = _playerService.NowPlaying;

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

    private void RemoveItemAsync(QueueItemViewModel item)
    {
       _queueManager.TryRemoveByIndex(item.Index);
    }
}