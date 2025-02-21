using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    [Info("TcpServerPlugin", "uggtiu", "1.0.1")]
    [Description("A plugin to open a TCP listener on the same port as the Rust server and log client IP addresses.")]
    public class TcpServerPlugin : RustPlugin
    {
        private TcpListener tcpListener;
        private bool isRunning = false;

        private void Init()
        {
            // Get the server port
            int serverPort = ConVar.Server.port;

            // Start the TCP server on the same port
            string address = $"0.0.0.0:{serverPort}";
            tcpListener = new TcpListener(IPAddress.Parse("0.0.0.0"), serverPort);
            tcpListener.Start();

            Puts($"TCP server listening on {address}");

            isRunning = true;
            Task.Run(() => AcceptClients());
        }

        private void Unload()
        {
            // Stop the TCP server
            isRunning = false;
            tcpListener?.Stop();
        }

        private async Task AcceptClients()
        {
            while (isRunning)
            {
                try
                {
                    var client = await tcpListener.AcceptTcpClientAsync();
                    var clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    Puts($"Получен запрос с IP-адреса {clientEndPoint.Address}");
                    Task.Run(() => HandleClient(client));
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Puts($"Error accepting client: {ex.Message}");
                    }
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            var buffer = new byte[1024];
            var stream = client.GetStream();
            int bytesRead;

            try
            {
                while (isRunning && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Puts($"Received data: {request}");

                    var response = Encoding.UTF8.GetBytes("Echo: " + request);
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                Puts($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}