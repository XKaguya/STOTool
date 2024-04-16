using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using STOTool.Class;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class NewsProcessor
    {
        public static async Task<List<NewsInfo>> GetNewsContentsAsync()
        {
            List<NewsInfo> newsContents = new List<NewsInfo>();

            string url = "https://www.arcgames.com/en/games/star-trek-online/news";
            string baseUrl = "https://www.arcgames.com";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string html = await response.Content.ReadAsStringAsync();
                        HtmlDocument htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(html);
                        
                        List<NewsInfo> parsedNews = ParseHtml(htmlDoc, baseUrl);
                        newsContents.AddRange(parsedNews);
                    }
                    else
                    {
                        Logger.Error($"Request failed with status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error: {ex.Message}");
                throw;
            }

            return newsContents;
        }

        private static List<NewsInfo> ParseHtml(HtmlDocument htmlDoc, string baseUrl)
        {
            List<NewsInfo> newsList = new List<NewsInfo>();

            string newsXPath = "//div[contains(@class, 'news-content') and contains(@class, 'element')]";
            HtmlNodeCollection newsNodes = htmlDoc.DocumentNode.SelectNodes(newsXPath);

            if (newsNodes != null)
            {
                foreach (HtmlNode node in newsNodes)
                {
                    string title = node.SelectSingleNode(".//h2[@class='news-title']")?.InnerText?.Trim() ?? "";
                    string imageUrl = node.SelectSingleNode(".//img[@class='item-img']")?.GetAttributeValue("src", "") ?? "";
                    string newsLink = baseUrl + node.SelectSingleNode(".//a[@class='read-more']")?.GetAttributeValue("href", "") ?? "";
                    string finalTitle = Regex.Replace(title, @"&\w+;", string.Empty);

                    NewsInfo newsContent = new NewsInfo
                    {
                        Title = finalTitle,
                        ImageUrl = imageUrl,
                        NewsLink = newsLink
                    };

                    newsList.Add(newsContent);
                }
            }

            return newsList;
        }
    }
}
