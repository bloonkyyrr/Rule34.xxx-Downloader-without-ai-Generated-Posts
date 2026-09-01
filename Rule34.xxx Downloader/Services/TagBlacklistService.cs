using R34Downloader.Models;
using System;
using System.Collections.Generic;

namespace R34Downloader.Services
{
    /// <summary>
    /// Checks post tags against the user-configured download blacklist.
    /// </summary>
    public static class TagBlacklistService
    {
        /// <summary>
        /// Returns true when any post tag matches a blacklisted tag.
        /// Underscores and spaces are treated as the same character.
        /// </summary>
        public static bool ContainsBlacklistedTags(string tagsString)
        {
            if (string.IsNullOrWhiteSpace(tagsString))
            {
                return false;
            }

            return ContainsBlacklistedTags(tagsString.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Returns true when any post tag matches a blacklisted tag.
        /// Underscores and spaces are treated as the same character.
        /// </summary>
        public static bool ContainsBlacklistedTags(IEnumerable<string> tags)
        {
            var blacklistedTags = GetNormalizedBlacklistedTags();
            if (blacklistedTags.Count == 0 || tags == null)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                if (blacklistedTags.Contains(NormalizeTag(tag)))
                {
                    return true;
                }
            }

            return false;
        }

        private static HashSet<string> GetNormalizedBlacklistedTags()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(SettingsModel.BlacklistedTags))
            {
                return result;
            }

            var parts = SettingsModel.BlacklistedTags.Split(new[] { ' ', '\t', '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var normalized = NormalizeTag(part);
                if (!string.IsNullOrEmpty(normalized))
                {
                    result.Add(normalized);
                }
            }

            return result;
        }

        private static string NormalizeTag(string tag)
        {
            return tag.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }
}
