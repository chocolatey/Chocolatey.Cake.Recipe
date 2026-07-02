using Octokit;

BuildParameters.Tasks.PublishPublicArtifactsTask = Task("Publish-Public-Artifacts")
    .IsDependentOn("Build-MSI")
    .IsDependentOn("Package")
    .WithCriteria(() => BuildParameters.ShouldPublishPublicArtifacts, "Skipping because publishing of public artifacts is not enabled")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || BuildParameters.ForceContinuousIntegration, "Skipping because this is a local build, and force isn't being applied")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => BuildParameters.IsTagged, "Skipping because current commit is not tagged")
    .Does(async () =>
    {
        var provider = BuildParameters.PublishProvider;

        // Publish any artifacts that have been added to the provider.
        // This will only publish artifacts that the provider can publish, and will skip any artifacts that cannot be published by the provider.
        // We expect all artifacts to have been added to the provider in the "Build-MSI" and "Package" tasks, or explicitly by calling the BuildParameters.PublishProvider.AddArtifactAsync() method in a custom task.
        await provider.PublishArtifactsAsync();
    })
    .OnError(exception =>
    {
        Error(exception.Message);
        Information("Publish-Public-Artifacts Task failed, but continuing with next Task...");
        publishingError = true;
    });

/// <summary>
/// Defines the type of artifact to be published to public repositories or locations.
/// This is used to determine if the publish provider can publish the artifact type, and to provide additional information for logging purposes.
/// </summary>
public enum ArtifactType
{
    NuGetPackage,
    ChocolateyPackage,
    Other,
}

/// <summary>
/// Defines the interface for a publish provider that can publish artifacts to public repositories or locations.
/// </summary>
/// <remarks>
/// A publish provider is not to be confused with standard uploading of artifacts to a build server or other internal locations.
/// A publish provider is intended to publish artifacts to public repositories or locations, such as NuGet.org, Chocolatey.org, GitHub Releases, etc.
/// (Only GitHub Releases is currently supported, but more providers may be added in the future.)
/// </remarks>
public interface IPublishProvider
{
    /// <summary>
    /// Gets the name of the publish provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Adds the specified artifact paths to the list of artifacts to be published by the provider.
    /// </summary>
    /// <param name="artifactType">The type of artifact to be published.</param>
    /// <param name="artifactPaths">The paths of the artifacts to be published.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddArtifactAsync(ArtifactType artifactType, params FilePath[] artifactPaths);

    /// <summary>
    /// Determines whether the provider can publish artifacts of the specified type.
    /// </summary>
    /// <param name="artifactType">The type of artifact to be published.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the provider can publish artifacts of the specified type.</returns>
    Task<bool> CanPublishArtifactsAsync(ArtifactType artifactType = ArtifactType.Other);

    /// <summary>
    /// Publishes the artifacts that have been added to the provider.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishArtifactsAsync();
}

/// <summary>
/// Provides a base implementation of the <see cref="IPublishProvider"/> interface.
/// </summary>
internal abstract class PublishProviderBase : IPublishProvider
{
    protected readonly ICakeContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishProviderBase"/> class.
    /// </summary>
    /// <param name="context">The Cake context.</param>
    protected PublishProviderBase(ICakeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public abstract string Name { get; }

    protected IDictionary<string, FilePath> ArtifactsToPublish { get; } = new Dictionary<string, FilePath>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public virtual async Task AddArtifactAsync(ArtifactType artifactType, params FilePath[] artifactPaths)
    {
        if (artifactPaths == null || artifactPaths.Length == 0)
        {
            return;
        }

        if (!await CanPublishArtifactsAsync(artifactType))
        {
            return;
        }

        foreach (var artifactPath in artifactPaths)
        {
            if (artifactPath == null || string.IsNullOrWhiteSpace(artifactPath.FullPath))
            {
                continue;
            }

            ArtifactsToPublish[artifactPath.FullPath] = artifactPath;
        }
    }

    /// <inheritdoc/>
    public abstract Task<bool> CanPublishArtifactsAsync(ArtifactType artifactType = ArtifactType.Other);

    /// <inheritdoc/>
    public abstract Task PublishArtifactsAsync();
}

/// <summary>
/// Represents a publish provider that aggregates multiple publish providers and delegates publishing tasks to them.
/// </summary>
public sealed class AggregatePublishProvider : IPublishProvider
{
    private readonly List<IPublishProvider> _publishProviders = new List<IPublishProvider>();

    /// <inheritdoc/>
    public string Name => _publishProviders.Count == 0
        ? "None"
        : string.Join(", ", _publishProviders.Select(p => p.Name));

    /// <inheritdoc/>
    public async Task AddArtifactAsync(ArtifactType artifactType, params FilePath[] artifactPaths)
    {
        if (artifactPaths == null || artifactPaths.Length == 0)
        {
            return;
        }

        foreach (var provider in _publishProviders)
        {
            await provider.AddArtifactAsync(artifactType, artifactPaths);
        }
    }

