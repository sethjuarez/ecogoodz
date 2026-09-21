# Auth security review

An adversarial (rubber-duck) review of the auth/password-reset flow was run against
`AccountController`, `LegacyUserMigrator`, `ForcePasswordChangeFilter`, and the Identity
config in `Program.cs`. This tracks each finding and its current status.

| # | Severity | Finding | Status |
|---|----------|---------|--------|
| 1 | Critical | `TempData["DevResetUrl"]` (dev-only raw reset link) could leak on a misconfigured production deployment | **Fixed** - gated on both `IWebHostEnvironment.IsDevelopment()` *and* an explicit `DevTools:ShowResetLinkInResponse` config flag (belt-and-suspenders; either alone defaults to off) |
| 2 | High | GoDaddy VPS TLS cert hostname mismatch makes transport confidentiality untrustworthy | **Deferred to deployment** - not resolvable in local dev; tracked as a release blocker for the real cutover |
| 3 | High | Reset tokens travel in the URL query string - risk via Referer leakage, browser history, proxy/cache logs | **Fixed** - `Referrer-Policy: no-referrer` and `Cache-Control: no-store` set on both `ResetPassword` actions |
| 4 | High (conditional) | Plesk may terminate TLS and forward over HTTP without forwarded-header config, producing insecure reset links/cookies | **Fixed** - `UseForwardedHeaders()` configured for `XForwardedFor`/`XForwardedProto`; `CookieSecurePolicy.Always` set explicitly |
| 5 | Medium | Timing/response differences could allow enumerating valid emails via ForgotPassword | **Mitigated** - identical confirmation redirect regardless of whether the account exists; residual timing side-channel accepted as low-risk for an ~18-user internal staff app |
| 6 | Medium | No rate limiting on Login/ForgotPassword/ResetPassword allowed flooding/enumeration | **Fixed** - per-IP fixed-window rate limiter (`Microsoft.AspNetCore.RateLimiting`, 10 req/min) applied to the whole `AccountController` |
| 7 | Medium | Password policy (`RequiredLength=8`, no complexity) weak for an externally-reachable system | **Fixed** - `RequiredLength` raised to 12 |
| 8 | Medium (conditional) | Identity didn't enforce unique emails; a duplicate/shared legacy email could resolve `FindByEmailAsync` to the wrong account | **Fixed** - `RequireUniqueEmail = true` enabled after confirming all 18 legacy users have unique, non-empty emails (verified via direct query against the `User` table) |
| 9 | Low | `ResetPassword` GET could throw on a malformed `t` parameter instead of failing gracefully | **Fixed** - `FormatException` from `Base64UrlDecode` caught and redirected to Login like an expired link |
| - | Defensive | `ForcePasswordChangeFilter` exempted the *entire* Account controller, so any future `[Authorize]` action added there would silently bypass the forced-reset gate | **Fixed** - inverted to an explicit action allow-list (`Account/ForcePasswordChange`, `Account/Logout`) |

## Still open / deployment-time work

- **#2 (TLS cert)**: must be resolved (real cert, correct hostname) before the reset
  flow can be considered safe on the public GoDaddy VPS. Not something local dev can
  fix.
- Re-run the duplicate-email check (finding #8) against production data before the
  real user-migration cutover, since the local dev DB may not reflect every legacy
  record.
