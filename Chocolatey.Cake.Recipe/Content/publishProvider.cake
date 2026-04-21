using Octokit;

BuildParameters.Tasks.PublishPublicArtifactsTask = Task("Publish-Public-Artifacts")
    .IsDependentOn("Build-MSI")
    .IsDependentOn("Publish-Release-Packages")
    .WithCriteria(() => BuildParameters.ShouldPublishPublicArtifacts, "Skipping because publishing of public artifacts is not enabled")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || BuildParameters.ForceContinuousIntegration, "Skipping because this is a local build, and force isn't being applied")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => BuildParameters.IsTagged, "Skipping because current commit is not tagged")
    .Does(async () =>
    {
        var provider = BuildParameters.PublishProvider;

        await provider.PublishArtifactsAsync();
    })
    .OnError(exception =>
    {
        Error(exception.Message);
        Information("Publish-Public-Artifacts Task failed, but continuing with next Task...");
        publishingError = true;
    });

public enum PublishProviderType
{
    None,
    GitHub,
}

public interface IPublishProvider
{
    string Name { get; }

    void AddArtifact(params FilePath[] artifactPaths);

    Task<bool> CanPublishArtifactsAsync();

    Task PublishArtifactsAsync();
}

public static IPublishProvider GetPublishProvider(ICakeContext context, PublishProviderType providerType)
{
    switch (providerType)
    {
        case PublishProviderType.GitHub:
            return new GitHubPublishProvider(context);

        default:
            return new NullPublishProvider(context);
    }
}

internal class NullPublishProvider : IPublishProvider
{
    private readonly ICakeContext _context;

    public NullPublishProvider(ICakeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public string Name { get; } = "None";

    public void AddArtifact(params FilePath[] artifactPaths)
    {
        // Nothing to do, we aren't publishing anything in this case.
    }

    public Task<bool> CanPublishArtifactsAsync()
    {
        return System.Threading.Tasks.Task.FromResult(false);
    }

    public Task PublishArtifactsAsync()
    {
        _context.Information("[NullPublishProvider] No artifacts possible to publish.");
        return System.Threading.Tasks.Task.CompletedTask;
    }
}

internal class GitHubPublishProvider : IPublishProvider
{
    private readonly ICakeContext _context;
    private readonly List<FilePath> _publishArtifacts = new List<FilePath>();

    public GitHubPublishProvider(ICakeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public string Name { get; } = "GitHub";

    public void AddArtifact(params FilePath[] artifactPaths)
    {
        _publishArtifacts.AddRange(artifactPaths);
    }

    public async Task<bool> CanPublishArtifactsAsync()
    {
        var credentials = GitReleaseManagerCredentials.FetchCredentials(_context);

        if (string.IsNullOrWhiteSpace(credentials.Token))
        {
            _context.Information("No GitReleaseManager Credentials found, unable to publish public artifacts.");
            return false;
        }

        var client = new GitHubClient(new Octokit.ProductHeaderValue("Chocolatey.Cake.Recipe"))
        {
            Credentials = new Credentials(credentials.Token),
        };

        try
        {
            var release = await client.Repository.Release.Get(BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.Milestone);

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
    }

    public async Task PublishArtifactsAsync()
    {
        if (!await CanPublishArtifactsAsync())
        {
            return;
        }

        var assets = string.Join(",", _publishArtifacts.Select(a => a.ToString()));

        if (assets.Length == 0)
        {
            return;
        }

        var addAssetsSettings = new GitReleaseManagerAddAssetsSettings();

        if (_context.EnvironmentVariable("CI") != null || _context.EnvironmentVariable("TEAMCITY_VERSION") != null)
        {
            addAssetsSettings.ArgumentCustomization = args => args.Append("--ci");
        }

        var gitReleaseManagerCredentials = GitReleaseManagerCredentials.FetchCredentials(_context);
        var tagName = BuildParameters.Version.Milestone;
        _context.Information("Using Tag Name '{0}' for publishing.", tagName);

        _context.RequireToolEx(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.GitReleaseManagerGlobalTool : ToolSettings.GitReleaseManagerTool, (context) => {
            context.GitReleaseManagerAddAssets(gitReleaseManagerCredentials.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, tagName, assets, addAssetsSettings);
        });
    }
}