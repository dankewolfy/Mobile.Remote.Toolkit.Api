# Development setup

Quick commands to start both backend API and frontend UI.

PowerShell script (recommended):

```powershell
cd C:\Git\Mobile.Remote.Toolkit.Api
.\scripts\start-dev.ps1
```

VS Code: open the folder `Mobile.Remote.Toolkit.Api` and run the task `Run Full Stack` (Terminal → Run Task...).

Frontend: the Vue app runs by default at `http://localhost:5173` and opens automatically when using `dev:open` script.

Environment variables: copy `Mobile.Remote.Toolkit.Vue/.env.example` to `.env` and set `VITE_API_BASE_URL` if your API runs on a different URL.

## Publishing a single EXE that contains both API and built frontend

Use the included PowerShell helper to automate building the Vue app and publishing the API as a single-file executable.

From PowerShell (run from this folder):

```powershell
cd C:\Git\Mobile.Remote.Toolkit.Api
.\scripts\publish-exe.ps1 -Runtime win-x64 -Configuration Release -OutputDir .\Mobile.Remote.Toolkit\publish -Zip
```

- `-Runtime`: target runtime identifier (e.g. `win-x64`).
- `-Configuration`: `Release` or `Debug`.
- `-OutputDir`: custom output directory for publish artifacts.
- `-Zip`: optional switch to create a zip file of the publish folder.

Requirements: `dotnet`, `node`, `npm` must be available in PATH. The script will run `npm ci` and `npm run build` inside `Mobile.Remote.Toolkit.Vue`, then run `dotnet publish` for the API project. The result is a single-file executable in the specified output folder.
