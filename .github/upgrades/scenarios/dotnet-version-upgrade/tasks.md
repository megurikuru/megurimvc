# .NET Version Upgrade Progress

## Overview

Upgrade meguri solution from netcoreapp2.0 to net10.0. Strategy: in-place upgrade of the single project, preserving MVC user-management UI.

**Progress**: 5/6 tasks complete <progress value="83" max="100"></progress> 83%

## Tasks

- ✅ 01-update-target-framework: Update project target framework to net10.0 ([Content](tasks/01-update-target-framework/task.md), [Progress](tasks/01-update-target-framework/progress-details.md))
- ✅ 02-upgrade-nuget-packages: Upgrade NuGet packages to compatible versions ([Content](tasks/02-upgrade-nuget-packages/task.md), [Progress](tasks/02-upgrade-nuget-packages/progress-details.md))
- ✅ 03-fix-source-incompatibilities: Resolve source-incompatible API usages ([Content](tasks/03-fix-source-incompatibilities/task.md), [Progress](tasks/03-fix-source-incompatibilities/progress-details.md))
- ✅ 04-update-hosting-and-startup: Migrate hosting model to generic host patterns if required ([Content](tasks/04-update-hosting-and-startup/task.md), [Progress](tasks/04-update-hosting-and-startup/progress-details.md))
- ✅ 05-run-build-and-tests: Build, run tests, and fix remaining issues ([Content](tasks/05-run-build-and-tests/task.md), [Progress](tasks/05-run-build-and-tests/progress-details.md))
- 🔲 06-finalize-and-document: Finalize changes and create PR
