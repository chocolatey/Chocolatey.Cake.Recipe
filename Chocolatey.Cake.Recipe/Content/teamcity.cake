///////////////////////////////////////////////////////////////////////////////
// BUILD PROVIDER
///////////////////////////////////////////////////////////////////////////////

public class TeamCityTagInfo : ITagInfo
{
    public TeamCityTagInfo(ICakeContext context)
    {
        // Test to see if current commit is a tag...
        context.Information("Testing to see if current commit contains a tag...");
        IEnumerable<string> redirectedStandardOutput;
        IEnumerable<string> redirectedError;

        var exitCode = context.StartProcess(
            "git",
            new ProcessSettings {
                Arguments = "tag -l --points-at HEAD",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            out redirectedStandardOutput,
            out redirectedError
        );

        if (exitCode == 0)
        {
            if (redirectedStandardOutput.Count() != 0)
            {
                IsTag = true;
                Name = redirectedStandardOutput.FirstOrDefault();
            }
        }
    }

    public bool IsTag { get; }

    public string Name { get; }
}

public class TeamCityRepositoryInfo : IRepositoryInfo
{
    public TeamCityRepositoryInfo(ITeamCityProvider teamCity, ICakeContext context)
    {
        Name = teamCity.Environment.Build.BuildConfName;

        var baseRef = context.BuildSystem().GitHubActions.Environment.Workflow.BaseRef;
        if (!string.IsNullOrEmpty(baseRef))
        {
            Branch = baseRef;
        }
        else
        {
            // This trimming is not perfect, as it will remove part of a
            // branch name if the branch name itself contains a '/'
            var tempName = context.Environment.GetEnvironmentVariable("Git_Branch");

            const string headPrefix = "refs/heads/";
            const string tagPrefix = "refs/tags/";

            if (!string.IsNullOrEmpty(tempName))
            {
                if (tempName.StartsWith(headPrefix))
                {
                    tempName = tempName.Substring(headPrefix.Length);
                }
                else if (tempName.StartsWith(tagPrefix))
                {
                    var gitTool = context.Tools.Resolve("git");
                    if (gitTool == null)
                    {
                        gitTool = context.Tools.Resolve("git.exe");
                    }

                    if (gitTool != null)
                    {
                        IEnumerable<string> redirectedStandardOutput;
                        IEnumerable<string> redirectedError;

                        var exitCode = context.StartProcess(
                            gitTool,
                            new ProcessSettings {
                                Arguments = "branch -r --contains " + tempName,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                            },
                            out redirectedStandardOutput,
                            out redirectedError
                        );

                        if (exitCode == 0)
                        {
                            var lines = redirectedStandardOutput.ToList();
                            if (lines.Count != 0)
                            {
                                tempName = lines[0].TrimStart(new []{ ' ', '*' }).Replace("origin/", string.Empty);
                            }
                        }
                    }
                }
                else if (tempName.IndexOf('/') >= 0)
                {
                    tempName = tempName.Substring(tempName.LastIndexOf('/') + 1);
                }
            }

            Branch = tempName;
        }

        Tag = new TeamCityTagInfo(context);
    }

    public string Branch { get; }

    public string Name { get; }

    public ITagInfo Tag { get; }
}

public class TeamCityPullRequestInfo : IPullRequestInfo
{
    public TeamCityPullRequestInfo(ITeamCityProvider teamCity)
    {
        IsPullRequest = teamCity.Environment.PullRequest.IsPullRequest;
    }

    public bool IsPullRequest { get; }
}

public class TeamCityBuildInfo : IBuildInfo
{
    public TeamCityBuildInfo(ITeamCityProvider teamCity)
    {
        Number = teamCity.Environment.Build.Number;
    }

    public string Number { get; }
}

public class TeamCityBuildProvider : IBuildProvider
{
    public TeamCityBuildProvider(ITeamCityProvider teamCity, ICakeContext context)
    {
        Build = new TeamCityBuildInfo(teamCity);
        PullRequest = new TeamCityPullRequestInfo(teamCity);
        Repository = new TeamCityRepositoryInfo(teamCity, context);

        _teamCity = teamCity;
        _context = context;
    }

    public IRepositoryInfo Repository { get; }

    public IPullRequestInfo PullRequest { get; }

    public IBuildInfo Build { get; }

    public bool SupportsTokenlessCodecov { get; } = false;

    public BuildProviderType Type { get; } = BuildProviderType.TeamCity;

    public IEnumerable<string> PrintVariables { get; } = new[] {
        "TEAMCITY_BUILD_BRANCH",
        "TEAMCITY_BUILD_COMMIT",
        "TEAMCITY_BUILD_ID",
        "TEAMCITY_BUILD_REPOSITORY",
        "TEAMCITY_BUILD_URL",
        "TEAMCITY_VERSION",
        "vcsroot.branch",
    };

    private readonly ITeamCityProvider _teamCity;

    private readonly ICakeContext _context;

    public void UploadArtifact(FilePath file)
    {
        _context.Information("Uploading artifact from path: {0}", file.FullPath);
        _teamCity.PublishArtifacts(file.FullPath);
    }
}