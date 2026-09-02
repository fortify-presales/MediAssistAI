# MediAssist AI Security Testing Scenarios

This repository is intentionally insecure for Fortify security testing. Do not deploy it to a production environment, connect it to real services, or use real credentials or patient data.

## Run the API

Run the application locally with the installed .NET SDK. The API uses only synthetic data. For SAST scans, use the repository script:

```powershell
.\scripts\run-fortify-sast.ps1 -DotnetRoot "$HOME\.dotnet" -BuildId "MediAssistAI-insecure"
```

The resulting FPR is local generated output under the ignored `artifacts\fortify\sast` directory.

## How to Interpret Scanner Coverage

The scanners answer different questions. A finding in one scanner is not expected to appear automatically in another scanner, and an absence from one report is not evidence that the behavior is safe.

- **Fortify SAST** observes source-code data flow and rules supported by the installed rulepack. It cannot prove what a deployed route returns for a live account, infer every business authorization rule, or report dependency CVEs from an FPR. Its rules also vary by rulepack version.
- **SCA** observes dependency manifests, resolved packages, known advisories, and policy metadata. It does not execute HTTP requests or analyze application data flow. A package CVE may exist even when no SAST issue references that package.
- **Fortify DAST** observes responses and behavior of a running, reachable deployment after it sends HTTP requests and payloads. It cannot see hidden code paths, prove data flow to a source-level sink, or determine that an installed package has a CVE unless that issue is exposed through a tested response.
- **Fortify Agentic Analyzer (FAA)** observes source architecture, LLM prompts, agent-to-tool relationships, authorization boundaries, and complementary Fortify findings. It does not crawl routes or send request bodies. It can identify agentic risks that are not represented by a classic source-to-sink SAST rule.

### Examples in This Repository

- **DAST but not necessarily SAST:** A live DAST scan can authenticate as Alex, request Jamie's patient ID from `GET /api/patients/{patientId}`, and observe cross-patient disclosure. SAST may report the EF query as `Access Control: Database`, group it with other database access paths, or not produce a separate IDOR category because authorization intent is a business rule.
- **FAA but not necessarily SAST:** FAA can reason that `UnsafePrescriptionAgent` imports destructive tools, combines a note containing instructions with trusted prompt text, and exposes the result through an unauthenticated agent entry point. Traditional SAST can flag individual authorization or data-flow defects, but it generally cannot determine that the tool set is excessive for the agent's purpose or that the combined agent design creates unsafe agency.
- **SCA but not SAST/DAST:** `Newtonsoft.Json` 12.0.1 and `System.Data.SqlClient` 4.8.3 have known advisories based on package identity and version. That is dependency intelligence, not a source-code or live-HTTP observation. A runtime exploit might be absent even though the dependency is still vulnerable.
- **SAST but not necessarily DAST:** SAST can see the path from `command` to `cmd.exe` in `POST /api/commands`. A DAST deployment may not confirm it if the process is blocked by hosting permissions, its scanner policy avoids dangerous payloads, or the endpoint cannot be reached during crawling.

Treat scanner results as complementary evidence: use SAST for source-level defects, SCA for components, DAST for deployed behavior, and FAA for agent architecture and trust-boundary analysis.

## Scanner Commands

Fortify Agentic Analyzer is a file-based scanner. It analyzes the source project and writes SARIF; it does not call the API or provide HTTP request bodies. Scan the agent implementation with:

```powershell
fortifyaa -scan . --scope src/Agents,src/Api --fpr <local-sast.fpr> --output artifacts/fortify/faa/MediAssistAI.faa.sarif --message-format plain
```

The repository FAA runner requires `-FortifyFprPath`, so each execution explicitly identifies its local SAST baseline.

The runner removes inherited `FCLI_DEFAULT_SSC_*` variables for its process so FAA uses the configured FoD context when both platform credential sets are present.

## Unified Scenario Catalog

The unsafe agent is exposed through `POST /api/agent/admin` without authorization. Endpoint requests below are manual runtime demonstrations or DAST seeds; FAA itself scans source files.

