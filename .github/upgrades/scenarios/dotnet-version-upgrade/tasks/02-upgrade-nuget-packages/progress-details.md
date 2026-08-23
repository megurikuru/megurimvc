# Progress Details — 02-upgrade-nuget-packages

## What changed
- Added explicit PackageReference for `Newtonsoft.Json` v13.0.3 to address high-severity advisory on older versions.
- Added `Microsoft.EntityFrameworkCore.Sqlite` v10.0.0 to satisfy `UseSqlite` APIs.

## Build and restore
- `dotnet restore` succeeded with warnings (some packages have low/medium advisories reported: NuGet.Packaging, NuGet.Protocol, SQLitePCLRaw.lib.e_sqlite3).
- `dotnet build` succeeded with warnings. Warnings include obsolete APIs (IWebHost/IHostingEnvironment) and EF migration snapshot obsolete APIs. These will be addressed in subsequent tasks.

## Issues encountered
- Needed to add EF Sqlite provider to restore UseSqlite extension.
- Database error page API (`UseDatabaseErrorPage`) removed in modern ASP.NET Core; replaced with DeveloperExceptionPage for development diagnostics.

## Next steps
- Task 03: Fix source-incompatible API usages (IWebHost/IHostingEnvironment, endpoint routing vs UseMvc, obsolete EF APIs) while preserving MVC user-management UI.

