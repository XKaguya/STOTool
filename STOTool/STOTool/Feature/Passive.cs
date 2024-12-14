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
        
        private static bool WriteCheckerJson(bool waitingForMaintenance, bool waitingForMaintenanceSent, bool maintenanceStarted, bool maintenanceStartedSent, bool maintenanceEnded, bool maintenanceEndedSent)
        {
            try
            {
                var jsonObj = new JObject
                (
                    new JProperty("WaitingForMaintenance", waitingForMaintenance),
                    new JProperty("WaitingForMaintenanceSent", waitingForMaintenanceSent),
                    new JProperty("MaintenanceStarted", maintenanceStarted),
                    new JProperty("MaintenanceStartedSent", maintenanceStartedSent),
                    new JProperty("MaintenanceEnded", maintenanceEnded),
                    new JProperty("MaintenanceEndedSent", maintenanceEndedSent)
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
            bool waitingForMaintenanceSent = data["WaitingForMaintenanceSent"].Value<bool>();

            bool maintenanceStarted = data["MaintenanceStarted"].Value<bool>();
            bool maintenanceStartedSent = data["MaintenanceStartedSent"].Value<bool>();

            bool maintenanceEnded = data["MaintenanceEnded"].Value<bool>();
            bool maintenanceEndedSent = data["MaintenanceEndedSent"].Value<bool>();

            MaintenanceInfo maintenanceInfo = await ServerStatus.CheckServerAsync();

            switch (maintenanceInfo.ShardStatus)
            {
                case MaintenanceTimeType.WaitingForMaintenance:
                    return UpdateJsonAndReturn(waitingForMaintenance, waitingForMaintenanceSent, PassiveEnum.WaitingForMaintenance, PassiveEnum.WaitingForMaintenanceSent);

                case MaintenanceTimeType.Maintenance:
                    return UpdateJsonAndReturn(maintenanceStarted, maintenanceStartedSent, PassiveEnum.MaintenanceStarted, PassiveEnum.MaintenanceStartedSent);

                case MaintenanceTimeType.MaintenanceEnded:
                case MaintenanceTimeType.SpecialMaintenance:
                case MaintenanceTimeType.None:
                    return UpdateJsonAndReturn(maintenanceEnded, maintenanceEndedSent, PassiveEnum.MaintenanceEnded, PassiveEnum.MaintenanceEndedSent);
                
                default:
                    return PassiveEnum.Null;
            }
        }
        
        private static PassiveEnum UpdateJsonAndReturn(bool currentState, bool currentStateSent, PassiveEnum stateEnum, PassiveEnum stateSentEnum)
        {
            if (!currentState)
            {
                WriteCheckerJson(
                    stateEnum == PassiveEnum.WaitingForMaintenance, 
                    !currentStateSent, 
                    stateEnum == PassiveEnum.MaintenanceStarted, 
                    stateEnum == PassiveEnum.MaintenanceStartedSent,
                    stateEnum == PassiveEnum.MaintenanceEnded, 
                    stateEnum == PassiveEnum.MaintenanceEndedSent
                );
                return !currentStateSent ? stateEnum : stateSentEnum;
            }
            else
            {
                return stateSentEnum;
            }
        }
    }
}