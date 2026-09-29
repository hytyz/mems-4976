Municipal Elections Management System
=====================================

Team
----
Polina Omelyantseva

Contributions
-------------
Polina Omelyantseva:
- Designed the EF Core Code First domain model (Municipality, Position, Candidate, Endorsement, MunicipalityAdmin)
- Scaffolded the ASP.NET Core MVC solution and generated the InitialCreate migration with the EF Core CLI
- Implemented ASP.NET Core Identity authentication/authorization with SuperAdmin and municipality-scoped admin roles
- Built public candidate directory (search, filter, pagination), candidate comparison, and dark mode
- Built the session-based personal voting guide with printer view and PDF export
- Implemented admin area CRUD for municipalities, positions, and candidates, including image upload
- Created the dev container, helper scripts, and this documentation
- Deployed the application to Azure App Service with the az CLI
What has not been completed
---------------------------
- GitHub repository has not been created/pushed (local history exists, no remote yet)
- Final PDF/Word documentation document (project description, ERD, screenshots)
- Azure deployment screenshot
- Zipped D2L submission
- No automated test project
- No standalone "browse all positions" page; positions are browsed per municipality
- Public self-registration is enabled (self-registered users receive no role)

See docs/completion-report.md for the full requirement-by-requirement status.

Major challenges encountered
----------------------------
- .NET SDK was not installed on the host, so all development ran inside a Podman
  dev container using the mcr.microsoft.com/dotnet/sdk:10.0 image.
- The ASP.NET code generator does not support scaffolding into an Area directly;
  Admin controllers were generated into the target folder/namespace and their views
  were authored by hand.

Special instructions for testing the application
------------------------------------------------
1. Install Podman (rootless) and pull the .NET 10 SDK image:
     podman pull mcr.microsoft.com/dotnet/sdk:10.0
2. Run the application:
     ./scripts/run.sh
   or run a single dotnet command inside the container:
     ./scripts/dotnet.sh <args>
3. Open http://localhost:5000.
   Seed data (roles, users, municipalities, positions, candidates) is created
   automatically on first launch.
4. Login accounts:
     super / P@$$w0rd   (Super Admin)
     pm    / P@$$w0rd   (Pitt Meadows Admin)
5. The SQLite database is created at src/MunicipalElections/municipal-elections.db
   when running locally.

Note on Podman: when these scripts run from inside the VS Code snap, the snap leaks
its revision-specific XDG_DATA_HOME and Podman fails with a container storage
"database configuration mismatch". scripts/_podman-env.sh repoints XDG_DATA_HOME,
XDG_CONFIG_HOME and XDG_CACHE_HOME at the real home directories, and both
scripts/dotnet.sh and scripts/run.sh source it automatically. No manual action is
required.

Deployed application URL
------------------------
https://mems-assignment-app.azurewebsites.net
