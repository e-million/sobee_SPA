# Sobee API Architecture

## Overview

Projects and roles:
- `sobee_API/sobee_API`: Web/API layer (controllers, services, DTOs, mapping, validation, middleware).
- `sobee_API/Sobee.Domain`: Domain data layer (entities, repositories, DbContexts).
- `sobee_API/sobee_API.Tests`: Unit, repository integration, contract, and baseline tests.

## Layered Design

Layers and allowed dependencies:
- Presentation (Controllers): `sobee_API/sobee_API/Controllers`
  - Depends on: Services, DTOs, Mapping, ApiControllerBase.
- Application (Services): `sobee_API/sobee_API/Services`
  - Depends on: Domain (calculators, ServiceResult), Repositories (interfaces), Mapping, DTOs.
- Domain (Pure logic): `sobee_API/sobee_API/Domain`
  - Depends on: no EF Core, no ASP.NET Core.
- Data access (Repositories): `sobee_API/Sobee.Domain/Repositories`
  - Depends on: Entities + DbContexts in `Sobee.Domain/Data`.

Dependency flow (one direction):
Controllers -> Services -> Repositories -> DbContexts
Services -> Domain
Services/Controllers -> Mapping

## DbContexts

- `SobeecoredbContext`: main application data.
- `ApplicationDbContext`: Identity data (ASP.NET Core Identity).

## Error Handling

- Service layer returns `ServiceResult<T>` from `sobee_API/sobee_API/Domain/ServiceResult.cs`.
- Controllers map ServiceResult to HTTP via `ApiControllerBase` and `ServiceResultExtensions`.
- Error payload shape: `ApiErrorResponse` with `error`, `code`, and optional `details`.
- Standard error codes are documented in `sobee_API/sobee_API/DTOs/Common/ErrorCodes.cs`.

## DTO Mapping

Centralized mapping lives in `sobee_API/sobee_API/Mapping`:
- `ProductMapping`, `OrderMapping`, `ReviewMapping`, `FavoriteMapping`, `PaymentMapping`

Services use these extensions to keep controllers and services thin and consistent.

## Repository Guidelines

- Repositories encapsulate EF Core queries and persistence.
- Read-only queries use `AsNoTracking()` unless tracking is required.
- Aggregation queries handle SQLite limitations with client-side evaluation where needed.

## Testing Strategy

- Domain unit tests: `sobee_API/sobee_API.Tests/Domain`
- Service unit tests: `sobee_API/sobee_API.Tests/Services`
- Repository integration tests: `sobee_API/sobee_API.Tests/Repositories` (SQLite in-memory)
- Contract tests: `sobee_API/sobee_API.Tests/Contracts`
- Baseline endpoint tests: `sobee_API/sobee_API.Tests/Tests` (Phase0)

## Conventions

- Error codes come from `ErrorCodes` constants.
- Controllers should be thin: delegate to services and map ServiceResult.
- Services should not reference HTTP types.
- Repositories should not contain business logic.
