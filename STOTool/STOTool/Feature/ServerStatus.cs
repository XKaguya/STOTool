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
                ShardStatus serverStatus = ExtractServerStatus(message);

                if (message == null)
                {
                    Logger.Debug("Statement Null.");
                    return new MaintenanceInfo { ShardStatus = MaintenanceTimeType.None };
                }

                var (date, startTime, endTime) = await ExtractMaintenanceTime(message);
                var (startEventTime, endEventTime) = TimeUntilMaintenance(date, startTime, endTime);
                DateTime currentTime = DateTime.Now;
                var maintenanceType = GetMaintenanceTimeType(currentTime, startEventTime, endEventTime, serverStatus);

                var maintenanceInfo = new MaintenanceInfo { ShardStatus = maintenanceType };
                if (startEventTime.HasValue && endEventTime.HasValue)
                {
                    var timeRemaining = startEventTime.Value.Subtract(currentTime);
                    maintenanceInfo.Days = timeRemaining.Days;
                    maintenanceInfo.Hours = timeRemaining.Hours;
                    maintenanceInfo.Minutes = timeRemaining.Minutes;
                    maintenanceInfo.Seconds = timeRemaining.Seconds;
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
                return ShardStatus.None;

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
                Logger.Error($"Failed to parse JSON: {ex.Message}");
                return ShardStatus.None;
            }
        }

        private static async Task<string?> GetMaintenanceTimeFromLauncherAsync()
        {
            string url = "http://launcher.startrekonline.com/launcher_server_status";
            HttpResponseMessage response = await Helper.HttpClient.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }

        private static (DateTime?, DateTime?) TimeUntilMaintenance(DateTime? date, TimeSpan? startTime, TimeSpan? endTime)
        {
            if (date == null || startTime == null || endTime == null)
                return (null, null);

            DateTime startDateTime = date.Value.Date.Add(startTime.Value);
            DateTime endDateTime = date.Value.Date.Add(endTime.Value);
            if (endTime < startTime)
                endDateTime = endDateTime.AddDays(1);

            return (startDateTime, endDateTime);
        }

        private static async Task<(DateTime?, TimeSpan?, TimeSpan?)> ExtractMaintenanceTime(string message)
        {
            return await Task.Run(() =>
            {
                DateTime? date = TryParseDate(message, @"(?i)(January|February|March|April|May|June|July|August|September|October|November|December) \d+");
                (TimeSpan? startTime, TimeSpan? endTime) = TryParseTimeSpan(message, @"(\d+-\d+:\d+)");
                (TimeSpan? utcStartTime, TimeSpan? utcEndTime) = TryParseTimeSpan(message, @"(\d+:\d+-\d+:\d+ UTC)");

                TimeSpan? finalStartTime = utcStartTime;
                TimeSpan? finalEndTime = utcEndTime;
                if (startTime != null && endTime != null && utcStartTime != null && utcEndTime != null && !CompareTimeRanges(startTime, endTime, utcStartTime, utcEndTime))
                {
                    Logger.Debug($"The time range isn't same. It should be Kael's problem.");
                }
                (finalStartTime, finalEndTime) = ConvertToLocalTime(finalStartTime, finalEndTime);
                return (date, finalStartTime, finalEndTime);
            });
        }

        private static bool CompareTimeRanges(TimeSpan? timeSpan0, TimeSpan? timeSpan1, TimeSpan? timeSpan2, TimeSpan? timeSpan3)
        {
            DateTime utcTimeSpan0 = DateTime.UtcNow.Date.Add(timeSpan0 ?? TimeSpan.Zero);
            DateTime utcTimeSpan1 = DateTime.UtcNow.Date.Add(timeSpan1 ?? TimeSpan.Zero);
            return utcTimeSpan0 < utcTimeSpan1;
        }

        private static (TimeSpan?, TimeSpan?) ConvertToLocalTime(TimeSpan? timeSpan0, TimeSpan? timeSpan1)
        {
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            DateTime utcDateTime0 = DateTime.SpecifyKind(DateTime.UtcNow.Date.Add(timeSpan0 ?? TimeSpan.Zero), DateTimeKind.Utc);
            DateTime utcDateTime1 = DateTime.SpecifyKind(DateTime.UtcNow.Date.Add(timeSpan1 ?? TimeSpan.Zero), DateTimeKind.Utc);

            DateTime localDateTime0 = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime0, localTimeZone);
            DateTime localDateTime1 = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime1, localTimeZone);

            return (localDateTime0.TimeOfDay, localDateTime1.TimeOfDay);
        }

        private static DateTime? TryParseDate(string input, string pattern)
        {
            Match match = Regex.Match(input, pattern);
            return match.Success && DateTime.TryParse(match.Value, out DateTime temp) ? temp : null;
        }

        private static (TimeSpan?, TimeSpan?) TryParseTimeSpan(string input, string pattern)
        {
            Match match = Regex.Match(input, pattern);
            if (match.Success)
            {
                string[] times = match.Value.Split('-');
                return (TimeSpan.TryParse(times[0].Trim(), out TimeSpan start) ? start : (TimeSpan?)null,
                        TimeSpan.TryParse(times[1].Trim(), out TimeSpan end) ? end : (TimeSpan?)null);
            }
            return (null, null);
        }
    } 
}