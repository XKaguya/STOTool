using System.ComponentModel;

namespace STOTool.Settings
{
    public static class GlobalVariables
    {
        [Description("Cache life time in minute. \nDefault value: 15, 10, 1")]
        public static ushort[] CacheLifeTime { get; set; } = { 15, 10, 1 };

        [Description("Program Level. \nDefault value: Normal")]
        public static string ProgramLevel { get; set; } = "Normal";

        [Description("Log level. \nDefault value: Info")]
        public static string LogLevel { get; set; } = "Info";

        [Description("Whether or not enable the Legacy Pipe Server instead of the Websocket Server. \nDefault value: False")]
        public static bool LegacyPipeMode { get; set; } = false;

        [Description("WebSocket Listener address. \nDefault value: http://localhost")]
        public static string WebSocketListenerAddress { get; set; } = "http://localhost";

        [Description("WebSocket Listener port. \nDefault value: 9500")]
        public static ushort WebSocketListenerPort { get; set; } = 9500;
    }
}