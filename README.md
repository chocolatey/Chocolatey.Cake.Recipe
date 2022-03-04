# Chocolatey.Cake.Recipe

This repository contains a set of opinionated Cake Scripts which are used to build some of the projects for Chocolatey.

## Requirements

In order to run the Chocolatey Cake Recipe build scripts, it is necessary to have at least git 2.22.0 installed on the machine
that is doing the build.  If this isn't the case, then the build will fail with an object reference error.  This is due
to the fact that the build runs the following git command:

```
git branch --show-current
```

which only became available in git 2.22.0.

## Usage

In order to consume these scripts, add the following to your initial build script:

```
#load nuget:?package=Chocolatey.Cake.Recipe
```

If you need to use a pre-release version of the package, you can use:

```
#load nuget:?package=Chocolatey.Cake.Recipe&prerelease
```

And if you need to use a specific version of the package, you can use something like:

```
#load nuget:?package=Chocolatey.Cake.Recipe&prerelease&version=0.1.0-unstable0026
```

## Push Sources

The Chocolatey Cake Recipe has the concept of having multiple sources where
generated NuGet and Chocolatey Packages can be pushed to.  There are two `types`
of Sources that can be created:

- Pre-release
- Release

Pre-release sources are pushed to whenever a `Continuous-Integration` build
completes have packages have been generated.  Release sources are pushed to
whenever the commit that is currently being built also has a tag.

By default, the Chocolatey Cake Recipe configures the same defined source as the
destination for both NuGet and Chocolatey packages, for both pre-release and
release packages.  This configuration starts with the definition of the
`DefaultPushSourceUrlVariable` which by default is equal to `NUGETDEV_SOURCE`.
This name of this environment variable can be changed to anything you want by
calling the `Environment.SetVariableNames` method in your recipe.cake file, and
setting the `defaultPushSourceUrlVariable` parameter.  This environment variable
name follows a convention of `<SOURCENAME>_SOURCE`.  i.e. in the case of the
default value, the name of the source is NUGETDEV.

With this environment variable set, the default package sources are configured
as follows:

```
PackageSources.Add(new PackageSourceData(context, defaultPushSourceUrlParts[0], defaultPushSourceUrl));
PackageSources.Add(new PackageSourceData(context, defaultPushSourceUrlParts[0], defaultPushSourceUrl, FeedType.Chocolatey));
PackageSources.Add(new PackageSourceData(context, defaultPushSourceUrlParts[0], defaultPushSourceUrl, FeedType.NuGet, false));
PackageSources.Add(new PackageSourceData(context, defaultPushSourceUrlParts[0], defaultPushSourceUrl, FeedType.Chocolatey, false));
```

This configures a NuGet Pre-Release and Release source, as well as a Chocolatey
Pre-Release and Release feed, all pointing at the same URL.  This might not be
what you want, but it is the default setup used in the TeamCity builds currently.
If you want to override these then this is certainly possible by calling the
`BuildParameters.SetParameters` method in your recipe.cake file, passing in a
`List<PackageSourceData>()` with the configured values.

When it comes to getting the credentials required to push packages to these
sources, it is expected that these are provided via environment variables that
follow the same naming convention as the first environment variable.  i.e. it is
expected that there would be either:

`<SOURCENAME>_API_KEY`

or

`<SOURCENAME>_USER` and `<SOURCENAME>_PASSWORD`

So, using the default setup as an example, where an api key is being used to push
the package, it would be expected that there are two environment variables setup
for the build configuration:

* NUGETDEV_SOURCE - contains to the URL to the feed to push packages to
* NUGETDEV_API_KEY - contains the api key for pushing packages to the feed

If the necessary credentials aren't provided for a source, then no attempt is
made to push packages to that source.

## Arguments

There are a number of arguments which can be passed into the build when using the Chocolatey Cake Recipe.  These include:

### Target

This controls which task is executed as part of the build.

- Type: `string`
- Default Value: `Default`

Example

```
.\build.ps1 --target=Clean
```

### Build Counter

This controls how the generated package version looks like.  This allows for unique package versions to be generated and published to a NuGet/Chocolatey repository.  For example, in this `0.10.16-beta-20190913-19` the build counter value is 19.

- Type: `string`
- Default Value: `BuildProvider.Build.Number`

Example

```
.\build.ps1 --buildCounter=7
```

### Configuration

This controls what value is passed to MSBuild, to control what configuration is ultimately being built.

- Type: `string`
- Default Value: `Release`

Example

```
.\build.ps1 --configuration=Debug
```

### Deployment Environment

From time to time, it is necessary to "change" the published configuration files.  For example, it might be necessary to change the default logging level in a log configuration file.  In the Chocolatey Cake Recipe, this is done, by default, by placing `.settings` files into a `settings` folder in the root of the project repository.  This is a JSON file which includes all the transformations that need to be done.  It is possible to place multiple `.settings` files in this folder, for example `RELEASE.settings`, `QA.settings`, or `PRODUCTION.settings`.  The deployment environment argument controls which of these files will be used to create the final set of artifacts for the build.

- Type: `string`
- Default Value: `Release`

Example

```
.\build.ps1 --deploymentEnvironment=QA
```

### Force Continuous Integration

By default, the entry task for Chocolatey Cake Recipe is `Package`, meaning that all artifacts will be generated, but no attempt to publish these artifacts will happen.  When running on a CI environment, it is expected that you will run `.\build.ps1 --target=CI` which will also attempt to publish the artifacts.  While it is possible to also run this target locally, the publishing tasks will be skipped since not running in a CI environment.  To force this, you can run `.\build.ps1 --target=CI --forceContinuousIntegration=true`

- Type: `boolean`
- Default Value: `false`

Example

```
.\build.ps1 --target=CI --forceContinuousIntegration=true
```
