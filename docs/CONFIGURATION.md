# Configuration

Fishing Assistant 3 stores its settings in SMAPI's standard `config.json`. The current
schema version is `3`. Saved configuration, the active validated configuration, and an
editable menu draft are treated as separate objects.

## Compatibility and migration

The version 3 schema keeps the Fishing Assistant 2 property names where their meaning
is still supported. Legacy enum names remain JSON strings, while single-button legacy
keybinds load into SMAPI `KeybindList` values. A version 2 file without `ConfigVersion`
is identified as legacy, normalized, assigned version 3, and written back through
SMAPI.

Unknown top-level properties are reported in the SMAPI log before a migrated file is
written. They are not silently treated as supported options. A file whose
`ConfigVersion` is newer than 3 is loaded read-only: its schema version is not
downgraded, the file is not automatically rewritten, and applying a draft is blocked
to avoid discarding future data.

## Validation

Numeric settings are clamped to supported ranges. Missing keybinds, invalid enum
values, blank item preferences, whitespace, and duplicate ignore-list entries are
normalized to deterministic safe values. Every correction and unsupported property is
reported through SMAPI logging.

Item preference strings currently preserve the legacy values after whitespace
normalization. Registry-aware item ID validation will be added before gameplay systems
consume those values.

## Draft behavior

`ConfigManager.CreateDraft` creates an independent deep copy of mutable configuration
members. The custom menu will edit only this draft. Applying validates and persists a
copy before replacing the active configuration; canceling can discard the draft
without changing runtime or saved settings.
