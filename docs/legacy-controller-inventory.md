# Legacy controller inventory &amp; rebuild plan

The legacy ASP.NET MVC5 app (`D:\projects\ecogoodz\backup\decompiled_project`) has 30
controllers. This tracks what's rebuilt, what's next, and what's likely dead weight -
to help decide what's worth the effort versus what the business no longer needs.

**Already rebuilt**: Account (auth), Buyer, Supplier, Product, Load, Home
(dashboard), Location, PackageType, BuyerSupplier, BuyerProduct, SupplierProduct.

## Tier 1 - core trading workflow (highest value, do next)

The actual business the app exists to run: matching buyers to suppliers for a given
product/location, at a negotiated rate, then tracking loads shipped against that
match.

| Controller | Legacy size | What it does |
|---|---|---|
| `BuyerSupplierController` | 1447 lines / 27 actions | The buyer&lt;-&gt;supplier matching engine - assigns which suppliers serve which buyer (and vice versa) per location, sets/edits negotiated rates with markup, shows recent loads per match. This is the operational core; Load records reference these matches. |
| `BuyerProductController` / `BuyerProductRateController` | 306 + 228 lines | First-pass buyer wanted-product CRUD is rebuilt. Buyer-side rates can now be added, edited, deactivated, and reviewed on products assigned to a buyer/supplier match. |
| `SupplierProductController` / `SupplierProductRateController` | 630 + 306 lines | First-pass supplier offered-product CRUD and supplier product rate add/edit/deactivate history are rebuilt. Legacy wizard flows and buyer-assignment automation are intentionally deferred. |
| `LocationController` | 202 lines | Already has a DbSet and is referenced heavily by Buyer/Supplier/Load - needs its own CRUD UI (currently only used as a dropdown source). |
| `PackageTypeController` | 129 lines | Small reference/lookup table (used on Load/Product forms). |

**Why this is next**: Load management (already built) only lets you record a
shipment - it can't help staff figure out *which* buyer/supplier/rate to use, which
is what BuyerSupplier + the rate controllers exist for. Without this tier, the app
tracks history but doesn't support the actual day-to-day matching decision the
legacy app was built for.

## Tier 2 - CRM / staff workflow (medium value)

Used daily by account managers, but doesn't block Tier 1 loads from being recorded.

| Controller | Legacy size | What it does |
|---|---|---|
| `ContactController` | 374 lines | Contacts per buyer/supplier (people, phone/email) - likely a child entity of Buyer/Supplier. |
| `CommunicationController` | 191 lines | Logs calls/emails/notes against a buyer or supplier - referenced heavily by the Report module (`CommunicationReport`). |
| `NotesController` | 159 lines | Free-form notes, probably attached to Buyer/Supplier/Load. |
| `TaskController` / `TaskHeadlineController` | 467 + 146 lines | Staff task/reminder tracking. |
| `UserController` | 495 lines | Staff user management (distinct from ASP.NET Identity - see below). |

## Tier 3 - reporting (large effort, defer)

The single biggest remaining chunk of code, and almost entirely read-only
dashboards/exports over data Tier 1/2 already produce. High line count, low logic
reuse (each is a bespoke chart/table), and nothing else depends on these existing
first.

| Controller | Legacy size |
|---|---|
| `ReportController` | 1683 lines / 36 actions - buyer/supplier/manager reports, gross-profit projections, last-load-shipped, communication reports, and more |
| `SummaryReportController` | 960 lines |
| `YtdSummaryReportController` | 796 lines |
| `MonthlySummaryReportController` | 599 lines |
| `AccountManagerReportController` | 504 lines |
| `EmailReportController` | 436 lines |
| `CompanyReportController` | 478 lines |

**Recommendation**: don't rebuild these 1:1. Once Tier 1 data exists, revisit with
the user about which 2-3 reports are actually used regularly - the legacy app likely
accumulated many that were built once and never opened again.

## Tier 4 - low-value legacy modules: decisions

Decision pass completed after the core trading/CRM workflows were restored. None
of these modules blocks buyer/supplier/product/load operations. Data counts below
come from the restored local Docker SQL Server database used for the rebuild pass:
`SELECT COUNT(*)` over `CompanyNews`, `RemovedCompanyNews`, `CustomField`,
`UserCustomField`, `Threshold`, `AccountManagerGoal`, `CompanyGoals`, and
`GoalParameters`.

| Controller | Legacy size | Decision | Rationale / follow-up |
|---|---:|---|---|
| `CompnyNewsController` (sic) | 295 lines | **Defer** | Internal company-news feed has legacy data (`CompanyNews`/`RemovedCompanyNews`) but no dependency from the trading workflow. Do not rebuild until staff explicitly asks for an in-app announcements feed; if needed, create a small scoped issue for read/list/create announcements only. |
| `MessagesController` | 11 lines | **Retire** | Legacy controller exposes only a tiny JSON endpoint and no meaningful workflow. Do not rebuild unless a concrete staff messaging requirement appears. |
| `CustomFieldController` | 155 lines | **Retire** | Legacy schema has 2 field definitions but 0 `UserCustomField` values in the restored data, and the current app uses explicit typed forms instead of dynamic user fields. Do not rebuild unless staff identifies an active custom-field process. |
| `GoalController` | 394 lines | **Defer to reports** | Goal tables contain real data (`AccountManagerGoal`, `CompanyGoals`, `GoalParameters`) and legacy report views reference them, but the value is tied to the still-open report-prioritization work. Revisit under #19; rebuild only if one of the selected high-value reports needs goal editing or goal display. |
| `ThresholdController` | 104 lines | **Defer to reports** | Single-row threshold config appears report/alert-related, not operational CRUD. Revisit under #19 only if a prioritized report or dashboard needs threshold configuration. |

No rebuild issue was created from this pass because no item is approved for
implementation now. Company news is deferred until a staff request appears; goals
and thresholds are deferred behind report prioritization.

## Already covered outside the controller layer

- `AccountController` (auth) is rebuilt with modern ASP.NET Core Identity - not a
  1:1 port, since the legacy plaintext-password system was replaced entirely (see
  `docs/security-review.md`).
- `HomeController` (dashboard) rebuilt with live stats instead of the legacy
  static landing page.

## Suggested order

1. **Location + PackageType CRUD** (small, unblocks the rest of Tier 1's forms).
2. **BuyerSupplier matching** (the biggest single win - the actual trading workflow).
3. Decide whether the legacy supplier-product-to-buyer wizard is still needed.
4. Revisit any remaining Tier 2 gaps based on what staff actually use day-to-day.
5. Tier 3 reports - only after confirming with the user which ones matter; do not
   port all of them by default.
6. Tier 4 - follow the decisions above: messages/custom fields retired;
   company news deferred; goals/thresholds deferred behind report
   prioritization.
