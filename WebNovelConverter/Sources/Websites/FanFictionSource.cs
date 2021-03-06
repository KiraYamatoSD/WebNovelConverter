﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using WebNovelConverter.Sources.Models;
using WebNovelConverter.Sources.Helpers;

namespace WebNovelConverter.Sources.Websites
{
    public class FanFictionSource : WebNovelSource
    {
        public override string BaseUrl => "https://fanfiction.net/";

        public override List<Mode> AvailableModes => new List<Mode> { Mode.TableOfContents };

        public FanFictionSource() : base("Fanfiction")
        {
        }

        protected FanFictionSource(string type) : base(type)
        {
        }

        public override async Task<IEnumerable<ChapterLink>> GetChapterLinksAsync(string baseUrl, CancellationToken token = default(CancellationToken))
        {
            string baseContent = await GetWebPageAsync(baseUrl, token);

            IHtmlDocument doc = await Parser.ParseAsync(baseContent, token);

            List<ChapterLink> chapterLinks = new List<ChapterLink>();
            foreach (var option in doc.DocumentElement.QuerySelectorAll("select#chap_select option"))
            {
                var chapterNr = option.GetAttribute("value");
                var name = option.GetInnerText().Substring(chapterNr.Length + 2);

                chapterLinks.Add(new ChapterLink
                {
                    Name = name,
                    Url = Regex.Replace(baseUrl.ToLower(),"(" + Regex.Escape(new Uri(BaseUrl).Host.ToLower()) + "/s/[0-9]+)/([0-9]+)/", "$1/" + chapterNr + "/")
                });
            }

            return chapterLinks;
        }

        public override async Task<WebNovelChapter> GetChapterAsync(ChapterLink link,
            ChapterRetrievalOptions options = default(ChapterRetrievalOptions),
            CancellationToken token = default(CancellationToken))
        {
            string content = await GetWebPageAsync(link.Url, token);
            IHtmlDocument doc = await Parser.ParseAsync(content, token);

            return new WebNovelChapter()
            {
                Content = new ContentCleanup(BaseUrl).Execute(doc, doc.QuerySelector("#storytext"))
            };
        }

        public override async Task<WebNovelInfo> GetNovelInfoAsync(string baseUrl, CancellationToken token = default(CancellationToken))
        {
            string baseContent = await GetWebPageAsync(baseUrl, token);

            IHtmlDocument doc = await Parser.ParseAsync(baseContent, token);

            var title = doc.QuerySelector("#profile_top b.xcontrast_txt")?.GetInnerText();

            return new WebNovelInfo
            {
                Title = title
            };
        }

    }
}
