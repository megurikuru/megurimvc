# 03-fix-source-incompatibilities: Resolve source-incompatible API usages

Address API changes reported in the assessment (IWebHost/IHostingEnvironment, DatabaseErrorPage, Identity EF helper APIs, etc.). Update Startup/Program, DI registration, and any code referencing obsolete types to their net10.0 equivalents. Keep user-management UI in MVC: adjust middleware and compatibility shims as needed to continue serving MVC controllers/views.

**Done when**: Solution compiles and unit/integration tests (if present) run; MVC user-management pages render locally without runtime errors.
