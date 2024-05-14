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
            try
            {
                while (true)
                {
                    using (var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        Logger.Info("Waiting for connection...");

                        await pipeServer.WaitForConnectionAsync();
                        Logger.Info("Pipe connected.");

                        await ProcessClientAsync(pipeServer);
                    }
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
                    await Task.Delay(100); // 等待一段时间，可以根据需要调整

                    byte[] buffer = new byte[256];
                    int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Logger.Info("Client disconnected.");
                        break;
                    }

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Logger.Info($"Received message from client: {receivedMessage}");

                    await ProcessClientMessageAsync(pipeServer, receivedMessage);
                }
            }
            catch (IOException ex)
            {
                Logger.Info($"Pipe is broken: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Info($"Error occurred: {ex.Message}");
            }
        }

        private static async Task ProcessClientMessageAsync(NamedPipeServerStream pipeServer, string receivedMessage)
        {
            if (receivedMessage == "cL")
            {
                await ClientCheckServerAlive(pipeServer);
            }
            else if (receivedMessage == "sS")
            {
                await ClientAskForPassiveType(pipeServer);
            }
            else if (receivedMessage == "sS2")
            {
                await ClientAskForScreenshot(pipeServer);
            }
            else if (receivedMessage == "dI")
            {
                await ClientAskForDrawImage(pipeServer);
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
                
                    using (MemoryStream memoryStream = new MemoryStream(messageBytes))
                    {
                        await memoryStream.CopyToAsync(pipeServer);
                        
                        pipeServer.WaitForPipeDrain();

                        Logger.Debug("Passive type sent successfully.");
                    }
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
                    
                    GC.Collect();
    
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
                    using MemoryStream memoryStream = new MemoryStream(messageBytes);

                    await memoryStream.CopyToAsync(pipeServer);
                        
                    pipeServer.WaitForPipeDrain();
                        
                    Logger.Info("Sent response.");
                }
                else
                {
                    Logger.Error("Client cant write.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
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

                    if (Helper.NullCheck(result))
                    {
                        Logger.Warning($"Image stream is null.");
                        return;
                    }

                    Logger.Info($"{result.Length}");
                    
                    await pipeServer.FlushAsync();
                    
                    await pipeServer.WriteAsync(Encoding.UTF8.GetBytes(result), 0, result.Length);
                    
                    pipeServer.WaitForPipeDrain();
                }
                else
                {
                    Logger.Error("Client cant write.");
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
