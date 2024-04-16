using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using STOTool.Class;
using STOTool.Enum;

namespace STOTool.Feature
{
    public class Passive
    {
        private static readonly string CheckerJson = "Checker.json";

        private static JToken? ParseCheckerJson()
        {
            try
            {
                if (File.Exists(CheckerJson))
                {
                    string jsonText = File.ReadAllText(CheckerJson);
                
                    JToken parsedJson = JToken.Parse(jsonText);

                    return parsedJson;
                }
                else
                {
                    var jsonObj = new JObject
                        (
                            new JProperty("WaitingForMaintenance", false), 
                            new JProperty("WaitingForMaintenanceSent", false), 
                            
                            new JProperty("MaintenanceStarted", false), 
                            new JProperty("MaintenanceStartedSent", false),
                            
                            new JProperty("MaintenanceEnded", false), 
                            new JProperty("MaintenanceEndedSent", false)
                        );
                    
                    File.WriteAllText(CheckerJson, jsonObj.ToString());
                    
                    return jsonObj;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while parsing JSON file: {ex.Message}");
                return null;
            }
        }

        private static bool WriteCheckerJson(bool arg1, bool arg2, bool arg3, bool arg4, bool arg5, bool arg6)
        {
            try
            {
                var jsonObj = new JObject
                (
                    new JProperty("WaitingForMaintenance", arg1), 
                    new JProperty("WaitingForMaintenanceSent", arg2), 
                            
                    new JProperty("MaintenanceStarted", arg3), 
                    new JProperty("MaintenanceStartedSent", arg4),
                            
                    new JProperty("MaintenanceEnded", arg5), 
                    new JProperty("MaintenanceEndedSent", arg6)
                );
                    
                File.WriteAllText(CheckerJson, jsonObj.ToString());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while writing JSON file: {ex.Message}");
                return false;
            }
        }
    
        public static async Task<PassiveEnum> MessageAction()
        {
            JToken data = ParseCheckerJson();

            if (data == null)
            {
                return PassiveEnum.Null;
            }
            
            bool waitingForMaintenance = data["WaitingForMaintenance"].Value<bool>();
            bool waitForMaintenanceSent = data["WaitingForMaintenanceSent"].Value<bool>();
            
            bool maintenanceStarted = data["MaintenanceStarted"].Value<bool>();
            bool maintenanceStartedSent = data["MaintenanceStartedSent"].Value<bool>();
            
            bool maintenanceEnded = data["MaintenanceEnded"].Value<bool>();
            bool maintenanceEndedSent = data["MaintenanceEndedSent"].Value<bool>();
            
            MaintenanceInfo maintenanceInfo = await ServerStatus.CheckServerAsync();

            if (maintenanceInfo.ShardStatus == MaintenanceTimeType.WaitingForMaintenance)
            {
                if (!waitingForMaintenance)
                {
                    if (!waitForMaintenanceSent)
                    {
                        WriteCheckerJson(true, false, false, false, false, false);
                        
                        return PassiveEnum.WaitingForMaintenance;
                    }
                    else
                    {
                        WriteCheckerJson(true, true, false, false, false, false);
                        
                        return PassiveEnum.WaitingForMaintenanceSent;
                    }
                }
                else
                {
                    return PassiveEnum.WaitingForMaintenanceSent;
                }
            }
            else if (maintenanceInfo.ShardStatus == MaintenanceTimeType.Maintenance)
            {
                if (!maintenanceStarted)
                {
                    if (!maintenanceStartedSent)
                    {
                        WriteCheckerJson(true, true, true, false, false, false);

                        return PassiveEnum.MaintenanceStarted;
                    }
                    else
                    {
                        WriteCheckerJson(true, true, true, true, false, false);
                        
                        return PassiveEnum.MaintenanceStartedSent;
                    }
                }
                else
                {
                    return PassiveEnum.MaintenanceStartedSent;
                }
            }
            else if (maintenanceInfo.ShardStatus == MaintenanceTimeType.MaintenanceEnded)
            {
                if (!maintenanceEnded)
                {
                    if (!maintenanceEndedSent)
                    {
                        WriteCheckerJson(false, false, false, false, true, false);

                        return PassiveEnum.MaintenanceEnded;
                    }
                    else
                    {
                        WriteCheckerJson(false, false, false, false, true, true);

                        return PassiveEnum.MaintenanceEndedSent;
                    }
                }
                else
                {
                    return PassiveEnum.MaintenanceEndedSent;
                }
            }
            else
            {
                return PassiveEnum.Null;
            }
        }
    
    }
}