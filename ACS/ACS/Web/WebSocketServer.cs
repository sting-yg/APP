using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACS.Web
{
    class WebSocketServer
    {
        private HttpListener httpListener;
        private const string WebSocketUrl = "http://localhost:3000/"; // WebSocket服务器的URL
        private const string CommandChannel = "command";

        public WebSocketServer()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(WebSocketUrl);
        }

        public async Task StartAsync()
        {
            httpListener.Start();
            Console.WriteLine($"WebSocket server listening at {WebSocketUrl}");

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
        }

        private async void HandleWebSocketRequest(HttpListenerContext context)
        {
            var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
            var webSocket = webSocketContext.WebSocket;

            Console.WriteLine("WebSocket connection accepted.");

            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message: {receivedMessage}");

                    if (receivedMessage != null) 
                    {
                        switch (receivedMessage)
                        {
                            case "move":
                                {
                                    
                                }
                                break;
                            case "load":
                                { 
                                
                                }
                                break;
                            case "":
                                { 

                                }
                            break;  
                        }
                    
                    }

                    // 处理接收到的命令，可以在这里添加相应的逻辑

                    // Example: 发送回应给Vue项目
                    var responseMessage = Encoding.UTF8.GetBytes("Server received your message: " + receivedMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("WebSocket closed by the client.");
                    break;
                }

                
            }
            
        }
    }
}
