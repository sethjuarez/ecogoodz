# Controller parity audit

This audit compares the legacy MVC5 controllers in
`D:\projects\ecogoodz\backup\decompiled_project\EcoGoodz\EcoGoodz\Controllers`
with the rebuilt ASP.NET Core app. It separates functionality that is rebuilt
under modern controller names from functionality that is intentionally retired or
still missing.

## Summary

The rebuilt app covers the operational CRUD surface and the highest-value
buyer/supplier/load trading workflow, but it is not a complete 1:1 port of every
legacy controller action. Many legacy actions were DataTables JSON endpoints,
partial views, or AJAX helpers that are replaced by server-rendered paged lists,
details pages, and Select2/Tom Select search endpoints. The remaining true gaps
are mostly workflow accelerators, report/dashboard surfaces, and low-value legacy
modules that were already deferred or retired.

## Parity matrix

| Legacy controller | Current surface | Status | Notes / gaps |
|---|---|---|---|
| `AccountController` | `AccountController` | **Complete, modernized** | Login, logout, forgot/reset password are rebuilt on ASP.NET Core Identity. Legacy plaintext-password behavior is intentionally not preserved. Current app adds forced password change and access denied handling. |
| `HomeController` | `HomeController`, `ReportController` | **Partial** | Current dashboard has live counts/recent loads. Legacy `GetReport` monthly invoice-lbs/goal dashboard charts are not rebuilt; they depend on goal/report data and should stay deferred with reporting unless staff confirms dashboard chart usage. |
| `BuyerController` | `BuyerController`, related child controllers | **Core complete; auxiliaries partial** | Buyer CRUD, detail page, locations/products/contacts/communications/notes links, recent-load widget, buyer tracking buyer/product/supplier-location helpers, location favorite toggle, current-user favorite-filtered list view, and status/substatus AJAX helpers are rebuilt. Legacy list/detail AJAX wrappers are replaced by MVC pages. Not rebuilt: note quick-update endpoint and legacy report fragments. |
| `SupplierController` | `SupplierController`, `SupplierProductController`, related child controllers | **Core complete; auxiliaries partial** | Supplier CRUD, detail page, products/locations/contacts/communications/notes links, recent-load widget, supplier-product-to-buyer helper, supplier tracking product/supplier-location helpers, location favorite toggle, current-user favorite-filtered list view, and status/substatus AJAX helpers are rebuilt. Not rebuilt: packaging/product lookup helpers and report fragments. |
| `ProductController` | `ProductController` | **Core complete** | Product CRUD, parent category selection, and the markup color / margin-parameter matrix (`ProductMarkUpColor`) with overlap validation are rebuilt. Legacy separate parent-product create/edit endpoints are represented by one product form. |
| `PackageTypeController` | `PackageTypeController` | **Complete** | CRUD/list behavior is rebuilt; legacy DataTables JSON endpoint is replaced by paged server-rendered list. |
| `LocationController` | `LocationController` | **Core complete; extended fields partial** | Location CRUD, copy-from-existing-location, and current-user favorite toggle are rebuilt. Copy preserves hidden legacy logistics fields and clones non-dock contacts plus buyer products/packages. Some extended logistics fields are still not directly editable on the current form. |
| `ContactController` | `ContactController` | **Complete for CRUD/copy** | Contact CRUD, primary-contact invariant, and legacy `CopyContact` behavior are rebuilt. `SetPrimaryContact` behavior is covered by create/edit primary flag. |
| `CommunicationController` | `CommunicationController` | **Complete for CRUD** | Communication list/create/edit/deactivate is rebuilt for buyer/supplier clients. Legacy DataTables JSON endpoint is replaced by current list UI. Higher-level communication reports remain deferred in reporting. |
| `NotesController` | `NoteController` | **Complete for CRUD** | Note list/create/edit/deactivate is rebuilt across supported scopes. Legacy DataTables JSON endpoint is replaced by current list UI. |
| `TaskController` | `StaffTaskController` | **Core complete; edit parity partial** | Staff task create/edit/list/toggle done/deactivate is rebuilt, including legacy-style multi-assignee task creation, per-user headline board, headline-specific task lists, and unread Assigned-task handling. Not rebuilt: legacy multi-assignee edit expansion for creator-owned tasks; current edit remains assignment-row scoped. |
| `TaskHeadlineController` | `StaffTaskController` headline actions | **Rebuilt / folded in** | Default headlines are created for staff users. Dedicated per-user headline list/create/edit/delete is rebuilt, excluding the reserved `Assigned` headline and blocking deletion while visible open tasks remain assigned to that headline. |
| `UserController` | `StaffUserController`, `AccountController` | **Complete for staff admin; modernized auth** | Staff user CRUD/deactivate and Identity user synchronization are rebuilt. Password reset/change flows are handled by modern account routes instead of legacy admin partials. |
| `LoadController` | `LoadController` | **Core complete** | Load CRUD/details, buyer/supplier locations, product lines, validation, CSV export of the filtered/sorted load list, and supplier account-manager lookup JSON are rebuilt. Legacy AJAX helper actions are otherwise replaced by populated forms/searches. |
| `BuyerProductController` | `BuyerProductController` | **Core complete** | Buyer product CRUD and packaging preservation are rebuilt. Legacy partial/AJAX create endpoints are replaced by current pages/search endpoints. |
| `BuyerProductRateController` | `BuyerSupplierController` product-rate actions | **Core complete** | Buyer-side rates on buyer/supplier product assignments can be added, edited, deactivated, audited into `BuyerProductHistory`, and reviewed on the match-product details page. |
| `SupplierProductController` | `SupplierProductController`, `BuyerSupplierController` | **Core complete; bulk gaps reduced** | Supplier product CRUD, supplier rate management, supplier rate audit history, supplier-rate propagation to tied buyer/supplier product rates, and the supplier-product-to-buyer assignment wizard are rebuilt. Not rebuilt: bulk supplier-location product update and broader assignment wizard surfaces. |
| `SupplierProductRateController` | `SupplierProductController` rate actions | **Core complete** | Supplier rates can be added, edited, deactivated, audited, and propagated to selected tied buyer/supplier product rates while preventing duplicate effective dates. |
| `BuyerSupplierController` | `BuyerSupplierController` | **Core complete; legacy wizard/report helpers missing** | Buyer/supplier match CRUD, assigned supplier products, buyer-side rates, search endpoints, and details are rebuilt. Not rebuilt: create-with-wizard flow, buyer/supplier location assignment list, accounting product DataTables endpoint as a separate list, markup color info helper, and standalone recent-load partials. |
| `ReportController` | `ReportController` | **One prioritized report rebuilt; rest deferred** | `LastLoadShippedReport` is rebuilt as `/Report/LastLoadShipped`. Buyer/supplier chart reports, gross profit reports, proposed-products report, last-login/new-account reports, communication reports, unique buyer reports, projection reports, show-communication settings, 2017/account-manager static reports, and average gross-profit report remain deferred pending staff prioritization. |
| `SummaryReportController` | none | **Deferred** | Summary graph and markup-share reports are intentionally not blindly ported. Rebuild only after staff selects the next high-value report. |
| `YtdSummaryReportController` | none | **Deferred** | YTD summary list/export remains deferred behind report prioritization. |
| `MonthlySummaryReportController` | none | **Deferred** | Monthly summary list/export and markup-share report remain deferred behind report prioritization. |
| `AccountManagerReportController` | none | **Deferred** | Goal and invoice-lbs dashboard/report widgets remain deferred with reporting/goals. |
| `EmailReportController` | none | **Deferred** | Scheduled/email report chart and upload-image workflow remain deferred until a current distribution requirement exists. |
| `CompanyReportController` | none | **Deferred** | Company goal and gross-profit reports remain deferred behind report prioritization. |
| `GoalController` | none | **Deferred to reports** | Account-manager/company goal editing and goal parameters are not rebuilt; see low-value decisions and reporting docs. Rebuild only if a selected report/dashboard requires goal maintenance. |
| `ThresholdController` | none | **Deferred to reports** | Threshold config is not rebuilt; revisit only if a prioritized report/alert needs it. |
| `CompnyNewsController` | none | **Deferred** | Internal company-news feed is not operationally connected to trading workflows. Rebuild only if staff asks for announcements. |
| `MessagesController` | none | **Retired** | Legacy controller was effectively a stub. |
| `CustomFieldController` | none | **Retired** | Restored data had no user custom-field values, and current forms use typed fields. |

## Functional gaps that would need new scoped work

These are the meaningful missing workflows if the goal changes from "restore the
core app" to "full 1:1 legacy parity":

1. **Reports beyond Last Load Shipped**: communication, gross profit/summary,
   goal, company, email, and account-manager dashboard reports.
2. **Task edit parity**: legacy creator-owned multi-assignee edit expansion.
3. **Remaining buyer/supplier helper workflows**: low-level tracking order/delete/note
   fragments and other confirmed helper needs.

## Recommendation

Do not treat the missing items above as bugs in the restored core app. Convert
only staff-confirmed gaps into scoped issues, with the legacy controller/action
names copied into the issue. Remaining task work is now limited to the lower-risk
creator-owned multi-assignee edit expansion; the headline board itself is rebuilt.
