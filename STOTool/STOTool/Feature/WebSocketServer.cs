using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using STOTool.Enum;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class WebSocketServer
    {
        private static HttpListener? _listener;
        private static CancellationTokenSource? _cancellationTokenSource;

        public static async Task StartWebSocketServerAsync(string[] prefixes)
        {
            _listener = new HttpListener();
            _cancellationTokenSource = new CancellationTokenSource();

            if (prefixes.Length != 0)
            {
                foreach (string prefix in prefixes)
                {
                    _listener.Prefixes.Add(prefix);
                }
            }
            else
            {
                throw new WebSocketException("Prefixes not defined.");
            }

            _listener.Start();
            Logger.Info("WebSocket server started.");

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    HttpListenerContext context = await _listener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _listener.Close();
                Logger.Info("WebSocket server stopped.");
            }
        }

        public static void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private static async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            WebSocketContext? webSocketContext = null;

            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                Logger.Debug($"Client connected.");

                WebSocket webSocket = webSocketContext.WebSocket;

                await HandleWebSocketMessages(webSocket);
            }
            catch (WebSocketException ex)
            {
                Logger.Error($"WebSocket error: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Logger.Error($"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
                if (webSocketContext?.WebSocket != null)
                {
                    await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal Server Error", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error: {ex.Message}\n{ex.StackTrace}");
                if (webSocketContext?.WebSocket != null)
                {
                    await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal Server Error", CancellationToken.None);
                }
            }
        }

        private static async Task HandleWebSocketMessages(WebSocket webSocket)
        {
            try
            {
                byte[] buffer = new byte[10000000];

                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Logger.Debug($"Received from client: {message}");

                        await ProcessClientMessageAsync(webSocket, message);

                        Array.Clear(buffer, 0, buffer.Length);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        Logger.Debug("WebSocket closed.");
                    }
                }
            }
            catch (WebSocketException ex)
            {
                Logger.Error($"WebSocket error: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Logger.Error($"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static async Task ProcessClientMessageAsync(WebSocket webSocket, string receivedMessage)
        {
            string[] messageParts = receivedMessage.Split(' ');

            if (System.Enum.TryParse(messageParts[0], out Command command))
            {
                if (messageParts.Length > 1 && int.TryParse(messageParts[1], out int index))
                {
                    if (command == Command.ClientAskForNews)
                    {
                        await ClientAskForNewsInIndex(webSocket, index);
                    }
                    else
                    {
                        Logger.Error($"Invalid command {command}");
                    }
                }
                else
                {
                    switch (command)
                    {
                        case Command.ClientCheckServerAlive:
                            await ClientCheckServerAlive(webSocket);
                            break;
                        case Command.ClientAskForPassiveType:
                            await ClientAskForPassiveType(webSocket);
                            break;
                        case Command.ClientAskForDrawImage:
                            await ClientAskForDrawImage(webSocket);
                            break;
                        case Command.ClientAskForRefreshCache:
                            await ClientAskRefreshCache();
                            break;
                        case Command.ClientAskIfHashChanged:
                            await ClientAskIfHashChanged(webSocket);
                            break;
                        default:
                            Logger.Error("Invalid Command.");
                            break;
                    }
                }
            }
            else
            {
                Logger.Error("Invalid command format.");
            }
        }
        
        private static async Task ClientAskForPassiveType(WebSocket webSocket)
        {
            try
            {
                PassiveEnum passiveType = await Passive.MessageAction();
                Logger.Debug($"Passive type: {passiveType}");
                byte[] messageBytes = Encoding.UTF8.GetBytes(passiveType.ToString());

                await webSocket.SendAsync(new ArraySegment<byte>(messageBytes, 0, messageBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                Logger.Debug("Passive type sent successfully.");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
            }
        }
        
        private static async Task ClientCheckServerAlive(WebSocket webSocket)
        {
            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes("Success");
                await webSocket.SendAsync(new ArraySegment<byte>(messageBytes, 0, messageBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                
                Logger.Debug("Sent WebSocket server status.");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
            }
        }
        
        private static async Task ClientAskRefreshCache()
        {
            try
            {
                await Cache.RemoveAll();
                
                Logger.Debug("Force refreshed all cache.");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
            }
        }
        
        private static async Task ClientAskIfHashChanged(WebSocket webSocket)
        {
            try
            {
                var result = await AutoNews.HasHashChanged();

                if (result != "null")
                {
                    byte[] resultBytes = Encoding.UTF8.GetBytes(result);

                    await webSocket.SendAsync(new ArraySegment<byte>(resultBytes, 0, resultBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                    Logger.Debug("Hash has changed. Sent result to client.");
                    return;
                }

                byte[] returnNull = Encoding.UTF8.GetBytes("null");

                await webSocket.SendAsync(new ArraySegment<byte>(returnNull, 0, returnNull.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (WebSocketException ex)
            {
                Logger.Error($"WebSocket error: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Logger.Error($"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static async Task ClientAskForNewsInIndex(WebSocket webSocket, int index)
        {
            try
            {
                string result = "null";
                if (index >= 0 && index <= 9)
                {
                    result = await GetNewsImage.CallScreenshot(index);
                }

                byte[] resultBytes = Encoding.UTF8.GetBytes(result);

                await webSocket.SendAsync(new ArraySegment<byte>(resultBytes, 0, resultBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                Logger.Debug("Sent.");
            }
            catch (Exception e)
            {
                Logger.Error($"Error in ClientAskForNewsInIndex: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private static async Task ClientAskForDrawImage(WebSocket webSocket)
        {
            try
            {
                string result = await DrawNewsImage.DrawImageAsync()!;

                byte[] resultBytes = Encoding.UTF8.GetBytes(result);
                await webSocket.SendAsync(new ArraySegment<byte>(resultBytes, 0, resultBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                Logger.Debug("Sent image.");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
            }
        }
    }
}