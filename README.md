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
| 4 Workshop (piece items) + Barcode | `WorkshopView`: ready-to-send queue (multi-select + send to a workshop via a challan), in-progress queue, and the accept/return-to-store/store-receive queues, each with their own action. Double-clicking an in-progress item opens `WorkshopItemActionsWindow`, exposing all 11 lifecycle actions (start processing through QC/ironing/packaging, accept/return/receive, mark/resolve unprocessed) - the server is the single source of truth for which one is actually valid from the item's current state, same discipline as every other screen. `BarcodeLookupView`: scan or type a barcode, see the garment/order/customer/status. Laundry (per-weight) queues, workshop schedule/report, and workshop billing are explicitly out of scope - see Known gaps. | **done - confirmed green on CI** (run [#11](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41283747344), first try) |
| 5 Reports | `ReportsView` (tab strip over three sub-screens): **Summary** (`reports_summary` for a date range - sales/payments/customers/workshop operations, with by-service/top-garments/top-customers breakdowns), **Outstanding** (`outstanding_list` - ageing buckets, searchable, per-customer totals), **Daily Collection** (`daily_collection` - by payment method, with the day's payment list). "Admin" (users/roles/settings) turned out not to be achievable from this app at all - see Known gaps. | **done - confirmed green on CI** (run [#13](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41284803320), first try) |
| 6 Packaging polish | **Done, confirmed on CI** (session persistence + background jobs: run [#15](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41288841771); installer: run [#17](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41289531613)): session persistence (`TokenStore`'s saved token is now validated via `GET /api/mobile/me` on startup and silently restores the session - no more signing in on every launch), background jobs (`BackgroundJobRunner`: WhatsApp worker every minute, photo cleanup/nightly backup/WhatsApp marketing daily, standing in for the server deploy's crontab), and a real installer (`installer/setup.iss`, Inno Setup - produces `UrbanDryCleanCRM-Setup.exe` with Start Menu/optional desktop shortcuts, per-user install by default so a shop-counter standard account doesn't need an admin prompt). **Not achievable by this app alone:** code signing (needs a purchased certificate only the business owner can buy) and true auto-update (needs a decision on where releases are hosted) - see Known gaps. | **done** |
| 7 Laundry (per-weight) mode | `NewLaundryOrderWindow`, opened from Orders ("+ New Laundry Order"): customer search/select (same as piece orders), a batch builder (one weight-priced service from `products`' new `universal_services` + a weight + the individual garments tagged into it), a batches cart, the same order details/estimate pattern as piece orders, submits to `laundry_orders_create`. `WorkshopView` gained Laundry "ready to send" (multi-select garments -> `create_laundry_challan`) and "in progress" queues from `workshop_queue`'s `laundry_pending`/`laundry_in_progress`; double-clicking an in-progress Laundry garment opens the same `WorkshopItemActionsWindow` piece items use (workshop_item_id-based actions don't distinguish piece from laundry server-side), so no new lifecycle UI was needed there. The Accept/Return/Store-Receive queues already mixed piece and laundry items server-side (a `kind` field), so they needed no client changes beyond a Type column. A floating-store account (no fixed store) cannot create a Laundry order at all - `laundry_orders_create` never reads a `store_id` from its payload, unlike `pos_orders_create` - see Known gaps. | **done - confirmed green on CI** (run [#19](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41299026929), first try) |

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
├── scripts/
│   └── prepare-backend.cjs     clones + builds urban-dry-clean-crm-node, bundles a portable Node
└── installer/
    └── setup.iss                Inno Setup script - packages publish/win-x64/ into Setup.exe
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
saved separately, encrypted at rest with Windows DPAPI (`TokenStore`, current-user scope), and
validated against `GET /api/mobile/me` on every startup so a returning user skips straight past
the login screen into a restored session.

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

**Phase 4 is confirmed**: [CI run #11](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41283747344)
went green on the first try - 33/33 tests, and `WorkshopView`/`WorkshopItemActionsWindow`/
`BarcodeLookupView` all compiled cleanly. The added repo-wide x:Name cross-check and the
by-hand model-property check both paid off again.

**Phase 5 is confirmed**: [CI run #13](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41284803320)
went green on the first try - 36/36 tests, and all four new Reports screens compiled cleanly.
Five phases in a row now building successfully on real Windows.

**Phase 6 part 1 (session persistence + background jobs) is confirmed**: [CI run #15](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41288841771)
went green - 42/42 tests, `App.xaml.cs`'s session-restore wiring and `BackgroundJobRunner`
compiled cleanly. One real compile error (`Timer` used without `using System.Threading;`) was
caught locally in this sandbox before ever reaching CI this time - `dotnet build` on
`Udc.Client` catches that class of bug directly, since it's plain C#, not a WPF-only issue.

**Phase 6 part 2 (installer) is confirmed**: [CI run #17](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41289531613)
went green on the first try - the runner installed Inno Setup via chocolatey, compiled
`installer/setup.iss` against the published `publish/win-x64/` folder, and uploaded the
resulting `UrbanDryCleanCRM-Setup.exe` (~74 MB, compressed) as its own CI artifact alongside the
raw publish folder. Not locally verifiable beforehand: an attempt to run the Inno Setup compiler
under Wine in this Linux sandbox failed at the very first step (the installer download came back
as an HTML page, not the real `.exe`), so this one relied entirely on the real `windows-latest`
CI run, same as every WPF screen in every earlier phase.

**Phase 7 (Laundry/per-weight mode) is confirmed**: [CI run #19](https://github.com/kumarrahul2908/CRM_urban_windows/actions/runs/41299026929)
went green on the first try - 47/47 tests, and `NewLaundryOrderWindow` plus the `WorkshopView`
additions (the biggest new-screen phase since Phase 3's `NewOrderWindow`) compiled cleanly. The
same review discipline that's caught every real bug since Phase 2 (well-formed-XML check,
repo-wide `x:Name`-vs-code-behind cross-check in both directions, by-hand model-property
cross-check) paid off again - seven phases building successfully now, six of them green on the
very first CI attempt.

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
push to `main`. Two artifacts come out of each run: `UrbanDryCleanCRM-Setup` (download, unzip,
run `UrbanDryCleanCRM-Setup.exe` - a normal Windows installer with Start Menu/optional desktop
shortcuts) and `urban-dry-clean-crm-native-win-x64` (the raw published folder, for anyone who'd
rather copy it over manually without installing).

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

- **No payment collected at order-creation time** - neither `NewOrderWindow` nor
  `NewLaundryOrderWindow` expose the `payments`/`wallet_amount` fields `pos_orders_create` and
  `laundry_orders_create` accept; record payment separately right after via the existing Record
  Payment screen.
- **No rate overrides / manual pricing / brand-color-remarks per item** in the New Order cart -
  every line uses the catalog's own piece rate for the customer's price list as-is.
- **The New Order / New Laundry Order total shown while building is an estimate** (subtotal, the
  typed discount, and delivery charge only) - tax and the urgent-level surcharge are computed
  server-side and only appear in the real total once the order is actually created.
- **A floating-store account (no fixed store) cannot create a Laundry order at all, and this isn't
  something the client can work around.** Checked `laundry_orders_create` in orders.ts: unlike
  `pos_orders_create`, it calls `resolveAuthorizedStoreId(db, ctx)` with no second argument, so it
  never reads a `store_id` from the request body - it always resolves the signed-in user's own
  fixed store and fails outright (`needs_selection` with nothing the client can supply to satisfy
  it) for anyone without one. `NewLaundryOrderWindow` detects this up front (via
  `pos_available_stores`' `is_fixed`) and blocks the whole form with an explanation rather than
  letting the user fill it in and hit a guaranteed error. Fixing this for real needs a backend
  change (accepting `store_id` the way the piece-order endpoint does) - out of scope here.
- **`laundry_orders_create` trusts the client-supplied batch `rate` rather than resolving it
  server-side.** Unlike `pos_orders_create`'s `priceLine()`, `OrderService.createLaundryOrder()`
  takes `batch.rate` directly from the request body (only checking it's `> 0`) - it never looks up
  the price list itself. `NewLaundryOrderWindow` always fills this from the already-price-list-
  resolved `universal_services` rate and never lets it be typed in, but this is a client-side
  safeguard only, not a server-enforced one - worth knowing if this API is ever called from
  anywhere else.
- **Record Payment supports one payment line only** - the backend accepts a split across several
  payment methods in one go (`payment_lines[]`); the UI only exposes a single method for now.
- **No customer edit** - only create and view; `customers_update` isn't wired up yet.
- **No workshop schedule, report, or billing screens** - `workshop_schedule`/`workshop_report`/
  `workshop_billing_*` aren't wired up; this phase covers day-to-day queue operations only.
- **`WorkshopItemActionsWindow` shows all 11 lifecycle actions always**, rather than only the
  ones valid for the item's current state - the backend is the authority on validity and returns
  a clear error for an action attempted out of order, so nothing incorrect can actually happen,
  but the UI doesn't yet hide inapplicable buttons the way the real app's own state-aware forms do.
- **No tag/barcode printing** - `BarcodeLookupView` looks a barcode up; it doesn't generate or
  print new tags.
- **Admin (users, roles, settings, print settings) is not achievable from this app at all, not
  just deferred.** Checked every file under `src/routes/mobile/` in the backend repo: there are
  no `/api/mobile/*` endpoints for user management, role management, or business settings -
  those are web-only features (the PHP-era web UI, then its Node port), never exposed to the
  mobile API this app and the Android app both consume. Building an Admin screen here would mean
  adding new backend endpoints first, which is a change to `urban-dry-clean-crm-node` and out of
  scope for this client-only repo unless specifically asked for.
- **WhatsApp admin (templates, analytics, marketing) has the same gap** - no mobile API surface
  for it either, same reasoning as Admin above.
- **Audit log, customer order history, and the dormant-customers report aren't wired up** -
  `audit_logs`, `customer_history`, and `dormant_customers` all exist and work, they're just not
  built into a screen yet; good first candidates for a later phase.
- **No code signing, and this isn't something a later phase can just add.** It requires a
  code-signing certificate purchased from a CA (or a Microsoft Trusted Signing subscription),
  which only the business owner can buy - no amount of further coding here produces one. Without
  it, Windows SmartScreen will keep warning on first run ("Windows protected your PC" > "More
  info" > "Run anyway").
- **No true auto-update, and this needs a decision, not just code.** Auto-update needs somewhere
  stable to host release feeds/binaries (a GitHub Releases feed on this repo is the obvious
  choice, but that's a choice for the project owner to confirm, not something to assume).
  `nightlyBackup.js`'s `mysqldump`-on-PATH requirement (see `DatabaseBackupService` in the
  backend repo) is no longer hypothetical now that `BackgroundJobRunner` actually runs it nightly
  - if this PC doesn't have the MySQL/MariaDB command-line client installed, check the log
  (`File > Open logs folder`) rather than assuming backups are happening silently.
