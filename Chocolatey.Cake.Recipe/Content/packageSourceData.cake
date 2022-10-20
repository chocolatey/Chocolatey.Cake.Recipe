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

public enum FeedType
{
    Chocolatey,
    NuGet
}

public class PackageSourceData
{
    public string Name { get; set; }
    public string PushUrl { get; set; }
    public FeedType Type { get; set; }
    public bool IsRelease { get; set; }
    public PackageSourceCredentials Credentials { get; private set; }

    public PackageSourceData(ICakeContext context, string name, string pushUrl)
        : this(context, name, pushUrl, FeedType.NuGet)
    {
    }

    public PackageSourceData(ICakeContext context, string name, string pushUrl, FeedType feedType)
        : this(context, name, pushUrl, feedType, true)
    {
    }

    public PackageSourceData(ICakeContext context, string name, string pushUrl, FeedType feedType, bool isRelease)
    {
        Name = name;
        PushUrl = pushUrl;
        Type = feedType;
        IsRelease = isRelease;

        Credentials = new PackageSourceCredentials(
            context.EnvironmentVariable(Name.ToUpperInvariant() + "_API_KEY"),
            context.EnvironmentVariable(Name.ToUpperInvariant() + "_USER"),
            context.EnvironmentVariable(Name.ToUpperInvariant() + "_PASSWORD")
        );
    }
}