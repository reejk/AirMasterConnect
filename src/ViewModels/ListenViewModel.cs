using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AirMaster7pConnect.ViewModels;

public class ListenViewModel : BaseViewModel, IAsyncDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pendingTask;
    
    public ObservableCollection<DataViewModel> Datas { get; } = new();
    
    public ValueTask DisposeAsync()
    {
        _cancellationTokenSource?.Cancel();
        return new ValueTask(_pendingTask ?? Task.CompletedTask);
    }
    
    public async Task ReceiveAsync()
    {
        using var cts = new CancellationTokenSource();
        _cancellationTokenSource = cts;
        
        using var udpClient = new UdpClient();
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 12414));

        try
        {
            while (!cts.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(cts.Token);
                if (result.Buffer.Length < 5)
                    continue;

                if (BitConverter.ToInt32(result.Buffer.AsSpan(0, 4)) != 0x03000000)
                    continue;

                if (result.Buffer[4] != result.Buffer.Length - 5 || result.Buffer[4] != 0x21)
                    continue;

                var data = Datas.FirstOrDefault(d => Equals(d.Address, result.RemoteEndPoint.Address));
                if (data == null)
                    Datas.Add(data = new DataViewModel(result.RemoteEndPoint.Address));

                data.Update(result.Buffer.AsSpan(23));
            }
        }
        catch (OperationCanceledException)
        {
        }

        _cancellationTokenSource = null;
    }

    public void StartReceive()
    {
        _pendingTask = ReceiveAsync();
    }
}