    /// <summary>
    /// Adds a publish provider to the aggregate provider.
    /// </summary>
    /// <param name="publishProvider">The publish provider to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="publishProvider"/> is null.</exception>
    public void AddPublishProvider(IPublishProvider publishProvider)
    {
        if (publishProvider == null)
        {
            throw new ArgumentNullException(nameof(publishProvider));
        }

        _publishProviders.Add(publishProvider);
    }

    /// <inheritdoc/>
    public async Task<bool> CanPublishArtifactsAsync(ArtifactType artifactType = ArtifactType.Other)
    {
        if (_publishProviders.Count == 0)
        {
            return false;
        }

        foreach (var provider in _publishProviders)
        {
            if (await provider.CanPublishArtifactsAsync(artifactType))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task PublishArtifactsAsync()
    {
        foreach (var provider in _publishProviders)
        {
            await provider.PublishArtifactsAsync();
        }
    }
}

/// <summary>
/// Represents a publish provider that publishes artifacts to GitHub Releases.
/// </summary>
/// <remarks>
/// This provider uses the GitReleaseManager tool to publish artifacts to GitHub Releases. It requires a valid GitHub token to be set in the environment variables or in the Cake context.
/// All artifact types are supported by this provider, and it will attempt to publish all artifacts that have been added to it.
/// </remarks>
internal class GitHubPublishProvider : PublishProviderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubPublishProvider"/> class.
    /// </summary>
    /// <param name="context">The Cake context.</param>
    public GitHubPublishProvider(ICakeContext context)
      : base(context)
    {
    }

    /// <inheritdoc/>
    public override string Name { get; } = "GitHub";

    /// <inheritdoc/>
    public override Task<bool> CanPublishArtifactsAsync(ArtifactType artifactType = ArtifactType.Other)
    {
        return System.Threading.Tasks.Task.FromResult(HasAvailableGitHubRelease());
    }

    /// <inheritdoc/>
    public override Task PublishArtifactsAsync()
    {
        if (!HasAvailableGitHubRelease())
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        var assets = string.Join(",", ArtifactsToPublish.Values.Select(a => a.ToString()));

        if (assets.Length == 0)
        {
            _context.Verbose("No artifacts to publish using {0} provider.", Name);

            return System.Threading.Tasks.Task.CompletedTask;
        }

        var addAssetsSettings = new GitReleaseManagerAddAssetsSettings();

        if (_context.EnvironmentVariable("CI") != null || _context.EnvironmentVariable("TEAMCITY_VERSION") != null)
        {
            addAssetsSettings.ArgumentCustomization = args => args.Append("--ci");
        }

        var gitReleaseManagerCredentials = GitReleaseManagerCredentials.FetchCredentials(_context);
        var tagName = BuildParameters.Version.Milestone;
        _context.Information("Using Tag Name '{0}' for publishing.", tagName);
        _context.Information("Adding {0} asset(s) to GitHub release.", ArtifactsToPublish.Count);

        _context.RequireToolEx(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.GitReleaseManagerGlobalTool : ToolSettings.GitReleaseManagerTool, (context) => {
            context.GitReleaseManagerAddAssets(gitReleaseManagerCredentials.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, tagName, assets, addAssetsSettings);
        });

        return System.Threading.Tasks.Task.CompletedTask;
    }

    private bool HasAvailableGitHubRelease()
    {
        var gitReleaseManagerCredentials = GitReleaseManagerCredentials.FetchCredentials(_context);

        if (string.IsNullOrWhiteSpace(gitReleaseManagerCredentials.Token))
        {
            _context.Information("No GitReleaseManager Credentials found, unable to publish public artifacts.");
            return false;
        }

        var client = new GitHubClient(new Octokit.ProductHeaderValue("Chocolatey.Cake.Recipe"))
        {
            Credentials = new Credentials(gitReleaseManagerCredentials.Token),
        };

        try
        {
            var release = client.Repository.Release.Get(BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.Milestone).GetAwaiter().GetResult();

            if (release == null)
            {
                _context.Warning("No GitHub release exists with the tag {0}, unable to publish public artifacts.", BuildParameters.Version.Milestone);
                return false;
            }

            return true;
        }
        catch (NotFoundException)
        {
            _context.Warning("No GitHub release exists with the tag {0}, unable to publish public artifacts.", BuildParameters.Version.Milestone);
            return false;
        }
        catch (AuthorizationException exception)
        {
            var message = string.Format("GitHub credentials are invalid or unauthorized, unable to publish public artifacts. {0}", exception.Message);

            throw new Exception(message, exception);
        }
        catch (ForbiddenException exception)
        {
            var message = string.Format("GitHub API access was forbidden or rate-limited, unable to publish public artifacts. {0}", exception.Message);

            throw new Exception(message, exception);
        }
    }
}
