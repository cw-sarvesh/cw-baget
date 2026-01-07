using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace BaGet.Core
{
    /// <summary>
    /// Service to check if a package has a restricted license based on configured patterns.
    /// </summary>
    public class LicenseChecker
    {
        private readonly LicenseFilterOptions _options;
        private readonly List<Regex> _blockedPatterns;

        public LicenseChecker(IOptionsSnapshot<BaGetOptions> bagetOptions)
        {
            _options = bagetOptions?.Value?.LicenseFilter ?? new LicenseFilterOptions();
            _blockedPatterns = _options.BlockedLicensePatterns?
                .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList() ?? new List<Regex>();
        }

        /// <summary>
        /// Checks if license filtering is enabled.
        /// </summary>
        public bool IsEnabled => _options.Enabled;

        /// <summary>
        /// Checks if a license URL matches any blocked license patterns.
        /// </summary>
        /// <param name="licenseUrl">The license URL to check.</param>
        /// <returns>True if the license is restricted, false otherwise.</returns>
        public bool IsRestrictedLicense(Uri licenseUrl)
        {
            if (!IsEnabled || licenseUrl == null || _blockedPatterns.Count == 0)
            {
                return false;
            }

            var urlString = licenseUrl.AbsoluteUri;
            return _blockedPatterns.Any(pattern => pattern.IsMatch(urlString));
        }

        /// <summary>
        /// Checks if a license expression matches any blocked license patterns.
        /// </summary>
        /// <param name="licenseExpression">The license expression to check.</param>
        /// <returns>True if the license is restricted, false otherwise.</returns>
        public bool IsRestrictedLicense(string licenseExpression)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(licenseExpression) || _blockedPatterns.Count == 0)
            {
                return false;
            }

            return _blockedPatterns.Any(pattern => pattern.IsMatch(licenseExpression));
        }

        /// <summary>
        /// Checks if a package has a restricted license by examining both LicenseUrl and LicenseExpression.
        /// </summary>
        /// <param name="licenseUrl">The license URL.</param>
        /// <param name="licenseExpression">The license expression.</param>
        /// <returns>True if the license is restricted, false otherwise.</returns>
        public bool IsRestrictedLicense(Uri licenseUrl, string licenseExpression)
        {
            return IsRestrictedLicense(licenseUrl) || IsRestrictedLicense(licenseExpression);
        }

        /// <summary>
        /// Checks if a package has a restricted license by reading from a nuspec stream.
        /// </summary>
        /// <param name="nuspecStream">The nuspec stream to read.</param>
        /// <returns>True if the license is restricted, false otherwise.</returns>
        public bool IsRestrictedLicenseFromNuspec(Stream nuspecStream)
        {
            if (!IsEnabled || nuspecStream == null || _blockedPatterns.Count == 0)
            {
                return false;
            }

            try
            {
                nuspecStream.Position = 0;
                var doc = XDocument.Load(nuspecStream);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                // Check license expression (newer format)
                var licenseElement = doc.Root?
                    .Element(ns + "metadata")?
                    .Element(ns + "license");

                string licenseExpression = null;
                if (licenseElement != null)
                {
                    var licenseType = licenseElement.Attribute("type")?.Value;
                    if (licenseType == "expression")
                    {
                        licenseExpression = licenseElement.Value;
                    }
                }

                if (IsRestrictedLicense(licenseExpression))
                {
                    return true;
                }

                // Check license URL (older format)
                var licenseUrlString = doc.Root?
                    .Element(ns + "metadata")?
                    .Element(ns + "licenseUrl")?
                    .Value;

                if (!string.IsNullOrWhiteSpace(licenseUrlString) &&
                    Uri.TryCreate(licenseUrlString, UriKind.Absolute, out var licenseUrl))
                {
                    return IsRestrictedLicense(licenseUrl);
                }

                return false;
            }
            catch
            {
                // If we can't parse the nuspec, we can't determine the license, so allow it
                return false;
            }
        }

        /// <summary>
        /// Gets license information from a package for error messages.
        /// </summary>
        /// <param name="licenseUrl">The license URL.</param>
        /// <param name="licenseExpression">The license expression.</param>
        /// <returns>A string describing the license.</returns>
        public static string GetLicenseInfo(Uri licenseUrl, string licenseExpression)
        {
            if (!string.IsNullOrWhiteSpace(licenseExpression))
            {
                return $"License Expression: {licenseExpression}";
            }

            if (licenseUrl != null)
            {
                return $"License URL: {licenseUrl.AbsoluteUri}";
            }

            return "Unknown license";
        }
    }
}