| Scenario | Surface | SAST validation | SCA validation | DAST validation | FAA validation | Exercise |
| --- | --- | --- | --- | --- | --- | --- |
| SQL injection | `GET /api/patients/search?name=` | Confirmed: SQL Injection, Critical | Not applicable | Pending deployed scan | Not applicable | Request a normal synthetic name such as `Alex`; DAST can mutate the `name` parameter. |
| Path manipulation | `GET /api/files?path=` | Confirmed: Path Manipulation, Critical | Not applicable | Pending deployed scan | Not applicable | Use a benign local text-file path in the training host; DAST can test traversal sequences. |
| SSRF | `GET /api/proxy?url=` | Confirmed: Server-Side Request Forgery, High | Not applicable | Pending deployed scan | Not applicable | Use a permitted public test URL; DAST can test URL parsing and internal-address payloads only in the isolated network. |
| Unsafe JSON deserialization | `POST /api/import` | Confirmed: Unsafe JSON Deserialization, Critical | Not applicable | Pending deployed scan | Not applicable | Post a JSON string body; DAST can vary type metadata. |
| Command injection | `POST /api/commands` | Confirmed: Command Injection, Critical | Not applicable | Pending deployed scan | Not applicable | Post the harmless value `"echo MediAssist training"`; DAST can test separators only in the isolated host. |
| Hardcoded secret | `GET /api/credentials` | Not reported by this rulepack/version | Not applicable | Pending deployed scan | Confirmed in FAA: Hardcoded Password | Request the endpoint to show the embedded training key. |
| IDOR | `GET /api/patients/{patientId}` | Confirmed: Access Control: Database, High | Not applicable | Pending deployed scan | Confirmed in FAA: IDOR | Request Jamie's ID, `0d0e1776-789f-438a-a0e7-4cadcd30d619`, while authenticated as Alex. |
| Excessive data exposure | `GET /api/patients/{patientId}/record` | Confirmed: Access Control: Database, High | Not applicable | Pending deployed scan | Confirmed in FAA: IDOR and Privacy Violation | Request Alex's ID, `8cbbbce4-24ba-4ea9-94b8-38a8971c0b76`, and inspect the returned relationship graph. |
| Mass assignment | `PUT /api/patients/{patientId}` | Confirmed: Mass Assignment, Medium | Not applicable | Pending deployed scan | Confirmed in FAA: Missing Authorization/IDOR | Send a full `Patient` JSON document and alter server-managed fields such as `Subject`. |
| Missing authorization | `POST /api/refills/{refillRequestId}/approve` | Grouped under Access Control: Database | Not applicable | Pending deployed scan | Confirmed in FAA: Missing Authorization | Create a synthetic refill request, then call the approval route without a role or approval check. |
| Excessive agency | Agent tools: `DeletePatient`, `ExportPatientData`, `ApproveRefill` | Not a distinct SAST agent-purpose rule | Not applicable | Pending deployed scan | Confirmed: unauthorized tool access/IDOR | Scan the unsafe agent files; manually post an agent-admin request containing `delete this patient`. |
| Agentic data exposure | Agent prompt receives full patient graph | Not a distinct SAST prompt-design rule | Not applicable | Pending deployed scan | Confirmed: Privacy Violation | Scan the unsafe agent files; manually request `show the patient record`. |
| Prompt injection | Synthetic note concatenated into prompt | Not a distinct SAST prompt-design rule | Not applicable | Not applicable | Confirmed: Prompt Injection | Scan [UnsafePrescriptionAgent.cs](../src/Agents/UnsafePrescriptionAgent.cs); the unsafe prompt is returned when no model is configured. |
| Agent refill workflow bypass | Agent direct approval tool | Not a distinct SAST agent-flow rule | Not applicable | Pending deployed scan | Confirmed: Missing Authorization | Create a synthetic request, then ask the unsafe agent to approve it. |
| Unbounded consumption | Unsafe Semantic Kernel invocation | Not a distinct SAST resource-policy rule | Not applicable | Not applicable | Confirmed: Unbounded Consumption | Configure `OpenAI__ApiKey` and `OpenAI__ModelId`, then post a long request to `/api/agent/admin`. |
| Vulnerable Newtonsoft.Json | `Newtonsoft.Json` 12.0.1 | Not applicable | Confirmed: `GHSA-5crp-9r3c-p9vr` | Not applicable | Not applicable | Run `dotnet restore`, Debricked, or FoD Open Source analysis and inspect the NU1903 warning/advisory. |
| Vulnerable SQL client | `System.Data.SqlClient` 4.8.3 | Not applicable | Confirmed: `GHSA-8g2p-5pqh-5jmc`, `GHSA-98g6-xh36-x2p7` | Not applicable | Not applicable | Run `dotnet restore`, Debricked, or FoD Open Source analysis and inspect NU1902/NU1903 warnings. |

SAST FPRs do not contain dependency CVEs. Use Debricked or Fortify on Demand Open Source analysis for the SCA rows. The first four agent scenarios use unsafe fallback logic without an OpenAI key; the unbounded model-invocation scenario requires configured OpenAI credentials.

## DAST Setup Later

Build the isolated test deployment with:

```powershell
docker build --tag mediassist-ai-security-test .
docker run --rm --publish 8080:8080 mediassist-ai-security-test
```

The OpenAPI discovery document is available at `http://<host>:8080/swagger/v1/swagger.json`. Configure the Fortify DAST scan with the deployed base URL and this document as the crawl seed, then include the DAST-applicable routes in the unified scenario catalog. The import and command endpoints now use documented JSON contracts: `{ "payload": "..." }` and `{ "command": "echo MediAssist security test" }`.

Use only synthetic accounts and data. The DAST target must have no network path to production systems or internal services outside the intended security-testing scope.