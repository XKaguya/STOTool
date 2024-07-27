using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using STOTool.Class;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class NewsProcessor
    {
        private const string Url = "https://api.arcgames.com/v1.0/games/sto/news";

        public static async Task<List<NewsInfo>> GetNews()
        {
            try
            {
                var content = await FetchPageContentAsync();
                var result = ParseJsonContent(content);

                if (Helper.NullCheck(result))
                {
                    return new List<NewsInfo>();
                }

                return result;
            }
            catch (HttpRequestException e)
            {
                Logger.Error($"Error in parsing HTML: {e.Message}\n{e.StackTrace}");
                return new List<NewsInfo>();
            }
        }

        private static async Task<string> FetchPageContentAsync()
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var builder = new UriBuilder(Url);
                    var query = HttpUtility.ParseQueryString(string.Empty);
                    query.Add("field[]", "images.img_microsite_thumbnail");
                    query.Add("field[]", "platforms");
                    query.Add("field[]", "updated");
                    query.Add("limit", "9");
                    query.Add("platform", "pc");

                    builder.Query = query.ToString();

                    HttpResponseMessage response = await Helper.HttpClient.GetAsync(builder.ToString());
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();

                        Logger.Trace(result);
                        
                        return result;
                    }
                    else
                    {
                        return "null";
                    }
                }
                catch (TimeoutException ex)
                {
                    Logger.Error($"Timeout Exception: {ex.Message}");
                    DrawNewsImage.Tips = "Official website might down. This is Cryptic's issue.";
                }
                catch (Exception e)
                {
                    Logger.Error($"General Exception: {e.Message} {e.StackTrace}");
                }

                retryCount++;
                if (retryCount < maxRetries)
                {
                    Logger.Info("Retrying...");
                    await Task.Delay(2000);
                }
            }

            Logger.Error("Failed to fetch page content after maximum retries.");
            return "null";
        }

        private static List<NewsInfo> ParseJsonContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            JObject json = JObject.Parse(content);
            JArray newsArray = (JArray)json["news"];
        
            if (newsArray == null)
            {
                return null;
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
    }
}