# .NET Version Upgrade

## Strategy
Upgrade the single project in-place to net10.0. Preserve MVC-based user-management UI; do not convert to Razor Pages.

## Preferences
- **Flow Mode**: Automatic
- **Commit Strategy**: After Each Task
- **Target Framework**: net10.0

## Decisions
- Keep user-management UI as MVC (controllers + views) — user request

## Custom Instructions
- For tasks that touch UI, ensure MVC controllers and views are not migrated to non-MVC patterns unless explicitly required.
