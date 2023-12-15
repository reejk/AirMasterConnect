using System;
using System.Net;

namespace AirMaster7pConnect.ViewModels;

public class DataViewModel : BaseViewModel
{
    private DateTime _lastUpdate;
    private int _pm2d5;
    private int _pm10;
    private float _hcho;
    private float _tvoc;
    private int _co2;
    private float _temperature;
    private float _relativeHumidity;

    public IPAddress Address { get; }

    public DateTime LastUpdate
    {
        get => _lastUpdate;
        private set => SetField(ref _lastUpdate, value);
    }

    public int PM2d5
    {
        get => _pm2d5;
        private set => SetField(ref _pm2d5, value);
    }

    public int PM10
    {
        get => _pm10;
        private set => SetField(ref _pm10, value);
    }

    public float HCHO
    {
        get => _hcho;
        private set => SetField(ref _hcho, value);
    }

    public float TVOC
    {
        get => _tvoc;
        private set => SetField(ref _tvoc, value);
    }

    public int CO2
    {
        get => _co2;
        private set => SetField(ref _co2, value);
    }

    public float Temperature
    {
        get => _temperature;
        private set => SetField(ref _temperature, value);
    }

    public float RelativeHumidity
    {
        get => _relativeHumidity;
        private set => SetField(ref _relativeHumidity, value);
    }

    public DataViewModel(IPAddress address)
    {
        Address = address;
    }

    public void Update(ReadOnlySpan<byte> data)
    {
        LastUpdate = DateTime.Now;

        PM2d5 = GetData(data, 0);
        PM10 = GetData(data, 1);
        HCHO = GetData(data, 2) / 100f;
        TVOC = GetData(data, 3) / 100f;
        CO2 = GetData(data, 4);
        Temperature = (GetData(data, 5) - 3500) / 100f;
        RelativeHumidity = GetData(data, 6) / 100f;
    }

    private static int GetData(ReadOnlySpan<byte> data, int index)
    {
        return data[index * 2 + 1] | (data[index * 2] << 8);
    }
}