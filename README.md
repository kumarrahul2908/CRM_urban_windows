# Urban Dry Clean CRM - native Windows app

A true native Windows desktop app - WPF, .NET 8, C#. No browser engine, no Chromium, no
Electron: real Win32 windows rendered by WPF. It talks to the backend in
[kumarrahul2908/urban-dry-clean-crm-node](https://github.com/kumarrahul2908/urban-dry-clean-crm-node)
over its existing `/api/mobile/*` REST API - the same API the Android app already uses - so no
business logic is duplicated or rewritten here. **This repo contains only the Windows client.**
The backend (Node/Fastify, TypeScript) and its database schema live in, and are built from, that
other repo; `scripts/prepare-backend.cjs` clones and builds it at packaging time.

The backend runs locally on each shop PC (bundled, with its own portable Node.js runtime so
nothing extra needs installing), all pointed at the same central MySQL database as the web app
and the Android app.

An earlier Electron-based prototype (a Chromium+Node wrapper around the web UI) was tried first
and then dropped in favor of this true native UI.

## Phase plan

| Phase | Scope | Status |
|---|---|---|
| 1 Foundation | Solution scaffold (`Udc.Client` testable class library + `Udc.App` WPF UI); `AppConfig`/`ConfigStore`/`TokenStore` (DPAPI-encrypted); `BackendProcessManager` (bundles + launches a portable Node runtime); `DbConnectionTester` (native MySQL connection test, no backend needed); `ApiClient` (typed wrapper over `/api/mobile/*`); Setup window (first-run DB config + test), Splash window, Login window, main shell window (menu, sidebar) with a real **Dashboard** screen (stat cards + 7-day sales) backed by live data from `dashboard_summary`; CI build (`windows-latest`, self-contained `dotnet publish` + portable Node bundling + backend clone-and-build) | **done - confirmed green on `windows-latest` CI** (run [#6](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41279323189)): tests pass, `Udc.App` compiles, backend clones/builds/bundles, artifact produced |
| 2 Customers + Orders (view) | **Customers:** list (search + pagination), detail (contact info, price list, wallet balance, outstanding), create. **Orders:** list (search + pagination), detail (items, payments, status, balance due), Record Payment (single payment line). Sidebar navigation wired up (Dashboard / Customers / Orders). | **done (code) - not yet confirmed on CI**, see Verification below |
| 3 POS / order creation | New order screen (piece + laundry): garment/service catalog browsing, price-list-aware rates, cart, discounts, submit. This is the single biggest remaining screen (the real app's `OrderService.createOrder()` alone is ~2000 lines) - deliberately split out from Phase 2 rather than rushed. | planned |
| 4 Workshop + Barcode | QC/Ironing/Packaging queue screens, challans, barcode/tag scanning + printing | planned |
| 5 Reports + Admin | Reports, master data, admin settings, users/roles, WhatsApp admin | planned |
| 6 Packaging polish | Proper installer (MSIX or WiX) with Start Menu/Desktop shortcuts, code signing, auto-update, background jobs (WhatsApp worker, nightly backup, photo cleanup - see Known gaps) | planned |

## Architecture

```
.
├── Udc.sln
├── src/
│   ├── Udc.Client/            class library, plain net8.0 - builds and tests on any OS
│   │   ├── AppConfig.cs / ConfigStore.cs / Paths.cs   local config (%APPDATA%\UrbanDryCleanCRM)
│   │   ├── TokenStore.cs                              DPAPI-encrypted bearer token on disk
│   │   ├── BackendProcessManager.cs                   starts/stops the bundled Node backend
│   │   ├── DbConnectionTester.cs                       MySQL connection test (MySqlConnector)
│   │   ├── ApiClient.cs + Models/                      typed wrapper over /api/mobile/*
│   │   └── ApiException.cs
│   ├── Udc.Client.Tests/      xunit tests for all of the above (`dotnet test`)
│   └── Udc.App/                net8.0-windows, WPF - the actual UI (Windows-only to build)
│       ├── App.xaml(.cs)                               boot sequence / window orchestration
│       └── Views/               SetupWindow, SplashWindow, LoginWindow, MainWindow, DashboardView
└── scripts/
    └── prepare-backend.cjs     clones + builds urban-dry-clean-crm-node, bundles a portable Node
```

**Why a separate repo from the backend?** The backend (Node/Fastify/TypeScript, plus the DB
schema and the web UI) is actively developed in `urban-dry-clean-crm-node` with its own CI. This
app is a client of that backend's existing mobile REST API, not a fork of its code, so it lives
on its own with its own fast CI loop - a broken/red check here is always about this client, never
about unrelated backend work, and vice versa. `prepare-backend.cjs` clones and builds the backend
repo (pinned to `BACKEND_REF`, default `main`) purely to produce the `dist/`/`public/`/`db/` this
app bundles and runs locally - it is never edited from here.

**Why split `Udc.Client` out of `Udc.App`?** Everything that isn't actual UI (config, the API
client, the backend process manager, the DB connection test) lives in a plain `net8.0` class
library with no WPF dependency, so it builds and unit-tests on any OS - this repo's CI (and the
sandbox this was first built in) can verify it without a Windows machine. Only the WPF project
itself (`net8.0-windows`, `UseWPF=true`) needs Windows to build, because the WPF compiler
toolchain (`Microsoft.NET.Sdk.WindowsDesktop`) isn't available on Linux/macOS SDKs at all -
confirmed while building this: `dotnet build`/`dotnet restore` on anything that includes
`Udc.App` (including `Udc.sln` itself) fails immediately with the WindowsDesktop SDK pack
missing, before anything else runs.

**Bundled backend:** `BackendProcessManager` spawns
`<install dir>/backend/node/node.exe <install dir>/backend/dist/server.js` as a child process on
app startup, waits for `/health`, then shows the UI. `backend/node/node.exe` is a portable
official Node.js Windows build (just the one `.exe`, nothing else from the zip) staged by
`scripts/prepare-backend.cjs` - so the shop PC never needs Node.js installed separately. Every
backend runtime dependency (mysql2, fastify, bcryptjs, zod, ...) is pure JavaScript, so the
production `node_modules` staged alongside it run unmodified.

**Config & secrets:** `AppConfig` (DB host/port/name/user/password, app secret key, local port)
is collected by the Setup window, connection-tested directly from .NET via `MySqlConnector`
(no need to start the backend first), and saved as JSON to
`%APPDATA%\UrbanDryCleanCRM\config.json`. The bearer token from `POST /api/mobile/login` is
saved separately, encrypted at rest with Windows DPAPI (`TokenStore`, current-user scope) -
not yet wired into the boot sequence to auto-skip the login screen (see Known gaps).

**Existing PHP/web app secret key:** if this PC points at the same database the web app already
uses, paste the exact same `APP_SECRET_KEY` into Setup so encrypted values (WhatsApp tokens,
etc.) stay decryptable from both places.

## Verification

**Phase 1 is confirmed**: [CI run #6](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41279323189)
is fully green on `windows-latest` - `Udc.Client.Tests` (13/13), `Udc.App` compiles and
publishes, the private backend repo clones/builds, the portable Node runtime bundles, and the
`urban-dry-clean-crm-native-win-x64` artifact was produced. Getting there took two real,
Windows-only bugs a Linux sandbox could never have caught by review alone (see git history:
`BACKEND_REPO_TOKEN` auth and `execFileSync` needing `shell:true` for `npm` on Windows) - both
now fixed.

**Phase 2 (Customers + Orders view) is not yet confirmed** - built in the same Linux sandbox:

- `dotnet build` / `dotnet test` on `Udc.Client` + `Udc.Client.Tests`: **passing** (21 tests -
  the original 13 plus 8 new ones covering `GetCustomersAsync`/`GetCustomerDetailAsync`/
  `CreateCustomerAsync`/`GetOrdersAsync`/`GetOrderDetailAsync`/`GetPaymentTypesAsync`/
  `RecordPaymentAsync` against a fake `HttpMessageHandler`, including the duplicate-customer
  error path and query-string building).
- The new WPF screens (`CustomersView`, `CustomerDetailWindow`, `CustomerFormWindow`,
  `OrdersView`, `OrderDetailWindow`, `RecordPaymentWindow`) and the `MainWindow` sidebar
  navigation wiring: **written and reviewed by hand, not yet compiled anywhere** - same
  Windows-only-toolchain limitation as Phase 1. Treat this phase as reviewed-but-unverified until
  the next CI run is green; given Phase 1's experience, don't be surprised if a Windows-only
  issue turns up that this review couldn't catch.

## Building

### One-time setup: a token to read the backend repo
`kumarrahul2908/urban-dry-clean-crm-node` is **private**, so CI's own `GITHUB_TOKEN` (scoped only
to this repo) can't clone it, and an anonymous `git clone` fails outright. Add a repo secret
named `BACKEND_REPO_TOKEN` here (Settings > Secrets and variables > Actions > New repository
secret) holding a token with read access to that repo - a fine-grained PAT scoped to just
`urban-dry-clean-crm-node` with "Contents: Read" is enough. Without it, the "Stage the backend"
step fails with a git auth error.

### CI (recommended - works without a Windows machine)
`.github/workflows/build.yml` tests `Udc.Client`, then publishes `Udc.App` self-contained for
`win-x64` and stages the bundled backend (cloning and building
`kumarrahul2908/urban-dry-clean-crm-node`, using the `BACKEND_REPO_TOKEN` secret above) next to
it, on a `windows-latest` runner. Trigger it from the Actions tab (needs this workflow file to
exist on `main` first - GitHub only lists workflows that are present on the default branch) or
push to `main`. Download the `urban-dry-clean-crm-native-win-x64` artifact, unzip it, and run
`Udc.App.exe` - no installer yet (that's Phase 5); for now this published folder *is* the app.

### Locally, on Windows
`urban-dry-clean-crm-node` is private, so the clone needs a token too - use a
[personal access token](https://github.com/settings/tokens) with read access to it (same kind as
`BACKEND_REPO_TOKEN` in CI), or use `BACKEND_LOCAL_PATH` instead (see below) if you already have
it checked out locally.
```powershell
dotnet test src/Udc.Client.Tests/Udc.Client.Tests.csproj
dotnet publish src/Udc.App/Udc.App.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
$env:BACKEND_REPO_TOKEN = "<your PAT>"
node scripts/prepare-backend.cjs publish/win-x64
# run publish/win-x64/Udc.App.exe
```
To build against a local backend checkout instead of cloning (e.g. both repos checked out
side by side), set `BACKEND_LOCAL_PATH` instead of `BACKEND_REPO_TOKEN`:
```powershell
$env:BACKEND_LOCAL_PATH = "..\urban-dry-clean-crm-node"
node scripts/prepare-backend.cjs publish/win-x64
```

### Locally, on Linux/macOS
Only `Udc.Client` (+ tests) can be built/run here - `Udc.App` needs Windows (see Verification):
```bash
dotnet test src/Udc.Client.Tests/Udc.Client.Tests.csproj
```

## Known gaps

- **No background jobs yet.** The Electron prototype ran the WhatsApp outbound worker, photo
  cleanup, nightly backup and WhatsApp marketing jobs on timers while the app was open (there's
  no system cron on a desktop PC). `Udc.App` doesn't do this yet - WhatsApp messages will queue
  but not actually send until this is ported. Needs doing before this app is deployable for real
  shop use, even though it's not core "native UI" work.
- **Session isn't restored on restart.** `TokenStore` saves the bearer token on login, but
  `App.xaml.cs` doesn't yet try it before showing the Login window - every launch requires
  signing in again.
- **No order creation (POS) yet** - see Phase 3. Orders can only be viewed, not created, from
  this app so far.
- **Record Payment supports one payment line only** - the backend accepts a split across several
  payment methods in one go (`payment_lines[]`); the UI only exposes a single method for now.
- **No customer edit** - only create and view; `customers_update` isn't wired up yet.
- **No installer, no code signing, no auto-update** - see Phase 6.
- **Database backups** (once ported) will have the same `mysqldump`-on-PATH requirement the
  backend's `DatabaseBackupService` already has - unrelated to this app, documented here so it
  isn't a surprise later.
