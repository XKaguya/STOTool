namespace STOTool.Settings
{
    public class GlobalVariables
    {
        public static string ProgramLevel { get; set; } = "Normal";
        
        public static string LogLevel { get; set; } = "Info";
        
        public static bool LegacyPipeMode { get; set; } = false;

        public static string WebSocketListenerAddress { get; set; } = "http://localhost";

        public static ushort WebSocketListenerPort { get; set; } = 9500;
    }
}