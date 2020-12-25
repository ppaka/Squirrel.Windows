﻿using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.VisualStudio;
using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using System;
using EnvDTE;

namespace NuGet.Client.VisualStudio.PowerShell
{
    public class PackageInstallBaseCommand : PackageActionBaseCommand
    {
        private ResolutionContext _context;

        public PackageInstallBaseCommand(
            IVsPackageSourceProvider packageSourceProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            SVsServiceProvider svcServiceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ISolutionManager solutionManager,
            IHttpClientEvents clientEvents)
            : base(packageSourceProvider, packageRepositoryFactory, svcServiceProvider, packageManagerFactory, solutionManager, clientEvents, PackageActionType.Install)
        {
            this.PackageSourceProvider = packageSourceProvider;
        }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter]
        public Client.FileConflictAction FileConflictAction { get; set; }

        [Parameter]
        public DependencyBehavior? DependencyVersion { get; set; }

        public IVsPackageSourceProvider PackageSourceProvider { get; set; }

        public bool ForceInstall { get; set; }

        public bool IsVersionEnum { get; set; }

        public DependencyVersion UpdateVersionEnum { get; set; }

        protected override void Preprocess()
        {
            this.ActiveSourceRepository = GetActiveRepository(Source);
            this.PackageActionResolver = new ActionResolver(
                ActiveSourceRepository,
                CreateDependencyResolutionSource(),
                ResolutionContext);
            base.Preprocess();
        }

        /// <summary>
        /// Used for Install-Package -Force and Update-Package -Reinstall
        /// </summary>
        /// <param name="identities"></param>
        /// <param name="targetedProjects"></param>
        protected void ForceInstallPackages(IEnumerable<PackageIdentity> identities, IEnumerable<VsProject> targetedProjects)
        {
            // Install package to the Default project. 
            VsProject project = targetedProjects.FirstOrDefault();
            IEnumerable<InstalledPackageReference> references = GetInstalledReferences(project);
            foreach (PackageIdentity identity in identities)
            {
                InstalledPackageReference alreadyInstalled = references.Where(p => p.Identity.Equals(identity)).FirstOrDefault();
                if (alreadyInstalled != null)
                {
                    ForceInstallPackage(alreadyInstalled.Identity, targetedProjects);
                }
                else
                {
                    ExecuteSinglePackageAction(identity, targetedProjects);
                }
            }
        }

        /// <summary>
        /// Forcely install single package identity.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="targetedProjects"></param>
        protected void ForceInstallPackage(PackageIdentity identity, IEnumerable<VsProject> targetedProjects)
        {
            // Forcely removed already installed package identity
            ExecuteSinglePackageAction(identity, targetedProjects, PackageActionType.Uninstall);
            // Install it back again.
            ExecuteSinglePackageAction(identity, targetedProjects, PackageActionType.Install);
        }

        /// <summary>
        /// Resolution Context for the command
        /// </summary>
        public ResolutionContext ResolutionContext
        {
            get
            {
                _context = new ResolutionContext();
                _context.DependencyBehavior = GetDependencyBehavior();
                _context.AllowPrerelease = IncludePrerelease.IsPresent;
                _context.ForceRemove = ForceInstall;
                // For forcely install. 
                _context.RemoveDependencies = false;
                if (!string.IsNullOrEmpty(Version))
                {
                    DependencyVersion updateVersion;
                    IsVersionEnum = Enum.TryParse<DependencyVersion>(Version, true, out updateVersion);
                    if (IsVersionEnum)
                    {
                        UpdateVersionEnum = updateVersion;
                    }
                    // If Version is prerelease, automatically allow prerelease (i.e. append -Prerelease switch).
                    else if (PowerShellPackage.IsPrereleaseVersion(Version))
                    {
                        _context.AllowPrerelease = true;
                    }
                }
                return _context;
            }
        }

        public override FileConflictAction ResolveFileConflict(string message)
        {
            if (FileConflictAction == FileConflictAction.Overwrite)
            {
                return Client.FileConflictAction.Overwrite;
            }

            if (FileConflictAction == FileConflictAction.Ignore)
            {
                return Client.FileConflictAction.Ignore;
            }

            return base.ResolveFileConflict(message);
        }

        private DependencyBehavior GetDependencyBehavior()
        {
            if (ForceInstall)
            {
                return DependencyBehavior.Ignore;
            }

            if (IgnoreDependencies.IsPresent)
            {
                return DependencyBehavior.Ignore;
            }
            else if (DependencyVersion.HasValue)
            {
                return DependencyVersion.Value;
            }
            else
            {
                // Read it from NuGet.Config and default to Lowest.
                return GetDependencyVersion();
            }
        }

        /// <summary>
        /// Returns the user specified DependencyVersion in nuget.config.
        /// </summary>
        /// <returns>The user specified DependencyVersion value in nuget.config.</returns>
        private DependencyBehavior GetDependencyVersion()
        {
            IMachineWideSettings _machineWideSettings = new VsMachineWideSettings();
            IRepositorySettings _repositorySettings = ServiceLocator.GetInstance<IRepositorySettings>();
            string configFolderPath = _repositorySettings.ConfigFolderPath;
            IFileSystem configSettingsFileSystem = GetConfigSettingsFileSystem(configFolderPath);
            var settings = Settings.LoadDefaultSettings(
                    configSettingsFileSystem,
                    configFileName: null,
                    machineWideSettings: _machineWideSettings);
            string dependencyVersionValue = settings.GetConfigValue("DependencyVersion");
            DependencyBehavior dependencyVersion;
            if (Enum.TryParse(dependencyVersionValue, ignoreCase: true, result: out dependencyVersion))
            {
                return dependencyVersion;
            }
            else
            {
                return DependencyBehavior.Lowest;
            }
        }

        protected internal virtual IFileSystem GetConfigSettingsFileSystem(string configFolderPath)
        {
            return new SolutionFolderFileSystem(ServiceLocator.GetInstance<DTE>().Solution, ".nuget", configFolderPath);
        }
    }
}
