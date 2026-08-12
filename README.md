# Fishing Assistant 3

Fishing Assistant 3 is a complete rewrite of Fishing Assistant 2, a configurable
fishing automation and assistance mod for Stardew Valley.

The project is currently in early alpha development. It isn't ready for normal gameplay
or migration from Fishing Assistant 2 yet.

## Goals

- Rebuild the useful Fishing Assistant 2 systems on a maintainable architecture.
- Provide a custom in-game configuration menu without requiring Generic Mod Config
  Menu.
- Support single-player, local split-screen, and multiplayer as release requirements.
- Preserve existing configuration where practical while fixing unsafe legacy behavior.
- Add new assistance options without taking control away from the player.

See the [development roadmap](docs/ROADMAP.md) for the planned milestones and release
criteria.

## Current status

The mod currently contains the SMAPI project scaffold and the first version of its
typed, validated configuration layer. Gameplay automation and the custom configuration
menu are not implemented yet. Development happens incrementally on the `development`
branch.

## Requirements

- Stardew Valley 1.6.15 or later in the supported 1.6 line.
- SMAPI 4.5.2 or later.
- .NET 6 SDK or a newer SDK capable of targeting .NET 6, for development.

## Build

The project uses
[`Pathoschild.Stardew.ModBuildConfig`](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)
to find the local Stardew Valley installation and reference the game and SMAPI
assemblies.

```powershell
dotnet build FishingAssistant.slnx --configuration Debug
```

Normal development builds do not deploy into the game's `Mods` directory and do not
create release ZIP files.

## Branch workflow

- `main` stays release-ready and is updated only through reviewed pull requests.
- `development` is the integration branch for active development.
- Pull requests should be small enough to review and must pass relevant validation
  before merging.

## Reference material

Development may use locally available game code and third-party open-source mods for
behavioral research. Proprietary decompiled game code and repository-local references
must never be committed or redistributed. Any substantial reuse from an open-source
project must follow its license and be attributed.

## License

Fishing Assistant is available under the [MIT License](LICENSE).
