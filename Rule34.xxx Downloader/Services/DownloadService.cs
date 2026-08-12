using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using R34Downloader.Models;

namespace R34Downloader.Services
{
    /// <summary>
    /// Download service.
    /// </summary>
    public static class DownloadService
    {
        #region Fields

        private static readonly CookieContainer CookieContainer;
        private static readonly HttpClient Client;

        /// <summary>
        /// Action to report download progress (downloaded MB, total MB).
        /// </summary>
        public static Action<double, double> OnDownloadProgress { get; set; }

        #endregion

        #region Constructors

        static DownloadService()
        {
            CookieContainer = new CookieContainer();

            // Standard GDPR cookies
            CookieContainer.Add(new Cookie("gdpr", "1", "/", ".rule34.xxx"));
            CookieContainer.Add(new Cookie("gdpr-consent", "1", "/", ".rule34.xxx"));

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = CookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            Client = new HttpClient(handler);
            Client.Timeout = TimeSpan.FromSeconds(300); // 5 minute timeout for large files
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            Client.DefaultRequestHeaders.Referrer = new Uri("https://rule34.xxx/");
            Client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");

            // Optimization for many connections
            ServicePointManager.DefaultConnectionLimit = 10;
            ServicePointManager.Expect100Continue = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Downloads and saves a file at the specified path.
        /// Skips the download when a file already exists at the target path.
        /// </summary>
        /// <param name="url">File url.</param>
        /// <param name="filePath">File path with name.</param>
        public static void Download(string url, string filePath)
        {
            Download(url, filePath, null, null);
        }

        /// <summary>
        /// Downloads and saves a file at the specified path with logging.
        /// Skips the download when a file already exists at the target path.
        /// </summary>
        /// <param name="url">File url.</param>
        /// <param name="filePath">File path with name.</param>
        /// <param name="id">Media ID for logging.</param>
        /// <param name="type">Media type (Video, Image, Gif) for logging.</param>
        public static void Download(string url, string filePath, string id, string type)
        {
            DownloadControlService.WaitIfPaused();

            var stopwatch = Stopwatch.StartNew();
            string status = "Downloaded";
            double fileSizeMB = 0;
            bool shouldLog = true;

            try
            {
                if (File.Exists(filePath))
                {
                    stopwatch.Stop();
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(type))
                    {
                        DownloadLogService.AddLog(id, type, "Skipped", stopwatch.Elapsed.TotalSeconds, 0);
                    }
                    shouldLog = false;
                    return;
                }

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Simple retry logic (3 attempts)
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    DownloadControlService.WaitIfPaused();

                    try
                    {
                        var response = Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;
                        response.EnsureSuccessStatusCode();

                        // Get content length from headers
                        long? contentLength = response.Content.Headers.ContentLength;

                        // Check file size limit before downloading
                        if (contentLength.HasValue && contentLength.Value > 0)
                        {
                            double fileSizeMBCheck = contentLength.Value / (1024.0 * 1024.0);

                            // If file size limit is enabled and file exceeds limit, skip it
                            if (SettingsModel.EnableFileSizeLimit && SettingsModel.MaxFileSizeMB > 0 && fileSizeMBCheck > SettingsModel.MaxFileSizeMB)
                            {
                                stopwatch.Stop();
                                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(type))
                                {
                                    DownloadLogService.AddLog(id, type, "Skipped", stopwatch.Elapsed.TotalSeconds, fileSizeMBCheck);
                                }
                                shouldLog = false;
                                return;
                            }
                        }

                        // If we have content length, use streaming with progress reporting
                        if (contentLength.HasValue && contentLength.Value > 0)
                        {
                            using (var contentStream = response.Content.ReadAsStreamAsync().Result)
                            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: false))
                            {
                                long totalBytes = contentLength.Value;
                                long downloadedBytes = 0;
                                byte[] buffer = new byte[8192];
                                int bytesRead;

                                while ((bytesRead = contentStream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    DownloadControlService.WaitIfPaused();

                                    fileStream.Write(buffer, 0, bytesRead);
                                    downloadedBytes += bytesRead;

                                    // Report progress
                                    double downloadedMB = downloadedBytes / (1024.0 * 1024.0);
                                    double totalMB = totalBytes / (1024.0 * 1024.0);
                                    OnDownloadProgress?.Invoke(downloadedMB, totalMB);
                                }

                                fileSizeMB = (double)totalBytes / (1024.0 * 1024.0);
                            }
                        }
                        else
                        {
                            // Fallback to loading all bytes if content length is not available
                            var data = response.Content.ReadAsByteArrayAsync().Result;
                            File.WriteAllBytes(filePath, data);
                            fileSizeMB = (double)data.Length / (1024.0 * 1024.0);
                        }

                        status = "Downloaded";
                        break; // Success
                    }
                    catch (Exception ex)
                    {
                        if (attempt == 3)
                        {
                            status = "Failed";
                        }
                        else
                        {
                            PauseAwareDelay(1000 * attempt);
                        }
                    }
                }
            }
            catch (Exception)
            {
                status = "Failed";
            }
            finally
            {
                stopwatch.Stop();
                if (shouldLog && !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(type))
                {
                    DownloadLogService.AddLog(id, type, status, stopwatch.Elapsed.TotalSeconds, fileSizeMB);
                }
            }
        }

        private static void PauseAwareDelay(int milliseconds)
        {
            var elapsed = 0;
            while (elapsed < milliseconds)
            {
                DownloadControlService.WaitIfPaused();
                var step = Math.Min(100, milliseconds - elapsed);
                System.Threading.Thread.Sleep(step);
                elapsed += step;
            }
        }

        #endregion
    }
}
