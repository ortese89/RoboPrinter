﻿using Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BackEnds.RoboPrinter.Services;

public class TcpExternalCommunication : ExternalCommunicationBase
{
    public event EventHandler<ExternalDeviceEventArgs> PickLabelRequested;
    public event EventHandler<ExternalDeviceEventArgs> ApplyLabelRequested;
    public event EventHandler<ExternalDeviceEventArgs> PrintLabelRequested;
    public event EventHandler ResetRequested;

    private readonly TcpListener _listener;
    private readonly ILogger<TcpExternalCommunication> _logger;
    private TcpClient _client;
    private bool _isRunning = false;

    public TcpExternalCommunication(IConfiguration configuration, ILogger<TcpExternalCommunication> logger)
    {
        _logger = logger;
        int port = Convert.ToInt32(configuration["ExternalDevices:TCP/IP:Port"]);
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public override void Connect()
    {
        if (!_isRunning)
        {
            _isRunning = true;
            _logger.LogInformation("TcpExternalCommunication - Starting Listener...");
            _listener.Start();
            AcceptClientAsync();
        }
    }

    private async Task AcceptClientAsync()
    {
        while (_isRunning)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                _client = client;
                await HandleClientAsync(client).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_isRunning)
                    break;
            }
        }
    }

    //private async Task HandleClientAsync(TcpClient client)
    //{
    //    try
    //    {
    //        using var networkStream = client.GetStream();
    //        var reader = new StreamReader(networkStream, Encoding.UTF8);

    //        while (client.Connected)
    //        {
    //            var buffer = new char[1024];
    //            int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
    //            if (bytesRead == 0) break;
    //            ProcessData(new string(buffer, 0, bytesRead));
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"HandleClientAsync {ex.Message}");
    //    }
    //}

    private string _dataBuffer = "";

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using var networkStream = client.GetStream();
            var reader = new StreamReader(networkStream, Encoding.UTF8);

            while (client.Connected)
            {
                var buffer = new char[1024];
                int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                // Accumula i dati ricevuti nel buffer
                _dataBuffer += new string(buffer, 0, bytesRead);

                // Processa i messaggi completi nel buffer
                while (_dataBuffer.Contains('\u0002') && _dataBuffer.Contains('\u0003'))
                {
                    int startIndex = _dataBuffer.IndexOf('\u0002'); // Trova STX
                    int endIndex = _dataBuffer.IndexOf('\u0003', startIndex); // Trova ETX dopo STX

                    if (endIndex > startIndex)
                    {
                        // Estrai il messaggio tra STX ed ETX
                        var message = _dataBuffer.Substring(startIndex + 1, endIndex - startIndex - 1);
                        _dataBuffer = _dataBuffer.Substring(endIndex + 1); // Rimuovi il messaggio elaborato

                        // Processa il messaggio
                        ProcessData(message.Trim());
                    }
                    else
                    {
                        // Se manca ETX, aspetta ulteriori dati
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HandleClientAsync {ex.Message}");
        }
    }



    public override void Disconnect()
    {
        _client?.Close();
        _isRunning = false;
        _listener.Stop();
    }

    public override void Reset()
    {
        _client?.GetStream().Flush();
    }

    public override void SendStatus(SystemStatus status)
    {
        SendMessage(status.ToString());
    }

    public void SendMessage(string message)
    {
        if (_client is not null && _client.Connected)
        {
            var writer = new StreamWriter(_client.GetStream());
            _logger.LogInformation("TcpExternalCommunication - Sending Message: {Message}", message);
            writer.Write(message);
            writer.Flush();
        }
    }
}
