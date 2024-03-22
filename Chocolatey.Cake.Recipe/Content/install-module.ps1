# Copyright © 2023 Chocolatey Software, Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
#
# You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

[cmdletBinding()]
Param(
    [Parameter()]
    [String]
    $ModuleName,

    [Parameter()]
    [String]
    $RequiredVersion
)

$FullyQualifiedName = @{ModuleName="$ModuleName";RequiredVersion="$RequiredVersion"}
if (Get-Module -ListAvailable -FullyQualifiedName $FullyQualifiedName) {
    Write-Host "The $ModuleName PowerShell Module with version $RequiredVersion is already installed."
}
else {
    Write-Host "Install Module $ModuleName with version $RequiredVersion..."
    # Bootstrap PowerShell Get
    if (-not (Get-PackageProvider NuGet -ErrorAction Ignore)) {
        Write-Host "Installing NuGet package provider"
        Install-PackageProvider NuGet -MinimumVersion 2.8.5.201 -ForceBootstrap -Force -Scope CurrentUser
    }

    if (-not (Get-InstalledModule PowerShellGet -MinimumVersion 2.0 -MaximumVersion 2.99 -ErrorAction Ignore)) {
        Install-Module PowerShellGet -MaximumVersion 2.99 -Force -AllowClobber -Scope CurrentUser
        Remove-Module PowerShellGet -Force
        Import-Module PowerShellGet -MinimumVersion 2.0 -Force
        Import-PackageProvider -Name PowerShellGet -MinimumVersion 2.0 -Force
    }

    Install-Module -Name $ModuleName -RequiredVersion $RequiredVersion -Force -Scope CurrentUser
}
