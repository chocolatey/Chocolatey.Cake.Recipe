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

public class BuildData
{
    private readonly List<IIssue> issues = new List<IIssue>();

    public DirectoryPath RepositoryRoot { get; }

    public IEnumerable<IIssue> Issues
    {
        get
        {
            return issues.AsReadOnly();
        }
    }

    public BuildData(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        RepositoryRoot = context.MakeAbsolute(context.Directory("./"));
    }

    public void AddIssues(IEnumerable<IIssue> issues)
    {
        this.issues.AddRange(issues);
    }
}
