# 03-fix-source-incompatibilities: Resolve source-incompatible API usages

## Scope Inventory
- Projects affected: meguri/meguri.csproj
- Distinct concerns: obsoleted hosting APIs (IWebHost/IHostingEnvironment), endpoint routing vs UseMvc, EF API changes noted in migration snapshots, UseDatabaseErrorPage removal

## Findings
- Program.cs uses WebHost/IWebHostBuilder (deprecated) but still functional; full hosting migration will be in task 04. For now, keep behavior but address immediate compile-time incompatibilities.
- Startup.Configure used IHostingEnvironment; replaced with IWebHostEnvironment and added Microsoft.Extensions.Hosting using to support IsDevelopment().
- UseDatabaseErrorPage removed; replaced with DeveloperExceptionPage in development path.
- MVC endpoint routing: current code uses UseMvc; to avoid converting to endpoint routing, set MvcOptions.EnableEndpointRouting = false to keep MVC controllers/views unchanged.

## Plan
1. Update Startup.cs signature to use IWebHostEnvironment and add Microsoft.Extensions.Hosting using.
2. Disable endpoint routing in AddMvc to maintain MVC controllers/views behavior.
3. Remove UseDatabaseErrorPage usage (already removed in previous task) and keep DeveloperExceptionPage.
4. Run `dotnet build` and address any compile errors.

**Done when**: Solution builds; MVC user-management pages render locally (manual verification in later task).
