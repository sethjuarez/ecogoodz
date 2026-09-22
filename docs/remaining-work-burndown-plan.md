# Remaining work burndown plan

This plan burns down the controller-parity backlog without blindly porting every
legacy action. Each feature slice follows the same rubric:

1. Implement the smallest complete workflow.
2. Add/extend controller or model tests for the legacy behavior.
3. Run `dotnet test .\src\EcoGoodz.slnx --no-restore`.
4. Rubber-duck review the diff for functional regressions.
5. Exercise the workflow in the running app with Playwright.
6. Commit the slice before moving on.

## Order

| Phase | Scope | Feature slices | Done when |
|---:|---|---|---|
| 1 | Operational data-entry accelerators | Location copy (**done**), contact copy (**done**), task multi-assignment/headline board parity | Staff can perform the high-frequency data-entry shortcuts that existed in legacy. |
| 2 | Trading setup accelerators | Supplier-product-to-buyer wizard, buyer/supplier tracking helpers, recent-load widgets | Relationship/product setup can be done with the same few-click paths as legacy where still useful. |
| 3 | Rate/history power tools | Rate history tables, supplier-rate propagation to tied buyer/supplier products | Rate changes are auditable and bulk updates avoid manual re-entry. |
| 4 | Catalog/admin completeness | Product markup color/margin matrix with conflict validation | Product margin color rules can be maintained when staff confirms they still matter. |
| 5 | Reports and dashboards | Communication report first, then staff-selected gross-profit/summary/dashboard reports | Each report has named users, filters, columns, and Playwright validation before implementation. |
| 6 | Low-priority helpers | Load export, favorites, quick status/substatus helpers | Implement only if users confirm the workflow still matters. |

Retired modules (`MessagesController`, `CustomFieldController`) stay retired
unless new requirements appear. Deferred modules (`CompnyNewsController`,
`GoalController`, `ThresholdController`) stay behind explicit staff/report needs.
