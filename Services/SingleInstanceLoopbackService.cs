using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Atomex.Client.Desktop.Services
{
    internal class SingleInstanceLoopbackService
    {
        const int DefaultAtomexTcpPort = 49531;

        private Task? _worker;
        private CancellationTokenSource? _cts;

        private bool IsRunning => _worker != null && !_worker.IsCompleted;

        public void RunInBackground(Action<string> onReceive)
        {
            if (IsRunning)
                throw new Exception("Loopback service is already runned");

            _cts = new CancellationTokenSource();

            _worker = Task.Run(async () =>
            {
                var ipPoint = new IPEndPoint(IPAddress.Loopback, DefaultAtomexTcpPort);

                using var serverSocket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                serverSocket.Bind(ipPoint);
                serverSocket.Listen();
                
                while (true)
                {
                    if (_cts.IsCancellationRequested)
                        break;

                    using var clientSocket = await serverSocket.AcceptAsync();
                    var buffer = new List<byte>();

                    do
                    {
                        var currByte = new byte[1];
                        var byteCounter = clientSocket.Receive(currByte, currByte.Length, SocketFlags.None);

                        if (byteCounter == 1)
                            buffer.Add(currByte[0]);

                    } while (clientSocket.Available != 0);

                    var receivedSocketText = Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Count);
                    if (receivedSocketText != string.Empty)
                    {
                        onReceive?.Invoke(receivedSocketText);
                    }

                    clientSocket.Disconnect(reuseSocket: false);
                    clientSocket.Close();
                }
            }, _cts.Token);
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
            }
            finally
            {
                // nothing to do...
            }
        }

        public static bool TrySendArgsToOtherInstance(string[] args, ILogger? logger = null)
        {
            if (!TryGetLoopbackTcpPort(out var tcpPort, logger))
                tcpPort = DefaultAtomexTcpPort;

            using var tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                tcpClient.Connect(IPAddress.Loopback, tcpPort);
            }
            catch (SocketException)
            {
                return false;
            }

            if (args.Length == 0)
                return true;

            var bytesArgs = Encoding.UTF8.GetBytes(args[0]);
            tcpClient.Send(bytesArgs);

            logger?.LogInformation("Sending data to running app instance {Data}", args[0]);

            tcpClient.Disconnect(false);
            tcpClient.Close();

            return true;
        }

        public static bool TryGetLoopbackTcpPort(out int port, ILogger? logger = null)
        {
            const string LoopbackConfigFileName = "loopback.conf";

            var pathToConf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LoopbackConfigFileName);

            port = 0;

            if (!File.Exists(pathToConf))
                return false;

            try
            {
                var json = File.ReadAllText(pathToConf);
                var config = JsonSerializer.Deserialize<LoopbackConfig>(json);

                if (config == null)
                    return false;

                port = config.Port;

                logger?.LogDebug("Loopback configuration port: {@port}", port);

                return true;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Can't read loopback configuraton file");

                return false;
            }
        }

        private record LoopbackConfig(int Port);
    }
}