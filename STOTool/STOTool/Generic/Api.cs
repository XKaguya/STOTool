using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using STOTool.Class;
using STOTool.Enum;
using STOTool.Feature;

namespace STOTool.Generic
{
    public class Api
    {
        private static ProgramLevel ProgramLevel { get; set; } = ProgramLevel.Normal;
        
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

        private static void UpdateServerStatus(ShardStatus status)
        {
            try
            {
                if (App.MainWindowInstance != null)
                {
                    if (status == ShardStatus.Up)
                    {
                        App.MainWindowInstance.BackGround.ImageSource = new BitmapImage( new Uri("pack://application:,,,/STOTool;component/Background/Bg_Up.png"));
                    }
                    else
                    {
                        App.MainWindowInstance.BackGround.ImageSource = new BitmapImage(new Uri("pack://application:,,,/STOTool;component/Background/Bg_Down.png"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
                throw;
            }
        }

        private static ShardStatus ConvertMaintenanceTimeToShardStatus(MaintenanceTimeType maintenanceTime)
        {
            switch (maintenanceTime)
            {
                case MaintenanceTimeType.Maintenance:
                    return ShardStatus.Maintenance;
                case MaintenanceTimeType.WaitingForMaintenance:
                    return ShardStatus.Up;
                case MaintenanceTimeType.None:
                    return ShardStatus.Up;
                default:
                    return ShardStatus.None;
            }
        }

        private static string MaintenanceInfoToString(MaintenanceInfo maintenanceInfo)
        {
            string result = "";

            if (maintenanceInfo.ShardStatus == MaintenanceTimeType.Maintenance)
            {
                result = $"The server is currently under maintenance. ETA finishes in {maintenanceInfo.Days} days {maintenanceInfo.Hours} hours {maintenanceInfo.Minutes} minutes {maintenanceInfo.Seconds} seconds.";
            }
            else if (maintenanceInfo.ShardStatus == MaintenanceTimeType.MaintenanceEnded)
            {
                result = "The maintenance has ended.";
            }
            else if (maintenanceInfo.ShardStatus == MaintenanceTimeType.WaitingForMaintenance)
            {
                result = $"The server is waiting for maintenance. ETA starts in {maintenanceInfo.Days} days {maintenanceInfo.Hours} hours {maintenanceInfo.Minutes} minutes {maintenanceInfo.Seconds} seconds.";
            }
            else
            {
                result = "";
            }

            return result;
        }
        
        public static async Task UpdatePerSecond()
        {
            try
            {
                List<EventInfo> calendarInfo = await Feature.Calendar.GetRecentEventsAsync();
                List<NewsInfo> newsInfo = await NewsProcessor.GetNewsContentsAsync();
                MaintenanceInfo maintenanceInfo = await ServerStatus.CheckServerAsync();

                App.MainWindowInstance.Dispatcher.InvokeAsync(() =>
                {
                    UpdateNews(newsInfo);
                    UpdateCalendar(calendarInfo);
                    UpdateMaintenance(maintenanceInfo);
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }

        private static void UpdateNews(List<NewsInfo> newsInfo)
        {
            try
            {
                Logger.Info("Updating news info.");
                
                for (int i = 0; i < newsInfo.Count && i < 9; i++)
                {
                    string textBlockName = "NewsTitle" + (i + 1);
                    TextBlock textBlock = App.MainWindowInstance.FindName(textBlockName) as TextBlock;
                    if (textBlock.Text != newsInfo[i].Title)
                    {
                        textBlock.Text = newsInfo[i].Title;
                    }

                    string imageSourceName = "News" + (i + 1);
                    Image image = App.MainWindowInstance.FindName(imageSourceName) as Image;
                    
                    BitmapImage bitmapImage = new BitmapImage(new Uri(newsInfo[i].ImageUrl, UriKind.Absolute));
                    
                    if (image.Source != bitmapImage)
                    {
                        image.Source = bitmapImage;
                    }
                }
            }   
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }

        private static void UpdateCalendar(List<EventInfo> calendarInfo)
        {
            try
            {
                Logger.Info("Updating calendar info.");
                
                for (int i = 0; i < calendarInfo.Count && i < 3; i++)
                {
                    string titleName = "RecentNewsTitle" + (i + 1);
                    string startTimeName = "RecentNewsStartTime" + (i + 1);
                    string endTimeName = "RecentNewsEndTime" + (i + 1);

                    TextBlock titleBlock = App.MainWindowInstance.FindName(titleName) as TextBlock;
                    TextBlock startTimeBlock = App.MainWindowInstance.FindName(startTimeName) as TextBlock;
                    TextBlock endTimeBlock = App.MainWindowInstance.FindName(endTimeName) as TextBlock;

                    if (calendarInfo.Count > i)
                    {
                        if (titleBlock.Text != calendarInfo[i].Summary)
                        {
                            titleBlock.Text = calendarInfo[i].Summary;
                        }

                        if (startTimeBlock.Text != calendarInfo[i].StartDate)
                        {
                            startTimeBlock.Text = $"Start Date: {calendarInfo[i].StartDate}";
                        }

                        if (endTimeBlock.Text != calendarInfo[i].EndDate)
                        {
                            endTimeBlock.Text = $"End Date: {calendarInfo[i].EndDate}";
                        }
                    }
                    else
                    {
                        titleBlock.Text = "";
                        startTimeBlock.Text = "";
                        endTimeBlock.Text = "";
                    }
                }

            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }
        
        private static void UpdateMaintenance(MaintenanceInfo maintenanceInfo)
        {
            try
            {
                Logger.Info("Updating maintenance info.");
                
                TextBlock maintenanceMessage = App.MainWindowInstance.FindName("MaintenanceInfo") as TextBlock;
                if (maintenanceMessage != null)
                {
                    maintenanceMessage.Text = MaintenanceInfoToString(maintenanceInfo);
                }

                UpdateServerStatus(ConvertMaintenanceTimeToShardStatus(maintenanceInfo.ShardStatus));
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }
    }
}