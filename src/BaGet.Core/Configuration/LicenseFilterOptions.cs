using System.Collections.Generic;

namespace BaGet.Core
{
    /// <summary>
    /// Configuration options for license filtering.
    /// </summary>
    public class LicenseFilterOptions
    {
        /// <summary>
        /// If true, license filtering is enabled and packages with restricted licenses will be blocked.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// List of license patterns to block. Patterns are case-insensitive and use regex matching.
        /// Examples: "AGPL", "GPL", "AFFERO.*GPL"
        /// </summary>
        public List<string> BlockedLicensePatterns { get; set; } = new List<string>();
    }
}

