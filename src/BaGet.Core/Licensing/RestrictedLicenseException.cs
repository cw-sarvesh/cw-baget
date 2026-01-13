using System;
using NuGet.Versioning;

namespace BaGet.Core
{
    /// <summary>
    /// An exception thrown when a package has a restricted license (AGPL/GPL) and cannot be mirrored or downloaded.
    /// </summary>
    public class RestrictedLicenseException : Exception
    {
        /// <summary>
        /// Create a new instance of the <see cref="RestrictedLicenseException"/>.
        /// </summary>
        /// <param name="packageId">The ID of the package with restricted license.</param>
        /// <param name="packageVersion">The version of the package with restricted license.</param>
        /// <param name="licenseInfo">Information about the restricted license.</param>
        public RestrictedLicenseException(string packageId, NuGetVersion packageVersion, string licenseInfo)
            : base($"Package {packageId} {packageVersion} has a restricted license ({licenseInfo}) and cannot be mirrored or downloaded.")
        {
            PackageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
            PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
            LicenseInfo = licenseInfo ?? throw new ArgumentNullException(nameof(licenseInfo));
        }

        /// <summary>
        /// The package ID with restricted license.
        /// </summary>
        public string PackageId { get; }

        /// <summary>
        /// The package version with restricted license.
        /// </summary>
        public NuGetVersion PackageVersion { get; }

        /// <summary>
        /// Information about the restricted license.
        /// </summary>
        public string LicenseInfo { get; }
    }
}

