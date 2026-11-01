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
| 2 Customers + Orders (view) | **Customers:** list (search + pagination), detail (contact info, price list, wallet balance, outstanding), create. **Orders:** list (search + pagination), detail (items, payments, status, balance due), Record Payment (single payment line). Sidebar navigation wired up (Dashboard / Customers / Orders). | **done - confirmed green on CI** (run [#8](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41281236864), after one real compile-error fix CI caught - a model property the UI referenced but the DTO never defined) |
| 3 POS / order creation (piece mode) | `NewOrderWindow`, opened from Orders ("+ New Order"): customer search/select (pulls their price list + wallet/outstanding via `pos_customer_summary`), store picker when the user has no fixed store, garment/service catalog from `products` (price-list-aware rates in one call), a cart, due date/urgent level/discount/home delivery/notes, a clearly-labeled client-side estimate (excludes tax/urgent charge - those are server-computed), submits to `pos_orders_create`. Laundry/per-weight order creation, collecting payment at order-creation time, and rate overrides are explicitly out of scope here - see Known gaps. | **done - confirmed green on CI** (run [#9](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41282863270), first try - no compile errors this time) |
| 4 Workshop (piece items) + Barcode | `WorkshopView`: ready-to-send queue (multi-select + send to a workshop via a challan), in-progress queue, and the accept/return-to-store/store-receive queues, each with their own action. Double-clicking an in-progress item opens `WorkshopItemActionsWindow`, exposing all 11 lifecycle actions (start processing through QC/ironing/packaging, accept/return/receive, mark/resolve unprocessed) - the server is the single source of truth for which one is actually valid from the item's current state, same discipline as every other screen. `BarcodeLookupView`: scan or type a barcode, see the garment/order/customer/status. Laundry (per-weight) queues, workshop schedule/report, and workshop billing are explicitly out of scope - see Known gaps. | **done (code) - not yet confirmed on CI**, see Verification below |
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

**Phase 2 is confirmed**: [CI run #8](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41281236864)
is green, after one real fix along the way - `OrderDetailWindow.xaml.cs` read `OrderHeader.Color`
to tint the status badge, but `OrderModels.cs` never defined that property, and only a real
compiler catches a missing-model-property bug like that (the Linux sandbox's `dotnet build` can
build `Udc.Client` alone, so it has nothing that references `Udc.App`'s own code to check
against).

**Phase 3 is confirmed**: [CI run #9](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41282863270)
went green on the first try - 27/27 tests, and `NewOrderWindow` (the largest screen so far)
compiled cleanly. The hand cross-check against model definitions and the well-formed-XML
validation adopted after Phase 2's miss paid off.

**Phase 4 (Workshop + Barcode) is not yet confirmed** - built in the same Linux sandbox:

- `dotnet build` / `dotnet test` on `Udc.Client` + `Udc.Client.Tests`: **passing** (33 tests -
  the prior 27 plus 6 new ones covering `GetWorkshopQueueAsync`/`SendWorkshopItemActionAsync`/
  `CreateChallanAsync`/`LookupBarcodeAsync` against a fake `HttpMessageHandler`, including an
  invalid-transition error path and query-string building).
- `WorkshopView`, `WorkshopItemActionsWindow`, `BarcodeLookupView` (plus the `MainWindow`
  sidebar wiring): **written and reviewed by hand, not yet compiled anywhere.** This phase added
  two extra review passes beyond Phase 2/3's: a grep-based x:Name-vs-code-behind cross-check
  across every `.xaml`/`.xaml.cs` pair in the app (not just the new files), and the usual
  by-hand model-property cross-check, specifically because this phase's `WorkshopItemActionsWindow`
  exposes all 11 backend lifecycle actions and a typo in any one of them would only fail at
  runtime when that specific button is clicked, not at CI build time the way Phase 2's miss did.

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
- **Order creation covers piece mode only** - laundry/per-weight orders (`laundry_orders_create`)
  aren't wired up; use the web app for those until a later phase adds it.
- **No payment collected at order-creation time** - `NewOrderWindow` doesn't expose the
  `payments`/`wallet_amount` fields `pos_orders_create` accepts; record payment separately right
  after via the existing Record Payment screen.
- **No rate overrides / manual pricing / brand-color-remarks per item** in the New Order cart -
  every line uses the catalog's own piece rate for the customer's price list as-is.
- **The New Order total shown while building the cart is an estimate** (subtotal, the typed
  discount, and delivery charge only) - tax and the urgent-level surcharge are computed
  server-side and only appear in the real total once the order is actually created.
- **Record Payment supports one payment line only** - the backend accepts a split across several
  payment methods in one go (`payment_lines[]`); the UI only exposes a single method for now.
- **No customer edit** - only create and view; `customers_update` isn't wired up yet.
- **Workshop covers piece items only** - the laundry (per-weight) pending/in-progress queues
  `workshop_queue` also returns aren't shown; use the web app for laundry-mode workshop tracking.
- **No workshop schedule, report, or billing screens** - `workshop_schedule`/`workshop_report`/
  `workshop_billing_*` aren't wired up; this phase covers day-to-day queue operations only.
- **`WorkshopItemActionsWindow` shows all 11 lifecycle actions always**, rather than only the
  ones valid for the item's current state - the backend is the authority on validity and returns
  a clear error for an action attempted out of order, so nothing incorrect can actually happen,
  but the UI doesn't yet hide inapplicable buttons the way the real app's own state-aware forms do.
- **No tag/barcode printing** - `BarcodeLookupView` looks a barcode up; it doesn't generate or
  print new tags.
- **No installer, no code signing, no auto-update** - see Phase 6.
- **Database backups** (once ported) will have the same `mysqldump`-on-PATH requirement the
  backend's `DatabaseBackupService` already has - unrelated to this app, documented here so it
  isn't a surprise later.
