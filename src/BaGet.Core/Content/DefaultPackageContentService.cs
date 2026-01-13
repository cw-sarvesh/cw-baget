using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Protocol.Models;
using NuGet.Versioning;

namespace BaGet.Core
{
    /// <summary>
    /// Implements the NuGet Package Content resource. Supports read-through caching.
    /// Tracks state in a database (<see cref="IPackageService"/>) and stores packages
    /// using <see cref="IPackageStorageService"/>.
    /// </summary>
    public class DefaultPackageContentService : IPackageContentService
    {
        private readonly IMirrorService _mirror;
        private readonly IPackageService _packages;
        private readonly IPackageStorageService _storage;
        private readonly LicenseChecker _licenseChecker;

        public DefaultPackageContentService(
            IMirrorService mirror,
            IPackageService packages,
            IPackageStorageService storage,
            LicenseChecker licenseChecker)
        {
            _mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
            _packages = packages ?? throw new ArgumentNullException(nameof(packages));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _licenseChecker = licenseChecker ?? throw new ArgumentNullException(nameof(licenseChecker));
        }

        public async Task<PackageVersionsResponse> GetPackageVersionsOrNullAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var versions = await _mirror.FindPackageVersionsAsync(id, cancellationToken);
            if (!versions.Any())
            {
                return null;
            }

            return new PackageVersionsResponse
            {
                Versions = versions
                    .Select(v => v.ToNormalizedString())
                    .Select(v => v.ToLowerInvariant())
                    .ToList()
            };
        }

        public async Task<Stream> GetPackageContentStreamOrNullAsync(
            string id,
            NuGetVersion version,
            CancellationToken cancellationToken = default)
        {
            // Allow read-through caching if it is configured.
            await _mirror.MirrorAsync(id, version, cancellationToken);

            // Check if package has restricted license before allowing download
            var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
            if (package != null)
            {
                // Get nuspec to check for license expression
                using (var nuspecStream = await _storage.GetNuspecStreamAsync(id, version, cancellationToken))
                {
                    if (nuspecStream != null)
                    {
                        if (_licenseChecker.IsRestrictedLicenseFromNuspec(nuspecStream))
                        {
                            var licenseInfo = LicenseChecker.GetLicenseInfo(package.LicenseUrl, null);
                            throw new RestrictedLicenseException(id, version, licenseInfo);
                        }
                    }
                    else if (_licenseChecker.IsRestrictedLicense(package.LicenseUrl))
                    {
                        var licenseInfo = LicenseChecker.GetLicenseInfo(package.LicenseUrl, null);
                        throw new RestrictedLicenseException(id, version, licenseInfo);
                    }
                }
            }

            if (!await _packages.AddDownloadAsync(id, version, cancellationToken))
            {
                return null;
            }

            return await _storage.GetPackageStreamAsync(id, version, cancellationToken);
        }

        public async Task<Stream> GetPackageManifestStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
        {
            // Allow read-through caching if it is configured.
            await _mirror.MirrorAsync(id, version, cancellationToken);

            if (!await _packages.ExistsAsync(id, version, cancellationToken))
            {
                return null;
            }

            return await _storage.GetNuspecStreamAsync(id, version, cancellationToken);
        }

        public async Task<Stream> GetPackageReadmeStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
        {
            // Allow read-through caching if it is configured.
            await _mirror.MirrorAsync(id, version, cancellationToken);

            var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
            if (!package.HasReadme)
            {
                return null;
            }

            return await _storage.GetReadmeStreamAsync(id, version, cancellationToken);
        }

        public async Task<Stream> GetPackageIconStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
        {
            // Allow read-through caching if it is configured.
            await _mirror.MirrorAsync(id, version, cancellationToken);

            var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
            if (!package.HasEmbeddedIcon)
            {
                return null;
            }

            return await _storage.GetIconStreamAsync(id, version, cancellationToken);
        }
    }
}
