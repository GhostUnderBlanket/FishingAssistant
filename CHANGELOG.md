# Changelog

All notable changes to Fishing Assistant 3 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial SMAPI project scaffold for Fishing Assistant 3.
- Startup log confirming that the mod loaded.
- Development roadmap and project documentation.
- A typed version 3 configuration schema with legacy-compatible keys and keybinds.
- Configuration normalization, migration reporting, editable drafts, and unit tests.
- Protection against overwriting configuration files created by a newer schema.
- Game-registry validation for bait, tackle, starter rod, and junk-ignore item IDs.
- Warnings for dependent settings that are inactive or overridden.
- Safe field-level recovery for unknown enum names and non-destructive fallback for
  unreadable configuration files.
- Initial responsive custom configuration menu with draft-based automation checkboxes,
  Apply, Cancel, Reset Defaults, mouse input, and controller navigation.
- Configuration-menu opening through a configurable keybind or the `fa_config` console
  command.
- Responsive category navigation and scrolling for all boolean configuration settings.

### Fixed

- Category and scrolling arrows now use game sprites instead of font glyphs, avoiding
  the left arrow rendering as a heart in Stardew Valley's dialogue font.
