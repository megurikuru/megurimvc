# 02-upgrade-nuget-packages: Upgrade NuGet packages to compatible versions

## Scope Inventory
- Projects affected: meguri/meguri.csproj
- Distinct concerns: packages with security advisories (Newtonsoft.Json 10.0.1), deprecated meta-packages (Microsoft.AspNetCore.All), tooling packages (codegen/tools)
- Change signals: assessment flagged NuGet.0002/0004/0005 issues for several packages

## Findings
- Newtonsoft.Json 10.0.1 is present transitively and has a reported high-severity advisory; add explicit PackageReference with a supported version.
- Code generation and EF tools were upgraded to 10.0.0 during task 01; verify compatibility and update if needed.

## Plan
1. Add or update explicit PackageReference entries for packages flagged in assessment (start with Newtonsoft.Json -> bump to 13.x stable).  
2. Run `dotnet restore` then `dotnet build` to validate package resolution.  
3. If restore/build reports API or compatibility issues, research and apply minimal code changes or adjust package versions.  
4. Document changes in progress-details.md.

**Done when**: All upgraded packages restore and the solution builds past package-related errors.
