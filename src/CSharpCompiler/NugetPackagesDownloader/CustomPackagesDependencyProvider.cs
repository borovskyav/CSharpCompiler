using NuGet.DependencyResolver;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace CSharpCompiler.NugetPackagesDownloader;

internal class CustomPackagesDependencyProvider : IDependencyProvider
{
    public CustomPackagesDependencyProvider(LibraryRange projectLibraryRange, IReadOnlyList<PackageIdentity> packageIdentities)
    {
        this.projectLibraryRange = projectLibraryRange;
        this.packageIdentities = packageIdentities;
    }

    public bool SupportsType(LibraryDependencyTarget libraryType)
    {
        return (libraryType & (LibraryDependencyTarget.Project | LibraryDependencyTarget.ExternalProject)) != LibraryDependencyTarget.None;
    }

    public Library GetLibrary(LibraryRange libraryRange, NuGetFramework targetFramework)
    {
        if(libraryRange.Name != projectLibraryRange.Name)
            return null!;

        return new Library
            {
                LibraryRange = libraryRange,
                Identity = new LibraryIdentity { Name = projectLibraryRange.Name, Version = projectLibraryRange.VersionRange.MinVersion, Type = LibraryType.Project, },
                Path = null,
                Dependencies = packageIdentities.Select(GetDependency),
                Resolved = true,
            };
    }

    private LibraryDependency GetDependency(PackageIdentity packageIdentity)
    {
        return new LibraryDependency
            {
                LibraryRange = new LibraryRange(packageIdentity.Id, new VersionRange(packageIdentity.Version), LibraryDependencyTarget.Package),
                VersionCentrallyManaged = true,
                ReferenceType = LibraryDependencyReferenceType.Direct,
            };
    }

    private readonly LibraryRange projectLibraryRange;
    private readonly IReadOnlyList<PackageIdentity> packageIdentities;
}