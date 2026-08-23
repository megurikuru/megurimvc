# 04-update-hosting-and-startup: Migrate hosting model to generic host patterns if required

Migrate WebHost/Startup patterns to the current generic host (IHost/IHostBuilder/Program.Main minimal hosting) where beneficial, ensuring MVC controllers and views continue to function.

**Done when**: Application starts locally and serves MVC routes. No 500 errors for startup operations.
