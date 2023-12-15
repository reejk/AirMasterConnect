using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Sandwych.SmartConfig;
using Sandwych.SmartConfig.Airkiss;

namespace AirMaster7pConnect.ViewModels;

public class ConnectionViewModel : BaseViewModel, IAsyncDisposable
{
    private readonly MainViewModel _main;
    private bool _inProgress = true;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pendingTask;

    public bool InProgress
    {
        get => _inProgress;
        private set => SetField(ref _inProgress, value);
    }

    public ObservableCollection<string> Found { get; } = new();

    public ConnectionViewModel(MainViewModel main)
    {
        _main = main;
    }
    
    public async void Listen()
    {
        var vm = new ListenViewModel();
        await _main.SetContentAsync(vm);
        vm.StartReceive();
    }

    public ValueTask DisposeAsync()
    {
        _cancellationTokenSource?.Cancel();
        return new ValueTask(_pendingTask ?? Task.CompletedTask);
    }
    
    public async Task ConnectAsync(string ssid, PhysicalAddress bssid, string password, IPAddress localAddress)
    {
        using var cts = new CancellationTokenSource();
        _cancellationTokenSource = cts;
        
        var provider = new AirkissSmartConfigProvider();
        var ctx = provider.CreateContext();

        ctx.DeviceDiscoveredEvent += (s, e) =>
        {
            Found.Add($"{e.Device.IPAddress} ({e.Device.MacAddress})");
        };

        var scArgs = new SmartConfigArguments
        {
            Ssid = ssid,
            Bssid = bssid,
            Password = password,
            LocalAddress = localAddress
        };

        try
        {
            using var job = new SmartConfigJob(TimeSpan.FromMinutes(1));
            await job.ExecuteAsync(ctx, scArgs, cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        InProgress = false;
        _cancellationTokenSource = null;
    }

    public void StartConnect(string ssid, PhysicalAddress bssid, string password, IPAddress localAddress)
    {
        _pendingTask = Task.Run(async () => await ConnectAsync(ssid, bssid, password, localAddress));
    }
}