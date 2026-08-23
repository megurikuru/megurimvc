# 01-update-target-framework: Update project target framework to net10.0

## Scope Inventory
- Projects affected: meguri/meguri.csproj (single project)
- Distinct concerns: project file properties, obsolete package references (Microsoft.AspNetCore.All), legacy DotNetCliToolReference entries
- Change signals: assessment reported netcoreapp2.0 target and presence of Microsoft.AspNetCore.All and DotNetCliToolReference entries

## Findings
- Project is SDK-style and currently targets netcoreapp2.0.
- Contains `Microsoft.AspNetCore.All` (obsolete for modern TFMs) and several `DotNetCliToolReference` entries that are not supported by newer SDKs.

## Plan
1. Update TargetFramework to `net10.0`.
2. Remove `Microsoft.AspNetCore.All` PackageReference and any `DotNetCliToolReference` entries. Package upgrades will be performed in task 02.
3. Run `dotnet restore` and then `dotnet build` to validate restore and basic compilation.

**Done when**: meguri.csproj targets `net10.0`; `dotnet restore` completes without fatal package resolution errors.
