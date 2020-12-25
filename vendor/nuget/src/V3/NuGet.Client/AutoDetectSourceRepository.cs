﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client;

namespace NuGet.Client
{
    /// <summary>
    /// A SourceRepository class that will detect if the url points to a V3 or V2 
    /// repository.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class AutoDetectSourceRepository : SourceRepository
    {
        PackageSource _source;

        string _host;

        // factory used to create V2 repo
        IPackageRepositoryFactory _v2RepoFactory;

        // Once the version of the repo is detected, this variable will 
        // be the real repo used.
        SourceRepository _repo;

        SemaphoreSlim _lock;

        public AutoDetectSourceRepository(
            PackageSource source, 
            string host,
            IPackageRepositoryFactory repoFactory)
        {
            _source = source;
            _host = host;
            _v2RepoFactory = repoFactory;
            _lock = new SemaphoreSlim(1);
        }

        private async Task DetectVersionWhenNeccessary()
        {
            await _lock.WaitAsync();
            try
            {
                if (_repo != null)
                {
                    return;
                }

                bool r = await IsV3Async(_source);
                if (r)
                {
                    _repo = new V3SourceRepository(_source, _host);
                    return;
                }

                r = await IsV2Async(_source);
                if (r)
                {
                    _repo = new NuGet.Client.Interop.V2SourceRepository(
                        _source, _v2RepoFactory.CreateRepository(_source.Url), _host);
                    return;
                }

                throw new InvalidOperationException(
                    String.Format("source {0} is not available", _source.Url));
            }
            finally
            {
                _lock.Release();
            }
        }

        private static async Task<bool> IsV2Async(PackageSource source)
        {
            var url = new Uri(source.Url);
            if (url.IsFile || url.IsUnc)
            {
                return true;
            }

            using (var client = new Data.DataClient())
            {
                var result = await client.GetFile(url);
                if (result == null)
                {
                    return false;
                }

                var raw = result.Value<string>("raw");
                if (raw != null && raw.IndexOf("Packages", StringComparison.OrdinalIgnoreCase) != -1) 
                {
                    return true;
                }

                return false;
            }
        }

        private static async Task<bool> IsV3Async(PackageSource source)
        {
            var url = new Uri(source.Url);
            if (url.IsFile || url.IsUnc)
            {
                return File.Exists(url.LocalPath);
            }

            using (var client = new Data.DataClient())
            {
                var v3index = await client.GetFile(url);
                if (v3index == null)
                {
                    return false;
                }

                var status = v3index.Value<string>("version");
                if (status != null && status.StartsWith("3.0"))
                {
                    return true;
                }

                return false;
            }
        }

        public override PackageSource Source
        {
            get { return _source; }
        }

        public override async Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            await DetectVersionWhenNeccessary();
            return await _repo.Search(searchTerm, filters, skip, take, cancellationToken);
        }

        public override async Task<Newtonsoft.Json.Linq.JObject> GetPackageMetadata(string id, Versioning.NuGetVersion version)
        {
            await DetectVersionWhenNeccessary();
            return await _repo.GetPackageMetadata(id, version);
        }

        public override async Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> GetPackageMetadataById(string packageId)
        {
            await DetectVersionWhenNeccessary();
            return await _repo.GetPackageMetadataById(packageId);
        }

        public override async void RecordMetric(PackageActionType actionType, PackageIdentity packageIdentity, PackageIdentity dependentPackage, bool isUpdate, IInstallationTarget target)
        {
            await DetectVersionWhenNeccessary();
            _repo.RecordMetric(actionType, packageIdentity, dependentPackage, isUpdate, target);
        }
    }

    public class DependencyResolutionRepository : SourceRepository
    {
        List<SourceRepository> _sources;

        public DependencyResolutionRepository(IEnumerable<SourceRepository> sources)
        {
            _sources = sources.ToList();
        }
        
        public override async Task<Newtonsoft.Json.Linq.JObject> GetPackageMetadata(string id, Versioning.NuGetVersion version)
        {
            foreach (var source in _sources)
            {
                var result = await source.GetPackageMetadata(id, version);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public override async Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> GetPackageMetadataById(string packageId)
        {
            foreach (var source in _sources)
            {
                var result = await source.GetPackageMetadataById(packageId);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public override void RecordMetric(PackageActionType actionType, PackageIdentity packageIdentity, PackageIdentity dependentPackage, bool isUpdate, IInstallationTarget target)
        {
            // should not be called
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            // should not be called
            throw new NotImplementedException();
        }

        public override PackageSource Source
        {
            // should not be called
            get { return null; }
        }
    }
}
