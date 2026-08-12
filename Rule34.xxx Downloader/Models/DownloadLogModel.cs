using System;

namespace R34Downloader.Models
{
    /// <summary>
    /// Represents a single download log entry.
    /// </summary>
    public class DownloadLogEntry
    {
        #region Properties

        /// <summary>
        /// Gets or sets the media ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the media type (Video, Image, or Gif).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the download status (Downloaded, Failed, or Skipped).
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the download duration in seconds.
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// Gets or sets the file size in megabytes.
        /// </summary>
        public double FileSizeMB { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the download was logged.
        /// </summary>
        public DateTime Timestamp { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the DownloadLogEntry class.
        /// </summary>
        public DownloadLogEntry()
        {
            Timestamp = DateTime.Now;
            FileSizeMB = 0;
        }

        /// <summary>
        /// Initializes a new instance of the DownloadLogEntry class with parameters.
        /// </summary>
        public DownloadLogEntry(string id, string type, string status, double duration, double fileSizeMB = 0)
            : this()
        {
            Id = id;
            Type = type;
            Status = status;
            Duration = duration;
            FileSizeMB = fileSizeMB;
        }

        #endregion
    }
}
