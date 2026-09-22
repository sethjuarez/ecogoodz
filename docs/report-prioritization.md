# Report prioritization

Issue #19 exists to avoid blindly porting the legacy reporting surface. The
legacy app had seven report controllers and dozens of bespoke tables/charts; only
reports tied to active operations should be rebuilt.

## Short list

| Priority | Legacy report | Primary users | Filters | Expected output | Decision |
|---:|---|---|---|---|---|
| 1 | Last load shipped (`ReportController.LastLoadShippedReport`) | Account managers and operations staff following up with buyers/suppliers that have gone stale | Buyer vs. supplier, date range, account manager, stale-days threshold | One row per buyer or supplier showing the most recent shipped/completed load, days since shipment, products, location, account manager, and last communication date | **Rebuilt first** in `ReportController.LastLoadShipped` |
| 2 | Communication report (`ReportController.CommunicationReport`) | Account managers reviewing outreach volume/activity | Date range; likely account manager/client filters | Activity list or summary of buyer/supplier communications | **Defer** until staff confirms this is still reviewed regularly |
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

The remaining reports are intentionally deferred until staff chooses the next
high-value report. New report work should get a scoped issue naming the exact
legacy report, users, filters, and output columns.
