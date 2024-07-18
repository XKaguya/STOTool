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

            switch (maintenanceInfo.ShardStatus)
            {
                case MaintenanceTimeType.WaitingForMaintenance:
                    if (!waitingForMaintenance)
                    {
                        WriteCheckerJson(true, !waitForMaintenanceSent, false, false, false, false);
                        if (!waitForMaintenanceSent)
                        {
                            return PassiveEnum.WaitingForMaintenance;
                        }
                        else
                        {
                            return PassiveEnum.WaitingForMaintenanceSent;
                        }
                    }
                    else
                    {
                        return PassiveEnum.WaitingForMaintenanceSent;
                    }

                case MaintenanceTimeType.Maintenance:
                    if (!maintenanceStarted)
                    {
                        WriteCheckerJson(true, true, !maintenanceStartedSent, false, false, false);
                        if (!maintenanceStartedSent)
                        {
                            return PassiveEnum.MaintenanceStarted;
                        }
                        else
                        {
                            return PassiveEnum.MaintenanceStartedSent;
                        }
                    }
                    else
                    {
                        return PassiveEnum.MaintenanceStartedSent;
                    }

                case MaintenanceTimeType.MaintenanceEnded:
                    if (!maintenanceEnded)
                    {
                        WriteCheckerJson(false, false, false, false, true, !maintenanceEndedSent);
                        if (!maintenanceEndedSent)
                        {
                            return PassiveEnum.MaintenanceEnded;
                        }
                        else
                        {
                            return PassiveEnum.MaintenanceEndedSent;
                        }
                    }
                    else
                    {
                        return PassiveEnum.MaintenanceEndedSent;
                    }
                
                case MaintenanceTimeType.SpecialMaintenance:
                    if (!maintenanceEnded && maintenanceStarted && maintenanceStartedSent)
                    {
                        WriteCheckerJson(false, false, false, false, true, !maintenanceEndedSent);
                        if (!maintenanceEndedSent)
                        {
                            return PassiveEnum.MaintenanceEnded;
                        }
                        else
                        {
                            return PassiveEnum.MaintenanceEndedSent;
                        }
                    }
                    else
                    {
                        return PassiveEnum.MaintenanceEndedSent;
                    }
                
                case MaintenanceTimeType.None:
                    if (!maintenanceEnded)
                    {
                        WriteCheckerJson(false, false, false, false, true, !maintenanceEndedSent);
                        if (!maintenanceEndedSent)
                        {
                            return PassiveEnum.MaintenanceEnded;
                        }
                        else
                        {
                            return PassiveEnum.MaintenanceEndedSent;
                        }
                    }
                    else
                    {
                        return PassiveEnum.MaintenanceEndedSent;
                    }

                default:
                    return PassiveEnum.Null;
            }
        }
    }
}