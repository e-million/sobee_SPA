# Sobee API Layer Rules

This repo follows a layered architecture. Keep changes aligned with these rules.

## Controller Layer
- Controllers should only handle routing, auth, model binding, and HTTP response mapping.
- Do not access DbContext or write EF queries in controllers.
- Use ApiControllerBase + ServiceResult mapping for consistent errors.

## Service Layer
- Services own business logic and orchestration.
- Services must not reference HTTP types (ControllerBase, IActionResult, HttpContext).
- Use ServiceResult<T> for errors.
- Use mapping extensions in `sobee_API/sobee_API/Mapping` for DTO projections.

## Repository Layer
- Repositories contain data access only.
- No business rules (pricing, discounts, status transitions) in repositories.
- Use AsNoTracking for read-only queries.

## Domain Layer
- No ASP.NET Core or EF Core dependencies.
- Pure logic only (calculators, state machines).

## Error Codes
- Use constants in `sobee_API/sobee_API/DTOs/Common/ErrorCodes.cs`.
*** End Patch"}]}
