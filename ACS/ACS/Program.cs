using MQTTnet;
using MQTTnet.Client;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;



namespace APP
{
    class Program
    {
        private static MqttFactory? mqttFactory;
        private static IMqttClient? mqttClient;
        private const string AppToAcsChannel = "APPACS";
        private const string AcsToAppChannel = "ACSAPP";
        private static HttpListener? httpListener;
        private const string WebSocketUrl = "http://localhost:3000/";
        private static string AmrStat = "";
        private static WebSocket? webSocket;




        static async Task Main(string[] args)
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(WebSocketUrl);
            httpListener.Start();

            Console.WriteLine("ACS started");

            

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                   .WithTcpServer("localhost", 1883)
                   .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            SubMqttACS();

            Console.WriteLine("ACS started");
            var amrStatusTimer = new Timer(async _ => await SendAmrStatusToWebSocket(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            while (true)
            {
                try
                {
                    var context = await httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        HandleWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            static void PubMqttACS(string cmd)
            {
                var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(AcsToAppChannel)
                .WithPayload(cmd)
                .Build();

                mqttClient?.PublishAsync(applicationMessage, CancellationToken.None);
            }

            static void SubMqttACS()
            {
                if (mqttClient == null)
                {
                    return;
                }
                mqttClient.ApplicationMessageReceivedAsync += ea =>
                {
                    Log.Information($"ACS subscribe from APP MQTT: {AppToAcsChannel}" + "--(" + ea.ApplicationMessage.ConvertPayloadToString() + ")");
                    AmrStat = ea.ApplicationMessage.ConvertPayloadToString();
                    

                    return Task.CompletedTask;
                };

                var mqttSubscribeOptions = mqttFactory?.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(
                        f =>
                        {
                            f.WithTopic(AppToAcsChannel);
                        })
                    .Build();
                mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
            }

            async void HandleWebSocketRequest(HttpListenerContext context)
            {
                var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                webSocket = webSocketContext.WebSocket;

                Console.WriteLine("WebSocket connection accepted.");

                var buffer = new byte[1024];
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received web message: {receivedMessage}");
                        
                        /*var responseMessage = Encoding.UTF8.GetBytes("ACS Server received your message: " + receivedMessage);
                        await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);*/

                        if (receivedMessage != null)
                        {
                            Console.WriteLine($"ACS publish {receivedMessage} to App MQTT ");
                            PubMqttACS(receivedMessage);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("WebSocket closed by the client.");
                        break;
                    }
                }
            }
            static async Task SendAmrStatusToWebSocket()
            {
                // 检查 WebSocket 是否可用
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    // 将 AmrStat 转换为字节数组
                    var amrStatBytes = Encoding.UTF8.GetBytes(AmrStat);

                    // 发送 AmrStat 到 WebSocket 客户端
                    await webSocket.SendAsync(new ArraySegment<byte>(amrStatBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}