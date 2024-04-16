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
        private const int MaxRetryCount = 3;

        private static int _retryCount = 0;

        public static async Task<List<EventInfo>> GetRecentEventsAsync()
        {
            try
            {
                string[] scopes = { CalendarService.Scope.CalendarReadonly };
                string fileName = "credentials.json";
                string credPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                UserCredential credential;

                using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
                {
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
                }


                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "STO Server Checker"
                });

                EventsResource.ListRequest request = service.Events.List("uhio1bvtudq50n2qhfeo98iduo@group.calendar.google.com");
                request.TimeMinDateTimeOffset = DateTime.UtcNow;
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
                else
                {
                    List<EventInfo> eventInfos = new List<EventInfo>();

                    foreach (var eventItem in events.Items)
                    {
                        EventInfo eventInfo = new EventInfo();

                        string start = "";
                        string end = "";
                        string timeTillEnd = "";
                        string timeTillStart = "";

                        if (eventItem.Start.DateTime != null)
                        {
                            DateTimeOffset startTime = eventItem.Start.DateTimeDateTimeOffset.Value.ToLocalTime();
                            DateTimeOffset endTime1 = eventItem.End.DateTimeDateTimeOffset.Value.ToLocalTime();
                            start = startTime.ToString("yyyy-MM-dd");
                            end = endTime1.ToString("yyyy-MM-dd");

                            if (DateTime.UtcNow < startTime)
                            {
                                TimeSpan timeFromStart = startTime - DateTime.UtcNow;
                                timeTillStart = $"{(int)timeFromStart.TotalDays} days.";
                            }
                            else
                            {
                                if (eventItem.End.DateTime != null)
                                {
                                    DateTimeOffset endTime = eventItem.End.DateTimeDateTimeOffset.Value.ToLocalTime();
                                    end = endTime.ToString("yyyy-MM-dd");

                                    if (DateTime.UtcNow < endTime)
                                    {
                                        TimeSpan timeUntilEnd = endTime - DateTime.UtcNow;
                                        timeTillEnd = $"{(int)timeUntilEnd.TotalDays} days.";
                                    }
                                    else
                                    {
                                        timeTillEnd = "Event Ended";
                                    }
                                }
                            }
                        }
                        else
                        {
                            start = eventItem.Start.Date;
                            end = "All-Day Event";
                        }

                        eventInfo.StartDate = start;
                        eventInfo.EndDate = end;
                        eventInfo.Summary = eventItem.Summary;
                        eventInfo.TimeTillStart = timeTillStart;
                        eventInfo.TimeTillEnd = timeTillEnd;

                        eventInfos.Add(eventInfo);
                    }

                    return eventInfos;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}, {ex.StackTrace}");
                if (_retryCount < MaxRetryCount)
                {
                    _retryCount++;
                    await Task.Delay(1000);
                    return await GetRecentEventsAsync();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}