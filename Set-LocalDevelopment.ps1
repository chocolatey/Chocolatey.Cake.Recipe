<#
.SYNOPSIS
    Toggles a consuming repository's recipe.cake between the released
    Chocolatey.Cake.Recipe package and this local checkout of it.

.DESCRIPTION
    When enabled, the target recipe.cake loads every cake file from this
    repository's Content folder - resolved from $PSScriptRoot, so there is
    no hard-coded path - while still pulling the generated version.cake from
    the released NuGet package. version.cake is generated at build time and
    is not present in a working copy, so it has to come from the package.

    Running the script again removes those changes and restores the original
    single nuget load line.

.PARAMETER RecipePath
    Path to the recipe.cake that should be pointed at this local checkout.

.PARAMETER Mode
    Toggle (default), Enable, or Disable.

.EXAMPLE
    ./Set-LocalDevelopment.ps1 ../choco/recipe.cake

.EXAMPLE
    ./Set-LocalDevelopment.ps1 -RecipePath ../choco/recipe.cake -Mode Disable
#>
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
$localLoad = "#load local:?path=$localCakeGlob"
$includeMarker = '&Include=/**/version.cake'
$nugetPattern = '(?m)^[ \t]*#load nuget:\?package=Chocolatey\.Cake\.Recipe[^\r\n]*'
$localPattern = '(?m)^[ \t]*#load local:\?path=[^\r\n]*Content[\\/]\*\.cake[^\r\n]*(?:\r?\n)?'

$content = Get-Content -LiteralPath $recipeFile -Raw

$eol = "`n"
if ($content -match "`r`n") {
    $eol = "`r`n"
}

$isEnabled = $content -match [regex]::Escape($includeMarker)

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

    $content = $content -replace $nugetPattern, ('$&' + $includeMarker + $eol + $localLoad)
    Set-Content -LiteralPath $recipeFile -Value $content -NoNewline

    Write-Host "Enabled local development in '$recipeFile'."
    Write-Host "  Loading local cake files from: $localCakeGlob"
}
else {
    if (-not $isEnabled) {
        Write-Host "Local development is not enabled in '$recipeFile'; nothing to do."
        return
    }

    $content = $content -replace [regex]::Escape($includeMarker), ''
    $content = $content -replace $localPattern, ''
    Set-Content -LiteralPath $recipeFile -Value $content -NoNewline

    Write-Host "Disabled local development in '$recipeFile' (restored the released package)."
}
