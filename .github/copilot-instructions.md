# Copilot instructions

## EcoGoodz deploy workflow

All deployable code must land on `main` first. Feature branches and pull requests
are for verification only and should run CI, but should not publish feature-branch
binaries to the `deploy` branch.

For deployment:

1. Create or update the pull request.
2. Wait for CI to pass.
3. Merge the pull request into `main`.
4. Let `.github/workflows/deploy.yml` run from `main`.
5. Verify that the `deploy` branch was updated from the new `main` SHA.

Do not manually dispatch the deploy workflow from a feature branch unless the user
explicitly overrides this workflow.
