using System.Threading;

namespace R34Downloader.Services
{
    /// <summary>
    /// Controls pause and resume state for active downloads.
    /// </summary>
    public static class DownloadControlService
    {
        private static readonly ManualResetEventSlim PauseGate = new ManualResetEventSlim(true);

        public static bool IsPaused => !PauseGate.IsSet;

        public static void BeginDownload()
        {
            PauseGate.Set();
        }

        public static void Pause()
        {
            PauseGate.Reset();
        }

        public static void Resume()
        {
            PauseGate.Set();
        }

        public static void WaitIfPaused()
        {
            PauseGate.Wait();
        }
    }
}
