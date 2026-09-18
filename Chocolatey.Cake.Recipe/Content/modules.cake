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

#module nuget:?package=Cake.BuildSystems.Module&version=0.3.1
#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0

// Required so that the "#tool choco:" pre-processor directive can be
// used to install Chocolatey packages, for example the vt-cli package
// used by the Submit-To-VirusTotal task. A module has to exist on disk
// before Cake runs, so unlike a tool it can't be installed on demand,
// and therefore has to be declared here rather than being pulled in only
// by the builds which need it.
#module nuget:?package=Cake.Chocolatey.Module&version=0.3.0