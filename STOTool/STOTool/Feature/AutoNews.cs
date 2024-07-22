using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Playwright;
using Newtonsoft.Json;
using STOTool.Class;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class AutoNews
    {
        private const string Url = "https://www.playstartrekonline.com/en/news#pc";
        private const string NewsNodesFile = "NewsNodes.json";
        private const string NewsXPath = "//a[@class='news-page__news-post']";

        private static async Task<HtmlNodeCollection> GetContentAsync()
        {
            var page = await Helper.Browser.NewPageAsync();
            
            try
            {
                await page.GotoAsync(Url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                string content = await page.ContentAsync();
                await page.CloseAsync();

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);

                HtmlNodeCollection newsNodes = htmlDoc.DocumentNode.SelectNodes(NewsXPath);

                return newsNodes;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                await page.CloseAsync();
                return null;
            }
        }

        private static string ExtractTitle(HtmlNode htmlNode)
        {
            string title = HttpUtility.HtmlDecode(htmlNode.SelectSingleNode(".//h3[contains(@class, 'news-page__news-post-title')]")?.InnerText.Trim());
            return title;
        }
        
        private static async Task<NewsNodes> FetchPageContentAsync()
        {
            HtmlNodeCollection newsNodes = await GetContentAsync();

            NewsNodes result = new NewsNodes();
            if (newsNodes != null && newsNodes.Count >= 4)
            {
                result.Node0 = ExtractTitle(newsNodes[0]);
                result.Node1 = ExtractTitle(newsNodes[1]);
                result.Node2 = ExtractTitle(newsNodes[2]);
                result.Node3 = ExtractTitle(newsNodes[3]);
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
            string json = JsonConvert.SerializeObject(newsData, Formatting.Indented);
            await File.WriteAllTextAsync(NewsNodesFile, json);
        }

        private static async Task<string>? CompareHash(NewsNodes currentData)
        {
            if (!File.Exists(NewsNodesFile))
            {
                Logger.Info("No previous data to compare.");
                var data = await FetchPageContentAsync();

                if (Helper.NullCheck(data))
                {
                    Logger.Debug("Somehow some nodes are null or empty. Write into file canceled.");
                    return "null";
                }
                
                await StoreIntoFile(data);
                return "null";
            }

            string jsonData = await File.ReadAllTextAsync(NewsNodesFile);
            var previousData = JsonConvert.DeserializeObject<NewsNodes>(jsonData);

            if (previousData != null && currentData.Hash == previousData.Hash)
            {
                Logger.Info("Data has not changed.");
                return "null";
            }
            else
            {
                await Cache.RemoveAll();
                
                Logger.Info("Data has changed.");
                var result = await GetNewsImage.CallScreenshot(0);
                
                return result;
            }
        }

        public static async Task<string> HasHashChanged()
        {
            var currentData = await FetchPageContentAsync();
            var result = await CompareHash(currentData);

            if (result != "null")
            {
                await StoreIntoFile(currentData);

                Logger.Debug("Due to news has updated, Force refresh all caches.");
                
                return result;
            }

            return "null";
        }
    }
}