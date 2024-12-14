using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using STOTool.Class;
using STOTool.Enum;
using STOTool.Settings;

namespace STOTool.Generic
{
    public class Api
    {
        private static ProgramLevel ProgramLevel { get; set; } = ProgramLevel.Normal;
        public const string ConfigFilePath = "config.xml";

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

                    SaveSettingsToLocalFile(ConfigFilePath);  
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
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlNode root = doc.DocumentElement;

                foreach (var prop in typeof(GlobalVariables).GetProperties())
                {
                    var node = root.SelectSingleNode(prop.Name);
                    if (node != null)
                    {
                        var value = node.InnerText;
                        if (prop.PropertyType == typeof(ushort[]))
                        {
                            prop.SetValue(null, value.Split(',').Select(ushort.Parse).ToArray());
                        }
                        else if (prop.PropertyType == typeof(bool))
                        {
                            prop.SetValue(null, bool.Parse(value));
                        }
                        else if (prop.PropertyType == typeof(ushort))
                        {
                            prop.SetValue(null, ushort.Parse(value));
                        }
                        else
                        {
                            prop.SetValue(null, value);
                        }
                    }
                    else
                    {
                        Logger.Debug($"Node '{prop.Name}' not found in XML.");
                    }
                }

                PostLoadConfig();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading settings from XML: {ex.Message}");
            }
        }
        
        private static void PostLoadConfig()
        {
            try
            {
                SetProgramLevel(System.Enum.Parse<ProgramLevel>(GlobalVariables.ProgramLevel));
                
                if (System.Enum.Parse<ProgramLevel>(GlobalVariables.ProgramLevel) == ProgramLevel.Debug)
                {
                    Logger.SetLogLevel(LogLevel.Trace);
                    return;
                }
                
                Logger.SetLogLevel(System.Enum.Parse<LogLevel>(GlobalVariables.LogLevel));
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }

        public static void SaveSettingsToLocalFile(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Settings");
            doc.AppendChild(root);

            foreach (var prop in typeof(GlobalVariables).GetProperties())
            {
                var value = prop.GetValue(null);
                var descriptionAttr = (DescriptionAttribute)prop.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                var description = descriptionAttr?.Description ?? string.Empty;

                if (prop.PropertyType == typeof(ushort[]))
                {
                    AppendElement(doc, root, prop.Name, string.Join(",", (ushort[])value), description);
                }
                else
                {
                    AppendElement(doc, root, prop.Name, value.ToString(), description);
                }
            }

            doc.Save(filePath);
        }

        private static void AppendElement(XmlDocument doc, XmlNode root, string name, string value, string description)
        {
            var element = doc.CreateElement(name);
            element.InnerText = value;
            
            foreach (var line in description.Split('\n'))
            {
                var comment = doc.CreateComment(line);
                root.AppendChild(comment);
            }

            root.AppendChild(element);
        }
        
        public static string MaintenanceInfoToString(MaintenanceInfo maintenanceInfo)
        {
            string result = "";

            switch (maintenanceInfo.ShardStatus)
            {
                case MaintenanceTimeType.Maintenance:
                    result = $"Maintenance finishes in {maintenanceInfo.Days} days {maintenanceInfo.Hours} hours {maintenanceInfo.Minutes} minutes {maintenanceInfo.Seconds} seconds.";
                    break;
                case MaintenanceTimeType.MaintenanceEnded:
                    result = "Maintenance has ended.";
                    break;
                case MaintenanceTimeType.WaitingForMaintenance:
                    result = $"Waiting for maintenance. Starts in {maintenanceInfo.Days} days {maintenanceInfo.Hours} hours {maintenanceInfo.Minutes} minutes {maintenanceInfo.Seconds} seconds.";
                    break;
                default:
                    result = "";
                    break;
            }

            if (!string.IsNullOrEmpty(maintenanceInfo.Message))
            {
                return maintenanceInfo.Message;
            }

            return result;
        }
    }
}