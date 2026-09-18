<#
.SYNOPSIS
    Toggles a consuming repository's recipe.cake between the released
    Chocolatey.Cake.Recipe package and this local checkout of it.

.DESCRIPTION
    When enabled, the target recipe.cake loads every cake file from this
    repository's Content folder - resolved from $PSScriptRoot, so there is
    no hard-coded path - while still loading the generated version.cake from
    the released NuGet package.

    version.cake is generated at build time (by the Generate-Version-File
    task), is git-ignored, and is not part of a fresh checkout, so it is
    taken from the package. If a local copy exists because this repository
    has been built, the local *.cake glob would load it a second time and the
    build fails on a duplicate BuildMetaData definition - so Enable removes
    that generated local file. It regenerates the next time this repository is
    built.

    The package is still restored while enabled (only version.cake is loaded
    from it), so the PowerShell helpers it ships under tools/ remain available.

    To undo, run with -Mode Disable, or - if you have no other local changes to
    the file - simply 'git restore recipe.cake'.

.PARAMETER RecipePath
    Path to the recipe.cake that should be pointed at this local checkout.

.PARAMETER Mode
    Toggle (default), Enable, or Disable.

.EXAMPLE
    ./Set-LocalDevelopment.ps1 ../choco/recipe.cake

.EXAMPLE
    ./Set-LocalDevelopment.ps1 -RecipePath ../choco/recipe.cake -Mode Disable
#>
#Requires -Version 5.1
[CmdletBinding()]
Param(
    [Parameter(Mandatory = $true, Position = 0)]
    [String]
    $RecipePath,

    [Parameter()]
    [ValidateSet('Toggle', 'Enable', 'Disable')]
    [String]
    $Mode = 'Toggle'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RecipePath -PathType Leaf)) {
    throw "Could not find a recipe.cake at '$RecipePath'."
}

$recipeFile = (Resolve-Path -LiteralPath $RecipePath).Path
$contentPath = Join-Path -Path $PSScriptRoot -ChildPath 'Chocolatey.Cake.Recipe/Content'

if (-not (Test-Path -LiteralPath $contentPath -PathType Container)) {
    throw "Could not find the Content folder at '$contentPath'. Run this script from within the Chocolatey.Cake.Recipe repository."
}

$localCakeGlob = ((Join-Path -Path $contentPath -ChildPath '*.cake') -replace '\\', '/')
$versionCake = Join-Path -Path $contentPath -ChildPath 'version.cake'
$includeMarker = '&Include=/**/version.cake'
$nugetPattern = '(?m)^(?<indent>[ \t]*)#load[ \t]+(?<ref>nuget:\?package=Chocolatey\.Cake\.Recipe[^\r\n]*)'
$includePattern = '&Include=/\*\*/version\.cake'
$localPattern = '\r?\n[ \t]*#load[ \t]+local:\?path=[^\r\n]*'

$content = Get-Content -LiteralPath $recipeFile -Raw

$eol = "`n"
if ($content -match "`r`n") {
    $eol = "`r`n"
}

$isEnabled = $content -match $includePattern

if ($Mode -eq 'Toggle') {
    if ($isEnabled) {
        $Mode = 'Disable'
    }
    else {
        $Mode = 'Enable'
    }
}

if ($Mode -eq 'Enable') {
    if ($isEnabled) {
        Write-Host "Local development is already enabled in '$recipeFile'."
        return
    }

    if ($content -notmatch $nugetPattern) {
        throw "No '#load nuget:?package=Chocolatey.Cake.Recipe...' directive was found in '$recipeFile'."
    }

    $replacement = '${indent}#load ${ref}' + $includeMarker + $eol + '${indent}#load local:?path=' + $localCakeGlob
    $content = $content -replace $nugetPattern, $replacement
    Set-Content -LiteralPath $recipeFile -Value $content -NoNewline

    if (Test-Path -LiteralPath $versionCake -PathType Leaf) {
        Remove-Item -LiteralPath $versionCake -Force
        Write-Host "Removed generated '$versionCake' so it is not loaded twice; it regenerates next time Chocolatey.Cake.Recipe is built."
    }

    Write-Host "Enabled local development in '$recipeFile'."
    Write-Host "  Loading local cake files from: $localCakeGlob"
}
else {
    if (-not $isEnabled) {
        Write-Host "Local development is not enabled in '$recipeFile'; nothing to do."
        return
    }

    $content = $content -replace $includePattern, ''
    $content = $content -replace $localPattern, ''
    Set-Content -LiteralPath $recipeFile -Value $content -NoNewline

    Write-Host "Disabled local development in '$recipeFile' (restored the released package)."
}
