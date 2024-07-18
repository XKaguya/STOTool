using System;
using System.IO;
using System.Xml;
using STOTool.Class;
using STOTool.Enum;
using STOTool.Settings;

namespace STOTool.Generic
{
    public class Api
    {
        private static ProgramLevel ProgramLevel { get; set; } = ProgramLevel.Normal;
        private static string ConfigFilePath { get; } = "config.xml";

        public static bool IsDebugMode()
        {
            return ProgramLevel == ProgramLevel.Debug;
        }

        public static string? GetDebugMessage()
        {
            string filePath = Directory.GetCurrentDirectory();
            string msgPath = Path.Combine(filePath, "debug.json");

            if (File.Exists(msgPath))
            {
                try
                {
                    string fileContent = File.ReadAllText(msgPath);
                    return fileContent;
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred while reading debug.json file: " + ex.Message);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static bool SetProgramLevel(ProgramLevel mode)
        {
            ProgramLevel = mode;

            return ProgramLevel == mode;
        }

        public static bool ParseConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    Logger.Error("Config file not exist.");
                    File.WriteAllText(ConfigFilePath, string.Empty);

                    SaveToXml(ConfigFilePath);  
                }

                LoadFromXml(ConfigFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
                return false;
            }
        }
        
        private static void LoadFromXml(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlNode root = doc.DocumentElement;

            GlobalVariables.ProgramLevel = root.SelectSingleNode(nameof(GlobalVariables.ProgramLevel)).InnerText;
            GlobalVariables.LogLevel = root.SelectSingleNode(nameof(GlobalVariables.LogLevel)).InnerText;
            GlobalVariables.LegacyPipeMode = bool.Parse(root.SelectSingleNode(nameof(GlobalVariables.LegacyPipeMode)).InnerText);
            GlobalVariables.WebSocketListenerAddress = root.SelectSingleNode(nameof(GlobalVariables.WebSocketListenerAddress)).InnerText;
            GlobalVariables.WebSocketListenerPort = ushort.Parse(root.SelectSingleNode(nameof(GlobalVariables.WebSocketListenerPort)).InnerText);

            PostLoadConfig();
        }

        private static void PostLoadConfig()
        {
            try
            {
                SetProgramLevel(System.Enum.Parse<ProgramLevel>(GlobalVariables.ProgramLevel));
                Logger.SetLogLevel(System.Enum.Parse<LogLevel>(GlobalVariables.LogLevel));
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
        
        private static void SaveToXml(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Settings");
            doc.AppendChild(root);

            AppendElement(doc, root, nameof(GlobalVariables.ProgramLevel), GlobalVariables.ProgramLevel);
            AppendElement(doc, root, nameof(GlobalVariables.LogLevel), GlobalVariables.LogLevel);
            AppendElement(doc, root, nameof(GlobalVariables.LegacyPipeMode), GlobalVariables.LegacyPipeMode.ToString());
            AppendElement(doc, root, nameof(GlobalVariables.WebSocketListenerAddress), GlobalVariables.WebSocketListenerAddress);
            AppendElement(doc, root, nameof(GlobalVariables.WebSocketListenerPort), GlobalVariables.WebSocketListenerPort.ToString());

            doc.Save(filePath);
        }

        private static void AppendElement(XmlDocument doc, XmlElement root, string elementName, string value)
        {
            XmlElement element = doc.CreateElement(elementName);
            element.InnerText = value;
            root.AppendChild(element);
        }

        public static string MaintenanceInfoToString(MaintenanceInfo maintenanceInfo)
        {
            string result = "";

            switch (maintenanceInfo.ShardStatus)
            {
                case MaintenanceTimeType.Maintenance:
                    result =
                        $"Server is currently under maintenance. Finishes in {maintenanceInfo.Days} days {maintenanceInfo.Hours} hours {maintenanceInfo.Minutes} minutes {maintenanceInfo.Seconds} seconds.";
                    break;
                case MaintenanceTimeType.MaintenanceEnded:
                    result = "Maintenance has ended.";
                    break;
                case MaintenanceTimeType.WaitingForMaintenance:
                    result =
                        $"Server is waiting for maintenance. Starts in {maintenanceInfo.Days} days {maintenanceInfo.Hours} hours {maintenanceInfo.Minutes} minutes {maintenanceInfo.Seconds} seconds.";
                    break;
                default:
                    result = "";
                    break;
            }

            return result;
        }
    }
}