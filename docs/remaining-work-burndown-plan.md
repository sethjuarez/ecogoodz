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
| 1 | Operational data-entry accelerators | Location copy (**done**), contact copy (**done**), task multi-assignee create (**done**), task creator-owned multi-assignee edit expansion (**done**), task headline CRUD/list/delete guard (**done**), per-headline task board and unread handling (**done**) | Staff can perform the high-frequency data-entry shortcuts that existed in legacy. |
| 2 | Trading setup accelerators | Supplier-product-to-buyer wizard (**done**), recent-load widgets (**done**), supplier tracking product/supplier helpers (**done**), buyer tracking buyer/product/supplier helpers (**done**) | Relationship/product setup can be done with the same few-click paths as legacy where still useful. |
| 3 | Rate/history power tools | Rate history tables (**done**), supplier-rate propagation to tied buyer/supplier products (**done**) | Rate changes are auditable and bulk updates avoid manual re-entry. |
| 4 | Catalog/admin completeness | Product markup color/margin matrix with conflict validation (**done**) | Product margin color rules can be maintained. |
| 5 | Reports and dashboards | Communication report first, then staff-selected gross-profit/summary/dashboard reports | Each report has named users, filters, columns, and Playwright validation before implementation. |
| 6 | Low-priority helpers | Location favorite toggle (**done**), load export (**done**), favorite list filters (**done**), quick status/substatus helpers (**done**), supplier account-manager load lookup (**done**) | Implement only if users confirm the workflow still matters. |

Retired modules (`MessagesController`, `CustomFieldController`) stay retired
unless new requirements appear. Deferred modules (`CompnyNewsController`,
`GoalController`, `ThresholdController`) stay behind explicit staff/report needs.
