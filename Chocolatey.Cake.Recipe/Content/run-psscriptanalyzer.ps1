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
    $AnalyzePath,

    [Parameter()]
    [String]
    $SettingsPath,

    [Parameter()]
    [String]
    $OutputPath,

    [Parameter()]
    [String[]]
    $ExcludePaths
)

#Requires -Modules PSScriptAnalyzer, ConvertToSARIF

Push-Location -Path $AnalyzePath

try {
    if ($PSBoundParameters.ContainsKey('ExcludePaths')) {
        $ExcludePaths = $ExcludePaths | Where-Object { Test-Path $_ } | ForEach-Object { (Resolve-Path -Path $_).Path }
    }
}
finally {
    Pop-Location
}

$scripts = Get-ChildItem -Path $AnalyzePath -Filter "*.ps1" -Recurse | ForEach-Object {
    $found = $false
    if ($PSBoundParameters.ContainsKey('ExcludePaths')) {
        foreach ($path in $ExcludePaths) {
            if ($_.FullName.StartsWith($path)) {
                $found = $true
            }
        }
    }
    if (-not $found) {
        $_
    }
}

$modules = Get-ChildItem -Path $AnalyzePath -Filter "*.psm1" -Recurse | ForEach-Object {
    $found = $false
    if ($PSBoundParameters.ContainsKey('ExcludePaths')) {
        foreach ($path in $ExcludePaths) {
            if ($_.FullName.StartsWith($path)) {
                $found = $true
            }
        }
    }
    if (-not $found) {
        $_
    }
}

if ($null -ne $modules) {
    Write-Output "Analyzing module files..."

    $records = Start-Job -ArgumentList $modules, $SettingsPath {
        Param(
            $modules,
            $SettingsPath
        )
        $modules | Invoke-ScriptAnalyzer -Settings $SettingsPath | Select-Object RuleName, ScriptPath, Line, Message 
    } | Wait-Job | Receive-Job

    if (-not ($null -EQ $records)) {
        Write-Output "Violations found in Module Files..."
        $records | Format-List | Out-String

        Write-Output $OutputPath

        Write-Output "Writing violations to output file..."
        $records | ConvertTo-SARIF -FilePath "$OutputPath\modules.sarif"
    }
    else {
        Write-Output "No rule violations found in Module Files."
    }
}
else {
    Write-Output "No Module Files to analyze"
}

if ($null -ne $scripts) {
    Write-Output "Analyzing script files..."

    $records = Start-Job -ArgumentList $Scripts, $SettingsPath {
        Param(
            $Scripts,
            $SettingsPath
        )
        $Scripts | Invoke-ScriptAnalyzer -Settings $SettingsPath | Select-Object RuleName, ScriptPath, Line, Message 
    } | Wait-Job | Receive-Job

    if (-not ($null -EQ $records)) {
        Write-Output "Violations found in Script Files..."
        $records | Format-List | Out-String

        Write-Output "Writing violations to output file..."
        $records | ConvertTo-SARIF -FilePath "$OutputPath\scripts.sarif"
    }
    else {
        Write-Output "No rule violations found in Script Files."
    }
}
else {
    Write-Output "No Script Files to analyze"
}

Write-Output "Analyzing complete."