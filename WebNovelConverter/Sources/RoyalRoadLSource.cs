﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using WebNovelConverter.Sources.Models;

namespace WebNovelConverter.Sources
{
    public class RoyalRoadLSource : WebNovelSource
    {
        public override string BaseUrl => "http://royalroadl.com/";

        public RoyalRoadLSource() : base("RoyalRoadL")
        {
        }

        public override async Task<IEnumerable<ChapterLink>> GetChapterLinksAsync(string baseUrl, CancellationToken token = default(CancellationToken))
        {
            string baseContent = await GetWebPageAsync(baseUrl, token);

            IHtmlDocument doc = await Parser.ParseAsync(baseContent, token);

            var chapterElements = from element in doc.All
                                  where element.LocalName == "li"
                                  where element.HasAttribute("class")
                                  let classAttrib = element.GetAttribute("class")
                                  where classAttrib.Contains("chapter")
                                  select element;

            return CollectChapterLinks(baseUrl, chapterElements);
        }

        protected override IEnumerable<ChapterLink> CollectChapterLinks(string baseUrl, IEnumerable<IElement> linkElements, Func<IElement, bool> linkFilter = null)
        {
            foreach (IElement chapterElement in linkElements)
            {
                IElement linkElement = chapterElement.Descendents<IElement>().FirstOrDefault(p => p.LocalName == "a");

                if (linkElement == null || !linkElement.HasAttribute("title") || !linkElement.HasAttribute("href"))
                    continue;

                string title = linkElement.GetAttribute("title");

                ChapterLink link = new ChapterLink
                {
                    Name = title,
                    Url = linkElement.GetAttribute("href"),
                    Unknown = false
                };

                yield return link;
            }
        }

        public override async Task<WebNovelChapter> GetChapterAsync(ChapterLink link, 
            ChapterRetrievalOptions options = default(ChapterRetrievalOptions),
            CancellationToken token = default(CancellationToken))
        {
            string pageContent = await GetWebPageAsync(link.Url, token);
            
            IHtmlDocument doc = await Parser.ParseAsync(pageContent, token);
            
            IElement firstPostElement = (from e in doc.All
                                        where e.LocalName == "div"
                                        where e.HasAttribute("class")
                                        let classAttribute = e.GetAttribute("class")
                                        where classAttribute.Contains("post_body")
                                        select e).FirstOrDefault();

            if (firstPostElement == null)
                return null;

            RemoveNavigation(firstPostElement);

            return new WebNovelChapter
            {
                Url = link.Url,
                Content = firstPostElement.InnerHtml
            };
        }

        public override async Task<string> GetNovelCoverAsync(string baseUrl, CancellationToken token = default(CancellationToken))
        {
            string baseContent = await GetWebPageAsync(baseUrl, token);

            IHtmlDocument doc = await Parser.ParseAsync(baseContent, token);

            return doc.GetElementById("fiction-header").Descendents<IElement>().FirstOrDefault(p => p.LocalName == "img")?.GetAttribute("src");
        }

        protected virtual void RemoveNavigation(IElement rootElement)
        {
            rootElement.Descendents<IElement>().LastOrDefault(p => p.LocalName == "table")?.Remove();
        }
    }
}