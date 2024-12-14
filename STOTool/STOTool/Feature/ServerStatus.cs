using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STOTool.Class;
using STOTool.Enum;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class ServerStatus
    {
        public static async Task<MaintenanceInfo> CheckServerAsync()
        {
            try
            {
                string? message = Api.IsDebugMode() ? Api.GetDebugMessage() : await GetMaintenanceTimeFromLauncherAsync();
                
                if (string.IsNullOrEmpty(message))
                {
                    Logger.Trace("There's no message in launcher page.");
                    return new MaintenanceInfo { ShardStatus = MaintenanceTimeType.Null };
                }
                
                ShardStatus serverStatus = ExtractServerStatus(message);
                string result = ExtractMessage(message);

                if (result == "null")
                {
                    Logger.Trace("There's no message.");
                    return new MaintenanceInfo { ShardStatus = MaintenanceTimeType.Null }; 
                }
                
                if (result != "null" && !result.Contains("UTC"))
                {
                    return new MaintenanceInfo { Message = result };
                }

                var (date, startTime, endTime) = await ExtractMaintenanceTime(message);
                var (startEventTime, endEventTime) = TimeUntilMaintenance(date, startTime, endTime);
                DateTime currentTime = DateTime.Now;
                var maintenanceType = GetMaintenanceTimeType(currentTime, startEventTime, endEventTime, serverStatus);

                var maintenanceInfo = new MaintenanceInfo { ShardStatus = maintenanceType };

                if (startEventTime.HasValue && endEventTime.HasValue && maintenanceType != MaintenanceTimeType.Maintenance && maintenanceType != MaintenanceTimeType.SpecialMaintenance && maintenanceType != MaintenanceTimeType.Null)
                {
                    var timeRemaining = startEventTime.Value.Subtract(currentTime);
                    if (timeRemaining < TimeSpan.Zero)
                    {
                        timeRemaining = TimeSpan.Zero;
                    }
                    maintenanceInfo.Days = timeRemaining.Days;
                    maintenanceInfo.Hours = timeRemaining.Hours;
                    maintenanceInfo.Minutes = timeRemaining.Minutes;
                    maintenanceInfo.Seconds = timeRemaining.Seconds;
                }
                else if (startEventTime.HasValue && endEventTime.HasValue && maintenanceType != MaintenanceTimeType.MaintenanceEnded && maintenanceType != MaintenanceTimeType.Null && maintenanceType != MaintenanceTimeType.None)
                {
                    var timeRemaining = endEventTime.Value.Subtract(currentTime);
                    if (timeRemaining < TimeSpan.Zero)
                    {
                        timeRemaining = TimeSpan.Zero;
                    }
                    maintenanceInfo.Days = timeRemaining.Days;
                    maintenanceInfo.Hours = timeRemaining.Hours;
                    maintenanceInfo.Minutes = timeRemaining.Minutes;
                    maintenanceInfo.Seconds = timeRemaining.Seconds;
                }

                if (Api.IsDebugMode())
                {
                    Logger.Trace(maintenanceInfo.ToString());
                }

                return maintenanceInfo;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
                throw;
            }
        }

        private static MaintenanceTimeType GetMaintenanceTimeType(DateTime currentTime, DateTime? startTime, DateTime? endTime, ShardStatus serverStatus)
        {
            if (serverStatus == ShardStatus.Up)
            {
                if (currentTime < startTime)
                    return MaintenanceTimeType.WaitingForMaintenance;
                if (currentTime > endTime)
                    return MaintenanceTimeType.MaintenanceEnded;
            }
            if (serverStatus == ShardStatus.Maintenance)
            {
                if (currentTime <= endTime)
                    return MaintenanceTimeType.Maintenance;
                if (currentTime > endTime)
                    return MaintenanceTimeType.SpecialMaintenance;
            }
            return MaintenanceTimeType.None;
        }

        private static ShardStatus ExtractServerStatus(string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return ShardStatus.None;
            }

            try
            {
                var json = JObject.Parse(message.Trim('\"').Replace("\\\"", "\""));
                var serverStatus = json["server_status"]?.ToString();
                return serverStatus switch
                {
                    "up" => ShardStatus.Up,
                    "down" => ShardStatus.Maintenance,
                    _ => ShardStatus.None
                };
            }
            catch (JsonReaderException ex)
            {
                Logger.Error($"Failed to extract server status: {ex.Message}");
                return ShardStatus.None;
            }
        }
        
        private static string ExtractMessage(string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return "null";
            }

            try
            {
                var json = JObject.Parse(message.Trim('\"').Replace("\\\"", "\""));
                var msg = json["message"]?.ToString();

                if (!string.IsNullOrEmpty(msg))
                {
                    return msg;
                }

                return "null";
            }
            catch (JsonReaderException ex)
            {
                Logger.Error($"Failed to extract message: {ex.Message}");
                return "null";
            }
        }

        private static async Task<string?> GetMaintenanceTimeFromLauncherAsync()
        {
            string url = "http://launcher.startrekonline.com/launcher_server_status";
            HttpResponseMessage response = await Helper.HttpClient.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }

        private static (DateTime?, DateTime?) TimeUntilMaintenance(DateTime? date, DateTime? startTime, DateTime? endTime)
        {
            if (date == null || startTime == null || endTime == null)
                return (null, null);

            DateTime startDateTime = date.Value.Date.Add(startTime.Value.TimeOfDay);
            DateTime endDateTime = date.Value.Date.Add(endTime.Value.TimeOfDay);
            if (endDateTime < startDateTime)
            {
                endDateTime = endDateTime.AddDays(1);
            }
            
            return (startDateTime, endDateTime);
        }

        private static async Task<(DateTime?, DateTime?, DateTime?)> ExtractMaintenanceTime(string message)
        {
            return await Task.Run(() =>
            {
                DateTime? date = TryParseDate(message, @"(?i)(January|February|March|April|May|June|July|August|September|October|November|December) \d+");
                (DateTime? startTime, DateTime? endTime) = TryParseDateTime(message, @"(\d{1,2}:\d{2})-(\d{1,2}:\d{2})", date!.Value);
                (DateTime? utcStartTime, DateTime? utcEndTime) = TryParseDateTime(message, @"(\d{1,2}:\d{2})-(\d{1,2}:\d{2} UTC)", date!.Value);
                
                Logger.Trace($"PT {startTime.ToString()} {endTime.ToString()}");
                Logger.Trace($"UTC {utcStartTime.ToString()} {utcEndTime.ToString()}");

                DateTime? finalStartTime = utcStartTime;
                DateTime? finalEndTime = utcEndTime;
                if (startTime != null && endTime != null && utcStartTime != null && utcEndTime != null && !CompareTimeRanges(startTime, endTime, utcStartTime, utcEndTime))
                {
                    Logger.Debug("The time range isn't the same.");
                }
                (finalStartTime, finalEndTime) = ConvertToLocalTime(finalStartTime, finalEndTime);
                return (date, finalStartTime, finalEndTime);
            });
        }

        private static bool CompareTimeRanges(DateTime? timeSpan0, DateTime? timeSpan1, DateTime? timeSpan2, DateTime? timeSpan3)
        {
            return timeSpan0 < timeSpan1 && timeSpan2 < timeSpan3;
        }

        private static (DateTime?, DateTime?) ConvertToLocalTime(DateTime? utcTime0, DateTime? utcTime1)
        {
            if (utcTime0 == null || utcTime1 == null)
                return (null, null);

            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            DateTime localDateTime0 = TimeZoneInfo.ConvertTimeFromUtc(utcTime0.Value, localTimeZone);
            DateTime localDateTime1 = TimeZoneInfo.ConvertTimeFromUtc(utcTime1.Value, localTimeZone);

            Logger.Trace(localDateTime0.ToString());
            Logger.Trace(localDateTime1.ToString());

            return (localDateTime0, localDateTime1);
        }

        private static DateTime? TryParseDate(string input, string pattern)
        {
            Match match = Regex.Match(input, pattern);
            return match.Success && DateTime.TryParse(match.Value, out DateTime temp) ? temp : null;
        }

        private static (DateTime?, DateTime?) TryParseDateTime(string input, string pattern, DateTime baseDate)
        {
            Match match = Regex.Match(input, pattern);
            if (match.Success)
            {
                Logger.Trace($"Regex Match Successful: {match.Value}");
                string[] times = match.Value.Split('-');
        
                if (times.Length != 2)
                {
                    Logger.Error($"Unexpected time format: {match.Value}");
                    return (null, null);
                }
        
                string startTimeStr = times[0].Trim();
                string endTimeStr = times[1].Trim();
                
                endTimeStr = endTimeStr.Split(' ')[0];
        
                Logger.Trace($"Start Time String: {startTimeStr}, End Time String: {endTimeStr}");
        
                if (TimeSpan.TryParse(startTimeStr, out TimeSpan start) && TimeSpan.TryParse(endTimeStr, out TimeSpan end))
                {
                    Logger.Trace($"Parsed Start Time: {start}, Parsed End Time: {end}");
                    return (baseDate.Add(start), baseDate.Add(end));
                }
                else
                {
                    Logger.Error($"Failed to parse times: Start Time: {startTimeStr}, End Time: {endTimeStr}");
                }
            }
            else
            {
                Logger.Trace("Regex Match Failed");
            }
    
            return (null, null);
        }
    }
}