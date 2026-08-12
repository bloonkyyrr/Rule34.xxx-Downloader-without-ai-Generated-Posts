using R34Downloader.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace R34Downloader.Services
{
    /// <summary>
    /// Service for managing download logs.
    /// </summary>
    public static class DownloadLogService
    {
        #region Fields

        private static readonly List<DownloadLogEntry> Logs = new List<DownloadLogEntry>();

        #endregion

        #region Methods

        /// <summary>
        /// Adds a new download log entry.
        /// </summary>
        public static void AddLog(string id, string type, string status, double duration, double fileSizeMB = 0)
        {
            lock (Logs)
            {
                Logs.Add(new DownloadLogEntry(id, type, status, duration, fileSizeMB));
            }
        }

        /// <summary>
        /// Gets all download logs.
        /// </summary>
        public static List<DownloadLogEntry> GetAllLogs()
        {
            lock (Logs)
            {
                return new List<DownloadLogEntry>(Logs);
            }
        }

        /// <summary>
        /// Gets logs filtered by status.
        /// </summary>
        public static List<DownloadLogEntry> GetLogsByStatus(string status)
        {
            lock (Logs)
            {
                return Logs.Where(l => l.Status == status).ToList();
            }
        }

        /// <summary>
        /// Gets logs filtered by type.
        /// </summary>
        public static List<DownloadLogEntry> GetLogsByType(string type)
        {
            lock (Logs)
            {
                return Logs.Where(l => l.Type == type).ToList();
            }
        }

        /// <summary>
        /// Clears all logs.
        /// </summary>
        public static void ClearLogs()
        {
            lock (Logs)
            {
                Logs.Clear();
            }
        }

        /// <summary>
        /// Gets the count of logs.
        /// </summary>
        public static int GetLogCount()
        {
            lock (Logs)
            {
                return Logs.Count;
            }
        }

        #endregion
    }
}
