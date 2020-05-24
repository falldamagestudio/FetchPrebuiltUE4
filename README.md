
# Fetch Prebuilt UE4 tool

This tool makes it easy to use pre-built UE4 Engine binaries in an Unreal project.

It allows everyone on the team to fetch the appropriate version of UE4 by running a single command.

The UE4 builds can be stored either in local storage or in a Google Cloud Storage bucket.

This wrapper helps users with authentication if files are kept in Google Cloud Storage. They can either use a pre-shared service account, or they can authenticate using their personal Google accounts.

# Related projects

[longtail](https://github.com/DanEngelbrecht/longtail/) is the C library that does the heavy lifting.

[golongtail](https://github.com/DanEngelbrecht/golongtail/) is a CLI utility that wraps longtail, provides a good commandline experience, and provides GCS up/download capabilities.

[LongtailLib](https://github.com/DanEngelbrecht/LongtailLib/) is a C# wrapper library for longtail.

# Typical usage

## Google Cloud setup

- Create a project in Google Cloud.
- Configure an OAuth consent screen of Internal type. Change no settings, use default scopes etc.
- Create an OAuth 2.0 Client ID for Desktop.
- Create a Cloud Storage bucket.
- Grant users in your organization access to the bucket.

## FetchPrebuiltUE4 authentication configuration

- If you want to use a Service Account, then download JSON credentials and put them into a file called `default-application-credentials.json` next to `FetchPrebuiltUE4`.
- Otherwise, you will be prompted to log in using a regular user account the first time that you access Google Cloud via `FetchPrebuiltUE4`.

## Build & upload UE4

- Build UE4, for example by using BuildGraph: `Engine\Build\BatchFiles\RunUAT.bat BuildGraph -Script="Engine/Build/InstalledEngineBuild.xml" -Target="Make Installed Build Win64" -set:HostPlatformOnly=true -set:WithServer=true -set:WithDDC=false -set:VS2019=true`
- Upload UE4 build to storage, `FetchPrebuiltUE4 --folder <UE4 engine folder> --package <version identification of your UE4 build>`.
- Create a `DesiredUE4Version.json` file next to `FetchPrebuiltUE4`, with the version identification of your UE4 build.

## Add FetchPrebuiltUE4 to Unreal project

- Add `FetchPrebuiltUE4` to source control.
- Create a `DesiredUE4Version.json` file next to `FetchPrebuiltUE4`, with the version identification of your UE4 build.
- Add `application-default-credentials.json` and `InstalledUE4Version.json` to the ignore list within the source control system.
- Add a `FetchPrebuiltUE4.config.json` file. Point it to the folder where the game project expects the UE4 engine to reside.
- Validate all the configuration by doing `FetchPrebuiltUE4 update-local-ue4-version` twice. The first time, UE4 will be downloaded; the second time, the tool will exit early saying that the UE4 version already is installed.

All configuration is now complete.

## Daily usage

Anyone on the team who wants to "get latest" for the UE4 version needs to execute this command: `FetchPrebuiltUE4 update-local-ue4-version`. That's all!

# License

This tool's license is available in [LICENSE.txt](LICENSE.txt). See [longtail](https://github.com/DanEngelbrecht/longtail) and [golongtail](https://github.com/DanEngelbrecht/golongtail) for licenses of the software it depends on.

# Cheers

Thanks to [Frank Olbricht](https://github.com/folbricht), [Dan Engelbrecht](https://github.com/DanEngelbrecht) and Embark Studios for creating high quality open-source software!
