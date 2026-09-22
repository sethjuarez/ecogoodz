# Report prioritization

Issue #19 exists to avoid blindly porting the legacy reporting surface. The
legacy app had seven report controllers and dozens of bespoke tables/charts; only
reports tied to active operations should be rebuilt.

## Short list

| Priority | Legacy report | Primary users | Filters | Expected output | Decision |
|---:|---|---|---|---|---|
| 1 | Last load shipped (`ReportController.LastLoadShippedReport`) | Account managers and operations staff following up with buyers/suppliers that have gone stale | Buyer vs. supplier, date range, account manager, stale-days threshold | One row per buyer or supplier showing the most recent shipped/completed load, days since shipment, products, location, account manager, and last communication date | **Rebuilt first** in `ReportController.LastLoadShipped` |
| 2 | Communication report (`ReportController.CommunicationReport`) | Account managers reviewing outreach volume/activity | Date range | Daily buyer/supplier communication-type counts plus active/proposed product status activity by report-enabled user | **Rebuilt** in `ReportController.Communication` |
| 3 | Gross profit / summary reports (`SummaryReportController`, `MonthlySummaryReportController`, `YtdSummaryReportController`, `CompanyReportController`) | Leadership/finance | Date range, buyer, supplier, buyer AM, supplier AM, comparison period | Aggregated gross-profit, markup share, and summary tables/charts | **Defer** pending staff selection of the one report shape they actually use |
| 4 | Email/company/account-manager scheduled reports (`EmailReportController`, `AccountManagerReportController`) | Leadership and account managers | User/date/month/year depending on report | Scheduled or dashboard-specific charts | **Defer** until a current distribution requirement exists |

## First rebuilt report

`/Report/LastLoadShipped` restores the most operationally useful legacy report:
it answers who has not shipped recently. The rebuilt version follows current MVC
patterns instead of the legacy DataTables JSON endpoint:

- Authenticated Razor page under the Reports menu.
- Buyer/supplier toggle.
- Date range filter, defaulting to the legacy 13-month window.
- Optional account-manager filter.
- Optional "not shipped in days" filter, which matches the legacy behavior by
  filtering after each client is reduced to its most recent shipped/completed
  load.
- Output columns: client, account manager, location, last shipped date, days
  since shipment, products, and last communication date.

The legacy report included load statuses `2` and `4`. The restored local SQL
Server data used for this pass labels those as `Shipped` and `Chargeback`, so the
rebuilt report preserves that legacy inclusion rather than guessing a different
"completed" status.

## Second rebuilt report

`/Report/Communication` restores the legacy communication activity report as a
server-rendered Razor page:

- Default date range is the legacy-style last 30 days.
- Columns are active users flagged for communication reporting who have lifetime
  communication or buyer/supplier history activity, matching the legacy
  `GetUsersWithCommunication` selection.
- Rows are daily activity buckets.
- Communication counts use the same displayed communication date convention as
  the app (`Date ?? CreateOn`).
- Each user/day cell shows buyer and supplier counts by communication type,
  an `Other Type` bucket for untyped communications, and legacy Active
  Products / Proposed Products status-change counts.

Like the existing Reports menu, the page is available to authenticated staff.
If these activity metrics should become management-only, restrict the action and
menu entry to an admin/manager role before rollout.

The remaining reports are intentionally deferred until staff chooses the next
high-value report. New report work should get a scoped issue naming the exact
legacy report, users, filters, and output columns.
