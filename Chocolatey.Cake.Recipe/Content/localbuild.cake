// Copyright © 2022 Chocolatey Software, Inc
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

///////////////////////////////////////////////////////////////////////////////
// BUILD PROVIDER
///////////////////////////////////////////////////////////////////////////////

public class LocalBuildTagInfo : ITagInfo
{
    public LocalBuildTagInfo(bool isTag, string name)
    {
        IsTag = isTag;
        Name = name;
    }

    public LocalBuildTagInfo(ICakeContext context)
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
            var lines = redirectedStandardOutput.ToList();
            if (lines.Any())
            {
                IsTag = true;
                Name = lines.FirstOrDefault();
                context.Information("Tag name is {0}", Name);
            }
            else
            {
                context.Information("No tag is present.");
            }
        }
    }

    public bool IsTag { get; }

    public string Name { get; }
}

public class LocalBuildRepositoryInfo : IRepositoryInfo
{
    public LocalBuildRepositoryInfo(ICakeContext context)
    {
        context.Information("Testing to see if valid git repository...");

        if (context.GitIsValidRepository(context.MakeAbsolute(context.Environment.WorkingDirectory)))
        {

            // Normally, would use BuildParameters.RootDirectoryPath here, but since
            // BuildProvider is executed before the Setup Task has executed, this property
            // is null.  Default to the current working directory for this test.
            var rootPath = context.GitFindRootFromPath(context.MakeAbsolute(context.Environment.WorkingDirectory));

            var gitTool = context.Tools.Resolve("git");
            if (gitTool == null)
            {
                gitTool = context.Tools.Resolve("git.exe");
            }

            if (gitTool == null)
            {
                context.Warning("Unable to find git, setting default values for repository properties...");
                Branch = "unknown";
                Name = "Local";
                Tag = new LocalBuildTagInfo(false, "unknown");
            }
            else
            {
                context.Information("What version of git are we using...");
                context.StartProcess("git", new ProcessSettings { Arguments = "--version" });

                context.Information("Getting current branch name...");
                IEnumerable<string> redirectedStandardOutput;
                IEnumerable<string> redirectedError;

                var exitCode = context.StartProcess(
                    "git",
                    new ProcessSettings {
                        Arguments = "branch --show-current",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    out redirectedStandardOutput,
                    out redirectedError
                );

                if (exitCode == 0)
                {
                    var lines = redirectedStandardOutput.ToList();
                    if (lines.Any())
                    {
                        Branch = lines.FirstOrDefault();
                        context.Information("Branch name is {0}", Branch);
                    }
                }
                else
                {
                    context.Error("Unable to find branch name!");
                    context.Information("Writing out standard out...");
                    var standardOutLines = redirectedStandardOutput.ToList();
                    foreach (var standardOutLine in standardOutLines)
                    {
                        context.Information(standardOutLine);
                    }

                    context.Information("Writing out standard error...");
                    var standardErrorLines = redirectedError.ToList();
                    foreach (var standardErrorLine in standardErrorLines)
                    {
                        context.Information(standardErrorLine);
                    }

                    Branch = "unknown";
                }

                Name = "Local";
                Tag = new LocalBuildTagInfo(context);
            }
        }
        else
        {
            context.Warning("Unable to locate git repository, setting default values for repository properties...");

            Branch = "unknown";
            Name = "Local";
            Tag = new LocalBuildTagInfo(false, "unknown");
        }
    }

    public string Branch { get; }

    public string Name { get; }

    public ITagInfo Tag { get; }
}

public class LocalBuildPullRequestInfo : IPullRequestInfo
{
    public LocalBuildPullRequestInfo()
    {
        IsPullRequest = false;
    }

    public bool IsPullRequest { get; }
}

public class LocalBuildBuildInfo : IBuildInfo
{
    public LocalBuildBuildInfo()
    {
        Number = "-1";
    }

    public string Number { get; }
}

public class LocalBuildBuildProvider : IBuildProvider
{
    public LocalBuildBuildProvider(ICakeContext context)
    {
        Build = new LocalBuildBuildInfo();
        PullRequest = new LocalBuildPullRequestInfo();
        Repository = new LocalBuildRepositoryInfo(context);

        _context = context;
    }

    public IRepositoryInfo Repository { get; }

    public IPullRequestInfo PullRequest { get; }

    public IBuildInfo Build { get; }

    public bool SupportsTokenlessCodecov { get; } = false;

    public BuildProviderType Type { get; } = BuildProviderType.Local;

    public IEnumerable<string> PrintVariables { get; }

    private readonly ICakeContext _context;

    public void UploadArtifact(FilePath file)
    {
        _context.Information("Uploading artifact from path: {0}", file.FullPath);
        var destinationFile = BuildParameters.Paths.Directories.Build.Combine("Artifacts").CombineWithFilePath(file.GetFilename());
        _context.EnsureDirectoryExists(destinationFile.GetDirectory());
        _context.CopyFile(file, destinationFile);
    }
}
