using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Playwright;
using STOTool.Class;
using STOTool.Generic;

namespace STOTool.Feature
{
    public class NewsProcessor
    {
        private const string Url = "https://www.playstartrekonline.com/en/news#pc";
        private const string BaseUrl = "https://www.playstartrekonline.com";
        private const string NewsXPath = "//a[@class='news-page__news-post']";

        public static async Task<List<NewsInfo>> GetNews()
        {
            try
            {
#if DEBUG
                string content = File.ReadAllText("debug.txt");
#else
                var content = await FetchPageContentAsync();
#endif
                return ParseHtmlContent(content);
            }
            catch (HttpRequestException e)
            {
                Logger.Error($"Error in parsing HTML: {e.Message}\n{e.StackTrace}");
                return new List<NewsInfo>();
            }
        }

        private static async Task<string> FetchPageContentAsync()
        {
            var page = await Helper.Browser.NewPageAsync();
            
            try
            {
                await page.GotoAsync(Url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                async Task ClickViewMoreAsync()
                {
                    await page.EvaluateAsync(
                        "document.querySelector('span.news-page__view-more[role=\"button\"]').click();");
                    await page.WaitForTimeoutAsync(500);
                }

                await ClickViewMoreAsync();
                await ClickViewMoreAsync();

                string content = await page.ContentAsync();
                await page.CloseAsync();

                return content;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                await page.CloseAsync();
                return null;
            }
        }

        private static List<NewsInfo> ParseHtmlContent(string content)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);

            List<NewsInfo> newsList = new List<NewsInfo>();
            HtmlNodeCollection newsNodes = htmlDoc.DocumentNode.SelectNodes(NewsXPath);

            if (newsNodes != null)
            {
                int count = 0;
                foreach (HtmlNode node in newsNodes)
                {
                    try
                    {
                        if (count >= 9)
                        {
                            break;
                        }

                        string title = HttpUtility.HtmlDecode(node.SelectSingleNode(".//h3[contains(@class, 'news-page__news-post-title')]")?.InnerText.Trim());
                        string imageUrl = node.SelectSingleNode(".//image")?.GetAttributeValue("xlink:href", string.Empty);
                        string newsLink = node.GetAttributeValue("href", string.Empty);

                        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(imageUrl) && !string.IsNullOrEmpty(newsLink))
                        {
                            newsList.Add(new NewsInfo
                            {
                                Title = title,
                                ImageUrl = imageUrl,
                                NewsLink = new Uri(new Uri(BaseUrl), newsLink).ToString()
                            });
                        }

                        count++;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.Message + e.StackTrace);
                    }
                }
            }

            return newsList;
        }
    }
}