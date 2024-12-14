using System.Text;
using STOTool.Generic;

namespace STOTool.Class
{
    public class Settings
    {
        public string? CacheLifeTime { get; set; } = null;
        public string? ProgramLevel { get; set; } = null;
        public string? LogLevel { get; set; } = null;
        public string? WebSocketListenerAddress { get; set; } = null;
        public ushort? WebSocketListenerPort { get; set; } = null;
        public bool? AutoUpdate { get; set; } = null;
        public ushort? UserInterfaceWebSocketPort { get; set; } = null;

        public bool IsParameterNull()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"CacheLifeTime: {CacheLifeTime} ProgramLevel: {ProgramLevel}, LogLevel: {LogLevel}, WebSocketListenerAddress: {WebSocketListenerAddress}, WebSocketListenerPort: {WebSocketListenerPort}, AutoUpdate: {AutoUpdate}");
            Logger.Debug(stringBuilder.ToString());
            
            return CacheLifeTime == null ||
                   ProgramLevel == null ||
                   LogLevel == null ||
                   WebSocketListenerAddress == null ||
                   WebSocketListenerPort == null ||
                   AutoUpdate == null ||
                   UserInterfaceWebSocketPort == null;
        }
    }
}