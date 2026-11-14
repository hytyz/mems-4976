# Skill: Identity, roles and tenant-aware authorization

## When to use
Adding an admin page, an API write endpoint, or anything that touches administrator accounts.

## Roles
- `SuperAdmin` (`AppRoles.SuperAdmin`): manages every tenant, creates/deletes tenants, manages accounts.
- `MunicipalityAdmin` (`AppRoles.MunicipalityAdmin`): assigned to exactly one tenant through
  `ApplicationUser.MunicipalityId`. Can edit their municipality (not its slug) and its positions and
  candidates.

`TenantClaimsPrincipalFactory` adds `tenant_id` and `tenant_slug` claims for a Municipality
Administrator. The Admin cookie and the API JWT are built from the same factory.

## Admin (Blazor Server)
- Pages inherit `[Authorize(Policy = AdminPolicies.AnyAdmin)]` from `Components/Pages/_Imports.razor`.
- Super-Admin-only pages live in `Components/Pages/Users` (`AdminPolicies.SuperAdmin`) or check
  `user.IsSuperAdmin()` and render `<AccessDeniedMessage />`.
- The service layer enforces the same rules again; the UI check is only for a friendly page.
- Security stamps are validated on every request, so deleting an account or resetting a password
  signs it out immediately.

## API
- `POST /api/auth/login` returns a JWT (30 minutes). Lockout: 5 failures → 5 minutes.
- Policies: `SuperAdmin`, `AnyAdmin`, `TenantAdmin` (role + tenant in URL must match).

## Passwords
`IdentityDefaults.Configure`: at least 8 characters with upper, lower, digit and symbol.
