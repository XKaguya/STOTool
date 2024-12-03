using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STOTool.Class;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class AutoNews
    {
        private const string Url = "https://api.arcgames.com/v1.0/games/sto/news";
        private const string NewsNodesFile = "NewsNodes.json";

        private static async Task<string> GetContentAsync()
        {
            var builder = new UriBuilder(Url);
            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("field[]", "images.img_microsite_thumbnail");
            query.Add("field[]", "platforms");
            query.Add("field[]", "updated");
            query.Add("limit", "4");
            query.Add("platform", "pc");

            builder.Query = query.ToString();

            try
            {
                HttpResponseMessage response = await Helper.HttpClient.GetAsync(builder.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                        
                    return result;
                }
                
                return "null";
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("SSL connection", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug($"Cryptic Issue: {ex.Message}");
                }
                else
                {
                    Logger.Error($"Network Issue: {ex.Message}");
                }
                
                return "null";
            }
        }
        
        private static List<NewsInfo> ParseJsonContent(string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    return new List<NewsInfo>();
                }

                JObject json = JObject.Parse(content);
                JArray newsArray = (JArray)json["news"];
        
                if (newsArray == null)
                {
                    return new List<NewsInfo>();
                }

                List<NewsInfo> newsList = new List<NewsInfo>();
                foreach (JObject newsItem in newsArray)
                {
                    string title = newsItem["title"]?.ToString();
                    string imageUrl = newsItem["images"]?["img_microsite_thumbnail"]?["url"]?.ToString();
                    string newsLink = $"https://www.arcgames.com/en/games/star-trek-online/news/detail/{newsItem["id"]}";

                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(imageUrl) && !string.IsNullOrEmpty(newsLink))
                    {
                        newsList.Add(new NewsInfo
                        {
                            Title = title,
                            ImageUrl = imageUrl,
                            NewsLink = newsLink
                        });
                    }
                }

                return newsList;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
                
                return new List<NewsInfo>();
            }
        }
        
        private static async Task<NewsNodes> FetchPageContentAsync()
        {
            string content = await GetContentAsync();

            if (Helper.NullCheck(content))
            {
                Logger.Debug("Might be cryptic network issue. Content is null. Ignore and proceed.");
                return new NewsNodes();
            }
            
            List<NewsInfo> newsInfos = ParseJsonContent(content);

            if (Helper.NullCheck(newsInfos))
            {
                return new NewsNodes();
            }

            NewsNodes result = new NewsNodes();
            if (newsInfos.Count >= 4)
            {
                result.Node0 = newsInfos[0].Title;
                result.Node1 = newsInfos[1].Title;
                result.Node2 = newsInfos[2].Title;
                result.Node3 = newsInfos[3].Title;
                result.Hash = GenerateHash(result);
            }
            
            return result;
        }
        
        private static string GenerateHash(NewsNodes newsNodes)
        {
            using var sha256 = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(newsNodes.Node0 + newsNodes.Node1 + newsNodes.Node2 + newsNodes.Node3);
            var hashBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }

        private static async Task StoreIntoFile(NewsNodes newsData)
        {
            if (Helper.NullCheck(newsData))
            {
                Logger.Error("Network issue detected. Write into file canceled.");
                return;
            }
            
            string json = JsonConvert.SerializeObject(newsData, Formatting.Indented);
            await File.WriteAllTextAsync(NewsNodesFile, json);
        }

        private static async Task<string> CompareHash(NewsNodes currentData)
        {
            if (!File.Exists(NewsNodesFile))
            {
                Logger.Info("No previous data to compare.");
                NewsNodes data = await FetchPageContentAsync();

                if (Helper.NullCheck(data))
                {
                    Logger.Debug("Somehow some nodes are null or empty. Write into file canceled.");
                    return "null";
                }
                
                await StoreIntoFile(data);
                return "null";
            }

            string jsonData = await File.ReadAllTextAsync(NewsNodesFile);
            NewsNodes previousData = JsonConvert.DeserializeObject<NewsNodes>(jsonData);
            
            if (previousData != null && currentData.Hash == previousData.Hash)
            {
                Logger.Debug($"Hash has not changed. Saved: {previousData.Hash} Current: {currentData.Hash}");
                return "null";
            }

            if (previousData != null && currentData.Hash != previousData.Hash)
            {
                Logger.Info($"Hash has changed. Saved: {previousData.Hash} Current: {currentData.Hash}");

                var result = await GetNewsImage.CallScreenshot(0, true);

                Logger.Info($"News Title: {currentData.Node0}, File Length: {result.Length}.");

                return result;
            }

            return "null";
        }

        public static async Task<string> HasHashChanged()
        {
            var currentData = await FetchPageContentAsync();

            if (Helper.NullCheck(currentData))
            {
                Logger.Debug("Current data is null.");
                
                return "null";
            }
            
            string result = await CompareHash(currentData);

            if (result != "null")
            {
                await StoreIntoFile(currentData);

                Logger.Debug($"Result length: {result.Length.ToString()}.");
                
                return result;
            }

            return "null";
        }
    }
}