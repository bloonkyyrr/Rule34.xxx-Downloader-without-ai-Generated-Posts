using HtmlAgilityPack;
using R34Downloader.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace R34Downloader.Services
{
    /// <summary>
    /// HTML parsing service.
    /// </summary>
    public static class R34HtmlService
    {
        #region Fields
        private const string ContentUrl = "https://rule34.xxx/index.php?page=post&s=list&tags=";

        private const byte PageSize = 42;

        #endregion

        #region Methods

        /// <summary>
        /// Checks for the presence of content for the specified tags.
        /// </summary>
        /// <param name="tags">Tags.</param>
        /// <returns>Return True if any content is found, otherwise False.</returns>
        public static bool IsSomethingFound(string tags)
        {
            var document = LoadHtmlDocument($"{ContentUrl}{tags}");
            var nodes = document.DocumentNode.SelectNodes("//div[@class='content']//span[@class='thumb']");

            return nodes != null;
        }

        /// <summary>
        /// Returns the maximum page for the specified tags.
        /// </summary>
        /// <param name="tags">Tags.</param>
        /// <returns>Returns the maximum page or 0 if nothing is found.</returns>
        public static int GetMaxPid(string tags)
        {
            var document = LoadHtmlDocument($"{ContentUrl}{tags}");
            var nodes = document.DocumentNode.SelectSingleNode("//div[@class='pagination']//a[@alt='last page']");
            var pidString = nodes?.GetAttributeValue("href", null);

            if (pidString == null)
            {
                return default;
            }

            var maxPid = pidString.Substring(pidString.LastIndexOf('=') + 1, pidString.Length - pidString.LastIndexOf('=') - 1);

            return Convert.ToInt32(maxPid);
        }

        /// <summary>
        /// Returns the amount of content on the specified page for the specified tags.
        /// </summary>
        /// <param name="tags">Tags.</param>
        /// <param name="pid">Page.</param>
        /// <returns>Returns the amount of content on the page, or -1 if nothing is found.</returns>
        public static int GetCountContent(string tags, int pid)
        {
            var document = LoadHtmlDocument($"{ContentUrl}{tags}&pid={pid}");
            var nodes = document.DocumentNode.SelectNodes("//div[@class='content']//span[@class='thumb']/a");

            if (nodes != null && pid == 0)
            {
                return nodes.Count;
            }

            if (nodes != null && pid != 0)
            {
                return pid + nodes.Count;
            }

            return ushort.MaxValue;
        }

        /// <summary>
        /// Downloads the specified content in the specified quantity.
        /// </summary>
        /// <param name="path">Path to save files.</param>
        /// <param name="tags">Tags.</param> 
        /// <param name="quantity">Quantity.</param>
        /// <param name="progress"><see cref="IProgress{T}"/></param>
        /// <param name="progress2"><see cref="IProgress{T}"/></param>
        public static void DownloadContent(string path, string tags, ushort quantity, IProgress<int> progress, IProgress<int> progress2)
        {
            var maxPages = quantity;
            ushort residue = PageSize;

            if (quantity < PageSize)
            {
                maxPages = PageSize;
                residue = quantity;
            }

            for (var pid = 0; pid < maxPages; pid += PageSize)
            {
                DownloadControlService.WaitIfPaused();

                var document = LoadHtmlDocument($"{ContentUrl}{tags}&pid={pid}");
                var nodes = document.DocumentNode.SelectNodes("//div[@class='content']//span[@class='thumb']/a");

                var posts = nodes.Select(x => x.GetAttributeValue("href", "").Replace("&amp;", "&"))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray();

                DownloadPosts(posts, path, pid, residue, maxPages, progress, progress2);
            }
        }

        #endregion

        #region Helpers

        private static void DownloadPosts(string[] posts, string path, int pid, int residue, int maxPages, IProgress<int> progress, IProgress<int> progress2)
        {
            var maxPosts = posts.Length;
            if (maxPages - pid < PageSize)
            {
                maxPosts = maxPages - pid;
            }
            else if (maxPages - pid == PageSize)
            {
                maxPosts = residue;
            }

            for (var i = 0; i < maxPosts; i++)
            {
                DownloadControlService.WaitIfPaused();

                var postId = ExtractPostId(posts[i]);
                if (IsPostAlreadyDownloaded(path, postId))
                {
                    var skippedStatus = pid + i + 1;
                    progress.Report(skippedStatus);
                    progress2.Report(skippedStatus);
                    continue;
                }

                var document = LoadHtmlDocument($"https://rule34.xxx/{posts[i]}");

                var videoNode = document.DocumentNode.SelectSingleNode("//video[@id='gelcomVideoPlayer']/source");
                var imageNode = document.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

                if (videoNode != null && SettingsModel.Video)
                {
                    var videoUrl = videoNode.GetAttributeValue("src", null);
                    if (videoUrl != null)
                    {
                        var filename = Path.GetFileName(videoUrl);
                        var questionMarkIndex = filename.IndexOf('?');
                        var id = questionMarkIndex > 0 ? filename.Substring(0, questionMarkIndex) : filename;
                        if (questionMarkIndex > 0)
                        {
                            filename = Path.GetFileName(filename.Substring(0, questionMarkIndex));
                        }

                        DownloadService.Download(videoUrl, Path.Combine(path, "Video", filename), id, "Video");
                    }
                }
                else
                {
                    var imageUrl = imageNode?.GetAttributeValue("content", null);
                    if (imageUrl != null)
                    {
                        var id = imageUrl.Split('?')[1];
                        imageUrl = imageUrl.Substring(0, imageUrl.LastIndexOf('?'));
                        var filename = $"{id}{Path.GetExtension(imageUrl)}";

                        if (filename.Contains(".gif") && SettingsModel.Gif)
                        {
                            DownloadService.Download(imageUrl, Path.Combine(path, "Gif", filename), id, "Gif");
                        }
                        else if (!filename.Contains(".gif") && SettingsModel.Images)
                        {
                            DownloadService.Download(imageUrl, Path.Combine(path, "Images", filename), id, "Image");
                        }
                    }
                }

                var reportStatus = pid + i + 1;
                progress.Report(reportStatus);
                progress2.Report(reportStatus);

                PauseAwareDelay(100);
            }
        }

        private static void PauseAwareDelay(int milliseconds)
        {
            var elapsed = 0;
            while (elapsed < milliseconds)
            {
                DownloadControlService.WaitIfPaused();
                var step = Math.Min(50, milliseconds - elapsed);
                Thread.Sleep(step);
                elapsed += step;
            }
        }

        private static string ExtractPostId(string postHref)
        {
            if (string.IsNullOrEmpty(postHref))
            {
                return null;
            }

            const string idMarker = "id=";
            var idIndex = postHref.IndexOf(idMarker, StringComparison.OrdinalIgnoreCase);
            if (idIndex < 0)
            {
                return null;
            }

            var idStart = idIndex + idMarker.Length;
            var idEnd = postHref.IndexOf('&', idStart);
            return idEnd >= 0 ? postHref.Substring(idStart, idEnd - idStart) : postHref.Substring(idStart);
        }

        private static bool IsPostAlreadyDownloaded(string savePath, string postId)
        {
            if (string.IsNullOrEmpty(postId))
            {
                return false;
            }

            var subFolders = new[] { "Images", "Gif", "Video" };
            foreach (var subFolder in subFolders)
            {
                var folder = Path.Combine(savePath, subFolder);
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                if (Directory.EnumerateFiles(folder, postId + ".*").Any())
                {
                    return true;
                }
            }

            return false;
        }

        private static HtmlDocument LoadHtmlDocument(string url)
        {
            var htmlWeb = new HtmlWeb
            {
                PreRequest = request =>
                {
                    if (request != null)
                    {
                        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
                        request.Referer = "https://rule34.xxx/";
                        request.Host = "rule34.xxx";

                        request.CookieContainer = new CookieContainer();

                        request.CookieContainer.Add(new Cookie
                        {
                            Name = "gdpr",
                            Value = "1",
                            Domain = ".rule34.xxx",
                            Path = "/",
                            Expires = DateTime.Now.AddYears(1)
                        });

                        request.CookieContainer.Add(new Cookie
                        {
                            Name = "gdpr-consent",
                            Value = "1",
                            Domain = ".rule34.xxx",
                            Path = "/",
                            Expires = DateTime.Now.AddYears(1)
                        });
                    }

                    return true;
                }
            };

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    return htmlWeb.Load(url);
                }
                catch (Exception)
                {
                    if (attempt == 3) throw;
                    System.Threading.Thread.Sleep(500 * attempt);
                }
            }

            return null;
        }

        #endregion
    }
}
    