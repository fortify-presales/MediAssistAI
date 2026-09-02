# MediAssist AI

MediAssist AI is a synthetic-data-only healthcare API demonstration. It does not use real patient data and does not provide clinical advice.

## Current slice

- ASP.NET Core 9 Web API
- EF Core with a local SQLite database
- JWT-protected prescription endpoint
- Synthetic Metformin prescription seed data
- Public health endpoint
- Prescription Agent chat and idempotent refill workflow

## Run locally

```powershell
dotnet run --project src/Api/MediAssistAI.Api.csproj
```

The API creates `mediassist.db` automatically. `GET /health` is public. `GET /api/prescriptions` requires a JWT with a `sub` claim matching the seeded synthetic subject `synthetic-patient-alex`.

Swagger UI is available at `/swagger`, and the OpenAPI document for DAST discovery is available at `/swagger/v1/swagger.json`.

The development host generates an ephemeral JWT signing key if `Jwt:SigningKey` is absent. Non-development environments require `Jwt:SigningKey` from configuration or an environment variable; never store it in source control.

Semantic Kernel is configured from the `OpenAI` section. Production also requires `OpenAI:ApiKey` and `OpenAI:ModelId`; use environment variables such as `OpenAI__ApiKey`, never repository configuration. The agent endpoint is introduced in the next implementation slice.

`POST /api/agent/chat` and `POST /api/refills` require an authenticated synthetic patient. The agent exposes only prescription lookup and refill request behavior; refill mutations always use the same ownership, eligibility, idempotency, and audit workflow as the REST API.

## Fortify SAST

Run a local Fortify scan and write an FPR to the ignored `artifacts\fortify\sast` directory:

```powershell
sourceanalyzer -b MediAssistAI-local -clean
sourceanalyzer -b MediAssistAI-local dotnet build src/Api/MediAssistAI.Api.csproj --configuration Release
sourceanalyzer -b MediAssistAI-local -scan -f MediAssistAI.fpr
auditworkbench MediAssistAI.fpr
```

If a terminal cannot locate its .NET SDK, set the SDK root explicitly:

```powershell
.\scripts\run-fortify-sast.ps1 -DotnetRoot "$HOME\.dotnet"
```

Run Fortify Agentic Analyzer against the source tree and supply the local SAST FPR to use as complementary existing-findings input:

```powershell
.\scripts\run-fortify-agentic-analyzer.ps1 -FortifyFprPath ".\artifacts\fortify\sast\MediAssistAI.fpr"

```

Alternatively, run Fortify Agentic Analyzer against an existing Fortify on Demand Release:

```powershell
Get-ChildItem Env: | Where-Object Name -like "FCLI_DEFAULT_SSC_*" | ForEach-Object { Remove-Item "Env:$($_.Name)" }
fcli fod session login
fortifyaa  -scan . --scope src/Agents,src/Api --fod-release "fortify-presales/MediAssistAI:main" --output MediAssistAI.faa.sarif -clean
```

FAA SARIF output is written to the ignored `artifacts\fortify\faa` directory by default.

## Security Testing

This application intentionally includes insecure endpoints and dependencies for security testing. It must use synthetic data only and be deployed only in an isolated test environment. See [docs/security-testing-scenarios.md](docs/security-testing-scenarios.md) for the scenario catalog, scanner coverage, and exercise instructions.

## Scanner coverage

SAST can cover AI-adjacent implementation defects when there is a conventional source-to-sink path:
- Sensitive patient data sent to logs, files, HTTP responses, or outbound requests.
- Untrusted data reaching a conventional injection or deserialization sink.
- Generic unbounded loops/resource use, if the rulepack recognizes the pattern.
- It cannot confirm a deployed route's runtime behavior, infer every business authorization rule, or report dependency CVEs from an FPR.

SCA covers third-party component risk rather than application behavior:
- Dependency manifests and resolved packages, including known CVEs and policy metadata.
- Vulnerable agent or API dependencies, even when no SAST finding references the package.
- It does not execute the API or trace source-code data flow.

DAST covers observable behavior in a running, reachable isolated deployment:
- HTTP responses and input handling after crawling routes and sending requests or payloads.
- Authorization and data-exposure behavior that depends on live accounts, such as cross-patient access.
- It cannot inspect hidden code paths, prove source-to-sink data flow, or identify dependency CVEs solely from package metadata.

FAA covers the agent-specific reasoning:
- Whether DeletePatient, ExportPatientData, and ApproveRefill are excessive tools for a prescription agent.
- Whether patient notes are being treated as trusted instructions.
- Whether the whole agent/tool/prompt architecture allows destructive actions.
- Whether AI-specific limits and trust boundaries are missing.
- It does not crawl API routes, send HTTP request bodies, or replace SAST data-flow analysis and SCA dependency intelligence.