# .NET Version Upgrade Plan

## Overview

**Target**: meguri solution → net10.0
**Scope**: 1 project (meguri), ~3k LOC. Upgrade project TFM and minimal code changes required to compile on .NET 10. Keep the user-management UI implemented with ASP.NET MVC views/controllers (no conversion to Razor Pages required).

## Tasks

### 01-update-target-framework: Update project target framework to net10.0

Update the project's TargetFramework/TargetFrameworks to `net10.0` in the project file(s). Ensure SDK-style project file format is preserved and remove obsolete package references such as `Microsoft.AspNetCore.All` if present.

**Done when**: meguri.csproj targets `net10.0`; `dotnet restore` completes without fatal package resolution errors.

---

### 02-upgrade-nuget-packages: Upgrade NuGet packages to compatible versions

Upgrade packages flagged in the assessment (security/non-recommended packages and recommended upgrades). Prefer stable releases that support net10.0. Replace or remove deprecated packages (for example, remove `Microsoft.AspNetCore.All` and depend on specific ASP.NET Core packages or the meta package supported for net10.0).

**Done when**: All upgraded packages restore and the solution builds past package-related errors.

---

### 03-fix-source-incompatibilities: Resolve source-incompatible API usages

Address API changes reported in the assessment (IWebHost/IHostingEnvironment, DatabaseErrorPage, Identity EF helper APIs, etc.). Update Startup/Program, DI registration, and any code referencing obsolete types to their net10.0 equivalents. Keep user-management UI in MVC: adjust middleware and compatibility shims as needed to continue serving MVC controllers/views.

**Done when**: Solution compiles and unit/integration tests (if present) run; MVC user-management pages render locally without runtime errors.

---

### 04-update-hosting-and-startup: Migrate hosting model to generic host patterns if required

Migrate WebHost/Startup patterns to the current generic host (IHost/IHostBuilder/Program.Main minimal hosting) where beneficial, ensuring MVC controllers and views continue to function.

**Done when**: Application starts locally and serves MVC routes. No 500 errors for startup operations.

---

### 05-run-build-and-tests: Build, run tests, and fix remaining issues

Perform full solution build, run automated tests (if any), and manual smoke tests for user-management workflows. Fix any compilation or runtime errors introduced by TFMs or package upgrades.

**Done when**: Solution builds without errors, warnings addressed per policy (warnings fixed), and smoke tests pass.

---

### 06-finalize-and-document: Finalize changes and create PR

Update README/notes about the new target framework and any runtime requirements. Commit remaining changes per commit strategy and open a pull request with a concise description of changes and known issues.

**Done when**: PR created or changes are committed and pushed to the remote on branch `upgrade-dotnet-10`.

