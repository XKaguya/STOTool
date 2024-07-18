using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using STOTool.Class;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class Calendar
    {
        public static async Task<List<EventInfo>>? GetRecentEventsAsync()
        {
            try
            {
                string[] scopes = { CalendarService.Scope.CalendarReadonly };
                string fileName = "credentials.json";
                string credPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                UserCredential credential;

                using var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read);
                var secrets = await GoogleClientSecrets.FromStreamAsync(stream);
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = secrets.Secrets.ClientId,
                        ClientSecret = secrets.Secrets.ClientSecret
                    },
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));

                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "STO Server Checker"
                });

                EventsResource.ListRequest request = service.Events.List("uhio1bvtudq50n2qhfeo98iduo@group.calendar.google.com");
                request.TimeMin = DateTime.UtcNow;
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 10;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                Events events = await request.ExecuteAsync();

                if (events.Items == null || events.Items.Count == 0)
                {
                    Logger.Info("No upcoming events found.");
                    return null;
                }

                List<EventInfo> eventInfos = new List<EventInfo>();

                foreach (var eventItem in events.Items)
                {
                    EventInfo eventInfo = new EventInfo
                    {
                        StartDate = eventItem.Start.DateTime?.ToLocalTime().ToString("yyyy-MM-dd") ?? eventItem.Start.Date,
                        EndDate = eventItem.End.DateTime?.ToLocalTime().ToString("yyyy-MM-dd") ?? "All-Day Event",
                        Summary = eventItem.Summary
                    };

                    if (eventItem.Start.DateTime.HasValue && DateTime.UtcNow < eventItem.Start.DateTime.Value)
                    {
                        TimeSpan timeFromStart = eventItem.Start.DateTime.Value - DateTime.UtcNow;
                        eventInfo.TimeTillStart = $"{(int)timeFromStart.TotalDays} days.";
                    }

                    if (eventItem.End.DateTime.HasValue)
                    {
                        DateTimeOffset endTime = eventItem.End.DateTime.Value.ToLocalTime();
                        if (DateTime.UtcNow < endTime)
                        {
                            TimeSpan timeUntilEnd = endTime - DateTime.UtcNow;
                            eventInfo.TimeTillEnd = $"{(int)timeUntilEnd.TotalDays} days.";
                        }
                        else
                        {
                            eventInfo.TimeTillEnd = "Event Ended";
                        }
                    }

                    eventInfos.Add(eventInfo);
                }

                return eventInfos;
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}, {ex.StackTrace}");
                return new List<EventInfo>();
            }
        }
    }
}
