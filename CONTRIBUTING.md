# Contributing

Fishing Assistant 3 is developed on the `development` branch. The `main` branch is
release-ready and must only be updated through a reviewed pull request.

## Workflow

1. Start work from the latest `development` branch.
2. Keep each change focused on one feature, fix, or foundation concern.
3. Document user-visible behavior, configuration changes, ownership scope, and known
   compatibility limits.
4. Build and run the relevant tests before pushing.
5. Merge completed work into `development` for integration.
6. Merge `development` into `main` only through a release pull request.

## Local validation

```powershell
dotnet build FishingAssistant.slnx --configuration Debug
dotnet build FishingAssistant.slnx --configuration Release
```

Both builds must complete without new project warnings. Development builds are
configured not to deploy into the installed game.

Gameplay changes also require manual validation in every affected play mode. See the
[release matrix](docs/ROADMAP.md#release-test-matrix) and the feature PR definition of
done in the roadmap.

## Source and reference policy

- Don't commit game assemblies, decompiled game code, local tools, IDE metadata, build
  output, saves, logs, or locally installed mods.
- Don't copy proprietary game code into the project.
- Follow and retain the license of any open-source code reused substantially, and add
  clear attribution.
- Prefer supported SMAPI APIs and public game members. Reflection or Harmony use must be
  isolated and justified in an architecture decision record.

## Commit style

Use concise conventional prefixes where they help communicate intent:

- `feat:` user-visible functionality;
- `fix:` defect correction;
- `refactor:` behavior-preserving restructuring;
- `test:` test-only changes;
- `docs:` documentation;
- `build:` build or dependency changes;
- `chore:` repository maintenance.
