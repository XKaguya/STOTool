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
        private static readonly string GoogleCalendarId = "uhio1bvtudq50n2qhfeo98iduo@group.calendar.google.com";
        
        public static async Task<List<EventInfo>?> GetRecentEventsAsync()
        {
            try
            {
                string[] scopes = { CalendarService.Scope.CalendarReadonly };
                string fileName = "credentials.json";
                string credPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                UserCredential credential;
                
                if (!File.Exists("client_secret.json"))
                {
                    Logger.Critical($"Google Calendar Credential file not exist. Will not proceed unless the file was placed correctly.");
                    return new List<EventInfo>();
                }

                await using var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read);
                
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

                EventsResource.ListRequest request = service.Events.List(GoogleCalendarId);
                request.TimeMinDateTimeOffset = DateTime.Now;
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 10;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                Events events = await request.ExecuteAsync();

                if (events.Items == null || events.Items.Count == 0)
                {
                    Logger.Info("No upcoming events found.");
                    return new List<EventInfo>();
                }

                List<EventInfo> eventInfos = new List<EventInfo>();

                foreach (var eventItem in events.Items)
                {
                    string startDate = eventItem.Start.DateTimeDateTimeOffset.HasValue 
                        ? eventItem.Start.DateTimeDateTimeOffset.Value.ToLocalTime().ToString("yyyy-MM-dd") 
                        : eventItem.Start.Date;
                    
                    string endDate = eventItem.End.DateTimeDateTimeOffset.HasValue 
                        ? eventItem.End.DateTimeDateTimeOffset.Value.ToLocalTime().ToString("yyyy-MM-dd") 
                        : "All-Day Event";

                    EventInfo eventInfo = new EventInfo
                    {
                        StartDate = startDate,
                        EndDate = endDate,
                        Summary = eventItem.Summary
                    };
                    
                    if (eventItem.Start.DateTimeDateTimeOffset.HasValue && DateTime.Now < eventItem.Start.DateTimeDateTimeOffset.Value.ToLocalTime())
                    {
                        TimeSpan timeFromStart = eventItem.Start.DateTimeDateTimeOffset.Value.ToLocalTime() - DateTime.Now;
                        eventInfo.TimeTillStart = $"{(int)timeFromStart.TotalDays} days";
                    }
                    
                    if (eventItem.End.DateTimeDateTimeOffset.HasValue)
                    {
                        DateTimeOffset endTime = eventItem.End.DateTimeDateTimeOffset.Value.ToLocalTime();
                        if (DateTime.Now < endTime)
                        {
                            TimeSpan timeUntilEnd = endTime - DateTime.Now;
                            eventInfo.TimeTillEnd = $"{(int)timeUntilEnd.TotalDays} days";
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
                if (ex.Message.Contains("SSL connection", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug($"Cryptic Issue: {ex.Message}");
                }
                else
                {
                    Logger.Error($"{ex.Message}, {ex.StackTrace}");
                }
                
                return new List<EventInfo>();
            }
        }
    }
}
