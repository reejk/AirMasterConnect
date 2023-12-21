using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SimpleWifi.Win32;

namespace AirMaster7pConnect.ViewModels;

public class MainViewModel : BaseViewModel
{
    private string _ssid = string.Empty;
    private string _bssid = string.Empty;
    private string _password = string.Empty;
    private object? _content;

    public string Ssid
    {
        get => _ssid;
        set => SetField(ref _ssid, value);
    }

    public string Bssid
    {
        get => _bssid;
        set => SetField(ref _bssid, value);
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public object? Content => _content;
    
    public ICommand UseCurrentConnectionCommand { get; }
    public ICommand ConnectCommand { get; }
    
    public MainViewModel()
    {
        UseCurrentConnectionCommand = new SimpleCommand(UseCurrentConnection);
        ConnectCommand = new SimpleCommand(Connect);
        
        UseCurrentConnection();

        var vm = new ListenViewModel();
        _content = vm;
        vm.StartReceive();
    }

    public ValueTask SetContentAsync(object? content)
    {
        var oldContent = _content;
        if (SetField(ref _content, content, nameof(Content)))
            return (oldContent as IAsyncDisposable)?.DisposeAsync() ?? ValueTask.CompletedTask;

        return ValueTask.CompletedTask;
    }
    
    public async void Connect()
    {
        if (!PhysicalAddress.TryParse(Bssid.Replace(':', '-'), out var bssid))
        {
            await SetContentAsync("BSSID format invalid (example: 01:02:03:04:05:06)");
            return;
        }
        
        var wifiInterface = GetWifiInterface();
        if (wifiInterface == null)
        {
            await SetContentAsync("Cannot find any available WiFi adapter");
            return;
        }
        
        var localAddress = wifiInterface.GetIPProperties()
            .UnicastAddresses
            .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(x => x.Address)
            .FirstOrDefault();
        
        if(localAddress == null)
        {
            await SetContentAsync($"Cannot find IPv4 address for WiFi interface: {wifiInterface.Name}");
            return;
        }

        var vm = new ConnectionViewModel(this);
        await SetContentAsync(vm);
        vm.StartConnect(Ssid, bssid, Password, localAddress);
    }

    public async void UseCurrentConnection()
    {
        var wifiInterface = GetWifiInterface();
        if (wifiInterface == null)
        {
            await SetContentAsync("Cannot find any available WiFi adapter");
            return;
        }
        
        var wlan = new WlanClient();
        foreach (var wifi in wlan.Interfaces)
        {
            if (wifi.NetworkInterface.Id != wifiInterface.Id)
                continue;

            var connection = wifi.CurrentConnection.wlanAssociationAttributes;
            int length = connection.dot11Ssid.SSID.AsSpan().IndexOf((byte)0);
            Ssid = Encoding.UTF8.GetString(connection.dot11Ssid.SSID, 0, length);
            Bssid = string.Join(":", connection.dot11Bssid.Select(x => $"{x:X2}"));
            if ((int)connection.dot11PhyType > 7)
                await SetContentAsync("AirMaster can't connect to 5GHz Wi-Fi.\nReconnect to 2.4Ghz or fill fields manually.");
        }

        if (Content is string)
            await SetContentAsync(null);
    }

    private static NetworkInterface? GetWifiInterface()
    {
        var adapters = NetworkInterface.GetAllNetworkInterfaces();
        return adapters.FirstOrDefault(x => x is { NetworkInterfaceType: NetworkInterfaceType.Wireless80211, OperationalStatus: OperationalStatus.Up, IsReceiveOnly: false });
    }
}