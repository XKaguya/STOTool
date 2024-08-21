using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using STOTool.Enum;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class PipeServer
    {
        private static readonly string PipeName;
        static PipeServer()
        {
            PipeName = "STOChecker";
        }

        public static async Task StartServerAsync()
        {
            Logger.Warning("Pipe will be deprecated in the future. Please use the WebSocket server instead.");
            Logger.Warning("The Pipe Server maintains just for compatibility of the old plugins.");
            
            try
            {
                while (true)
                {
                    await using var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    
                    Logger.Info("Waiting for connection...");

                    await pipeServer.WaitForConnectionAsync();
                    Logger.Info("Pipe connected.");

                    await ProcessClientAsync(pipeServer);
                }
            }
            catch (Exception e)
            {
                Logger.Fatal(e.Message + e.StackTrace);
                throw;
            }
        }
        
        private static async Task ProcessClientAsync(NamedPipeServerStream pipeServer)
        {
            try
            {
                while (pipeServer.IsConnected)
                {
                    await Task.Delay(100);

                    byte[] buffer = new byte[256];
                    int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Logger.Info("Client disconnected.");
                        break;
                    }

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Logger.Info($"Received the following command from client: {receivedMessage}");

                    await ProcessClientMessageAsync(pipeServer, receivedMessage);
                }
            }
            catch (IOException ex)
            {
                Logger.Info($"Pipe is broken: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error occurred: {ex.Message}\n + {ex.StackTrace}");
            }
        }

        private static async Task ProcessClientMessageAsync(NamedPipeServerStream pipeServer, string receivedMessage)
        {
            string[] messageParts = receivedMessage.Split(' ');

            if (System.Enum.TryParse<Command>(messageParts[0], out Command command))
            {
                int index = 0;
                if (messageParts.Length > 1 && int.TryParse(messageParts[1], out index))
                {
                    if (command == Command.ClientAskForNews)
                    {
                        await ClientAskForNewsInIndex(pipeServer, index);
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
                            await ClientCheckServerAlive(pipeServer);
                            break;
                        case Command.ClientAskForPassiveType:
                            await ClientAskForPassiveType(pipeServer);
                            break;
                        case Command.ClientAskForScreenshot:
                            await ClientAskForScreenshot(pipeServer);
                            break;
                        case Command.ClientAskForDrawImage:
                            await ClientAskForDrawImage(pipeServer);
                            break;
                        case Command.ClientAskForRefreshCache:
                            await ClientAskRefreshCache();
                            break;
                        case Command.ClientAskIfHashChanged:
                            await ClientAskIfHashChanged(pipeServer);
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

        private static async Task ClientAskForPassiveType(PipeStream pipeServer)
        {
            try
            {
                if (pipeServer.IsConnected && pipeServer.CanWrite)
                {
                    Logger.Debug("Client is connected and writeable. Trying to send passive type through pipe.");

                    PassiveEnum passiveType = await Passive.MessageAction();
                    Logger.Debug($"Passive type: {passiveType}");
                    byte[] messageBytes = Encoding.UTF8.GetBytes(passiveType.ToString());
                
                    await pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length);
                    pipeServer.WaitForPipeDrain();

                    Logger.Debug("Passive type sent successfully.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }

        private static async Task ClientAskForScreenshot(PipeStream pipeServer)
        {
            try
            {
                if (pipeServer.IsConnected && pipeServer.CanWrite)
                {
                    Logger.Debug("Client is connected and writeable. Trying to send screenshot though pipe.");
    
                    string base64Image = await Screenshot.CaptureAndSaveAsync();

                    byte[] imageData = Convert.FromBase64String(base64Image);
                    int imgSize = imageData.Length;
                    
                    byte[] sizeBytes = BitConverter.GetBytes(imgSize);
                    
                    await pipeServer.WriteAsync(sizeBytes, 0, sizeBytes.Length);
                    
                    pipeServer.WaitForPipeDrain();
                    
                    await pipeServer.WriteAsync(imageData, 0, imageData.Length);
                    
                    pipeServer.WaitForPipeDrain();
    
                    Logger.Debug($"Screenshot sent successfully.");
                }
                else
                {
                    Logger.Error("Client can't write.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }

        private static async Task ClientCheckServerAlive(PipeStream pipeServer)
        {
            try
            {
                if (pipeServer.IsConnected && pipeServer.CanWrite)
                {
                    Logger.Info("Client awaiting for response. Sending now...");

                    byte[] messageBytes = Encoding.UTF8.GetBytes("Success");
                    await pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length);
                    pipeServer.WaitForPipeDrain();

                    Logger.Info("Sent response.");
                }
                else
                {
                    Logger.Error("Client can't write.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }
        
        private static async Task ClientAskRefreshCache()
        {
            try
            {
                await Cache.RemoveAll();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }
        
        private static async Task ClientAskIfHashChanged(PipeStream pipeServer)
        {
            try
            {
                var result = await AutoNews.HasHashChanged();

                if (result != "null")
                {
                    if (pipeServer.IsConnected && pipeServer.CanWrite)
                    {
                        Logger.Info("Pipe is connected and writable. Preparing to send response...");

                        byte[] resultBytes = Encoding.UTF8.GetBytes(result);

                        await pipeServer.WriteAsync(resultBytes, 0, resultBytes.Length);

                        pipeServer.WaitForPipeDrain();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }
        
        private static async Task ClientAskForNewsInIndex(PipeStream pipeServer, int index)
        {
            try
            {
                if (pipeServer.IsConnected && pipeServer.CanWrite)
                {
                    Logger.Info("Pipe is connected and writable. Preparing to send response...");

                    /*string result = await GetNewsImage.CallScreenshot(index);

                    if (string.IsNullOrEmpty(result))
                    {
                        Logger.Warning("Result is null or empty after calling CallScreenshot.");
                        return;
                    }

                    byte[] resultBytes = Encoding.UTF8.GetBytes(result);*/
                    
                    // No longer in maintenance. This feature has been disposal in PipeServer.
                    
                    byte[] returnNull = Encoding.UTF8.GetBytes("null");

                    await pipeServer.WriteAsync(returnNull, 0, returnNull.Length);

                    pipeServer.WaitForPipeDrain();
                }
                else
                {
                    Logger.Error("Pipe is not connected or not writable.");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error in ClientAskForNewsInIndex: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }
        
        private static async Task ClientAskForDrawImage(PipeStream pipeServer)
        {
            try
            {
                if (pipeServer.IsConnected && pipeServer.CanWrite)
                {
                    Logger.Info("Client awaiting for response. Sending now...");

                    string result = await DrawNewsImage.DrawImageAsync();

                    if (string.IsNullOrEmpty(result))
                    {
                        Logger.Warning("Image stream is null.");
                        return;
                    }

                    byte[] resultBytes = Encoding.UTF8.GetBytes(result);
                    await pipeServer.WriteAsync(resultBytes, 0, resultBytes.Length);
                    
                    pipeServer.WaitForPipeDrain();
                }
                else
                {
                    Logger.Error("Client can't write.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }
    }
}