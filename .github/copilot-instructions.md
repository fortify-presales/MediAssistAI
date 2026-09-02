# MediAssistAI Project Guidelines

## Purpose and Safety Boundary

MediAssistAI is a synthetic-data-only ASP.NET Core reference application for Fortify SAST, SCA, DAST/WebInspect, and Fortify Agentic Analyzer (FAA) security-testing exercises. It is not a clinical product and must never use real patient data, real credentials, or production services.

- Treat every intentionally insecure route, dependency, prompt, and agent tool as a training artifact; do not remove, harden, or silently "fix" it unless the task explicitly asks to change that scenario.
- Keep unsafe functionality clearly named, isolated from the normal workflow, and described as security-testing-only.
- Never make the application suitable for deployment beyond an isolated test environment. Do not add production integrations, real identity providers, or access to internal networks.
- Keep secrets out of source control. Use configuration or environment variables for values such as `Jwt:SigningKey` and `OpenAI__ApiKey`.

See [Security Testing Scenarios](../docs/security-testing-scenarios.md) for the authoritative scenario catalog, scanner applicability, safe exercise constraints, and DAST deployment requirements.

## Architecture

Preserve the existing dependency direction:

- `src/Models` contains domain entities and enums.
- `src/Security` contains security contracts, policies, and configuration options.
- `src/Services` contains application service and repository interfaces.
- `src/Data` implements EF Core persistence and synthetic data seeding.
- `src/Agents` contains Semantic Kernel registration, safe agent behavior, and deliberately unsafe agent scenarios.
- `src/Api` contains ASP.NET Core hosting, controllers, request handling, and HTTP-specific security context.

Add behavior in the appropriate layer. Keep controllers thin, use services for workflow logic, use repository interfaces for persistence access, and preserve the established ownership, eligibility, idempotency, and audit behavior for normal prescription and refill workflows.

## Adding Features and Scenarios

For normal application features:

- Use synthetic healthcare data only and maintain authorization boundaries based on the authenticated patient `sub` claim.
- Add or update focused tests in `tests/Api.Tests` for observable behavior, authorization, and workflow invariants.
- Maintain Swagger/OpenAPI annotations and JSON request contracts so API discovery remains usable for DAST.

For intentionally insecure security-testing scenarios:

- Make the vulnerability deliberate, minimal, and tied to a documented scanner exercise.
- Update the unified scenario catalog in [Security Testing Scenarios](../docs/security-testing-scenarios.md) with the route or component, expected SAST/SCA/DAST/FAA coverage, and a safe synthetic exercise.
- Add or update the Postman seed collection at [MediAssistAI-Fortify-DAST.postman_collection.json](../docs/MediAssistAI-Fortify-DAST.postman_collection.json) for every new `/api` route. Include required headers, authorization behavior, valid synthetic bodies, and configurable variables.
- Do not claim a scanner will detect a scenario without evidence. SAST observes source-level rules and data flow; SCA observes dependency advisories; DAST observes a running deployment; FAA assesses agent architecture and trust boundaries.

## Build and Validation

Use .NET 9. Validate code changes with:

```powershell
dotnet build MediAssistAI.sln --no-restore
dotnet test MediAssistAI.sln --no-build
```

When API routes or OpenAPI contracts change, also verify the Swagger document and Postman collection. For local Fortify SAST and FAA commands, follow [README.md](../README.md). Do not run security scans or DAST against anything other than the isolated synthetic test deployment.
