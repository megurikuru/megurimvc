# meguri

This project was upgraded to target .NET 10.0.

Notes:
- Target framework: net10.0
- MVC-based user-management UI kept as controllers + views; no migration to Razor Pages was performed.
- Known warnings/advisories:
  - Newtonsoft.Json was upgraded to 13.0.3 to address a high-severity advisory.
  - Some packages (e.g., SQLitePCLRaw.lib.e_sqlite3, NuGet.Packaging, NuGet.Protocol) show advisory warnings and may require further review.

Runtime:
- Requires .NET 10 runtime to run locally or in production.

See .github/upgrades/scenarios/dotnet-version-upgrade/ for full upgrade artifacts (assessment, plan, tasks, progress).