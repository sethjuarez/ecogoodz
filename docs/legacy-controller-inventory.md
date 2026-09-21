# Legacy controller inventory &amp; rebuild plan

The legacy ASP.NET MVC5 app (`D:\projects\ecogoodz\backup\decompiled_project`) has 30
controllers. This tracks what's rebuilt, what's next, and what's likely dead weight -
to help decide what's worth the effort versus what the business no longer needs.

**Already rebuilt** (4 of 30): Account (auth), Buyer, Supplier, Product, Load, Home
(dashboard).

## Tier 1 - core trading workflow (highest value, do next)

The actual business the app exists to run: matching buyers to suppliers for a given
product/location, at a negotiated rate, then tracking loads shipped against that
match.

| Controller | Legacy size | What it does |
|---|---|---|
| `BuyerSupplierController` | 1447 lines / 27 actions | The buyer&lt;-&gt;supplier matching engine - assigns which suppliers serve which buyer (and vice versa) per location, sets/edits negotiated rates with markup, shows recent loads per match. This is the operational core; Load records reference these matches. |
| `BuyerProductController` / `BuyerProductRateController` | 306 + 228 lines | Which products a buyer wants, and at what rate over time (rate history, not just current price). |
| `SupplierProductController` / `SupplierProductRateController` | 630 + 306 lines | Same shape, supplier side - what a supplier offers and at what rate over time. |
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
| `GoalController` | 394 lines | Account-manager sales goals/targets. |
| `ThresholdController` | 104 lines | Small config table (likely alert thresholds referenced by reports). |
| `CustomFieldController` | 155 lines | Legacy's answer to "extra fields per entity" - worth checking whether the normalized schema still needs this or if it was cruft from schema drift. |
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

## Tier 4 - likely dead weight / needs a decision

| Controller | Legacy size | Note |
|---|---|---|
| `CompnyNewsController` (sic) | 295 lines | Internal company news/announcements feed - ask whether this was ever really used. |
| `MessagesController` | 11 lines | Nearly empty - probably a stub or redirect, minimal risk either way. |

## Already covered outside the controller layer

- `AccountController` (auth) is rebuilt with modern ASP.NET Core Identity - not a
  1:1 port, since the legacy plaintext-password system was replaced entirely (see
  `docs/security-review.md`).
- `HomeController` (dashboard) rebuilt with live stats instead of the legacy
  static landing page.

## Suggested order

1. **Location + PackageType CRUD** (small, unblocks the rest of Tier 1's forms).
2. **BuyerSupplier matching** (the biggest single win - the actual trading workflow).
3. **BuyerProduct/BuyerProductRate + SupplierProduct/SupplierProductRate** (rate
   history alongside the matching).
4. Revisit Tier 2 based on what the user says staff actually use day-to-day.
5. Tier 3 reports - only after confirming with the user which ones matter; do not
   port all of them by default.
6. Tier 4 - confirm with the user whether either is still wanted before spending any
   time on them.
