using System.Diagnostics;
using MediAssistAI.Data;
using MediAssistAI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace MediAssistAI.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class InsecureTestingController(MediAssistDbContext database, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private const string TrainingApiKey = "mediassist-training-secret-key-2026";

    [HttpGet("patients/search")]
    [SwaggerOperation(Tags = new[] { "Patients" }, Summary = "Search patients", Description = "Security Testing endpoint: intentionally susceptible to SQL injection.")]
    public async Task<IActionResult> SearchPatients([FromQuery] string name, CancellationToken cancellationToken)
    {
        var command = database.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT Id, DisplayName FROM Patients WHERE DisplayName LIKE '%{name}%'";
        await database.Database.OpenConnectionAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var patients = new List<object>();
        while (await reader.ReadAsync(cancellationToken)) patients.Add(new { Id = reader.GetString(0), DisplayName = reader.GetString(1) });
        return Ok(patients);
    }

    [HttpGet("files")]
    [SwaggerOperation(Tags = new[] { "Files" }, Summary = "Read a file", Description = "Security Testing endpoint: intentionally susceptible to path traversal.")]
    public IActionResult ReadFile([FromQuery] string path) => Content(System.IO.File.ReadAllText(path));

    [HttpGet("proxy")]
    [SwaggerOperation(Tags = new[] { "Proxy" }, Summary = "Proxy a URL", Description = "Security Testing endpoint: intentionally susceptible to server-side request forgery (SSRF).")]
    public async Task<IActionResult> Proxy([FromQuery] string url, CancellationToken cancellationToken) =>
        Content(await httpClientFactory.CreateClient().GetStringAsync(url, cancellationToken));

    [HttpPost("import")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(Tags = new[] { "Import" }, Summary = "Import serialized data", Description = "Security Testing endpoint: intentionally uses unsafe JSON deserialization.")]
    public IActionResult Import([FromBody] ImportRequest request) =>
        Ok(JsonConvert.DeserializeObject<object>(request.Payload, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }));

    [HttpPost("commands")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(Tags = new[] { "Commands" }, Summary = "Run a command", Description = "Security Testing endpoint: intentionally susceptible to command injection.")]
    public IActionResult RunCommand([FromBody] CommandRequest request) =>
        Ok(Process.Start(new ProcessStartInfo("cmd.exe", "/c " + request.Command) { UseShellExecute = false }));

    [HttpGet("credentials")]
    [SwaggerOperation(Tags = new[] { "Credentials" }, Summary = "Retrieve API credentials", Description = "Security Testing endpoint: intentionally exposes a hardcoded credential.")]
    public IActionResult GetCredentials() => Ok(new { ApiKey = TrainingApiKey });

    [HttpGet("patients/{patientId:guid}")]
    [SwaggerOperation(Tags = new[] { "Patients" }, Summary = "Get a patient", Description = "Security Testing endpoint: intentionally lacks object-level authorization.")]
    public async Task<IActionResult> GetPatient(Guid patientId, CancellationToken cancellationToken) =>
        Ok(await database.Patients.FindAsync([patientId], cancellationToken));

    [HttpGet("patients/{patientId:guid}/record")]
    [SwaggerOperation(Tags = new[] { "Patients" }, Summary = "Get a patient record", Description = "Security Testing endpoint: intentionally exposes a broad patient record without authorization.")]
    public async Task<IActionResult> GetPatientRecord(Guid patientId, CancellationToken cancellationToken) =>
        Ok(await database.Patients
            .Include(patient => patient.Prescriptions)
            .ThenInclude(prescription => prescription.Medication)
            .Include(patient => patient.Prescriptions)
            .ThenInclude(prescription => prescription.RefillRequests)
            .SingleOrDefaultAsync(patient => patient.Id == patientId, cancellationToken));

    [HttpPut("patients/{patientId:guid}")]
    [SwaggerOperation(Tags = new[] { "Patients" }, Summary = "Update a patient", Description = "Security Testing endpoint: intentionally permits mass assignment and lacks object-level authorization.")]
    public async Task<IActionResult> UpdatePatient(Guid patientId, [FromBody] Patient patient, CancellationToken cancellationToken)
    {
        database.Update(patient);
        await database.SaveChangesAsync(cancellationToken);
        return Ok(patient);
    }

    [HttpPost("refills/{refillRequestId:guid}/approve")]
    [SwaggerOperation(Tags = new[] { "Refills" }, Summary = "Approve a refill", Description = "Security Testing endpoint: intentionally permits an unauthenticated workflow action.")]
    public async Task<IActionResult> ApproveRefill(Guid refillRequestId, CancellationToken cancellationToken)
    {
        var request = await database.RefillRequests.FindAsync([refillRequestId], cancellationToken);
        if (request is null) return NotFound();
        request.Status = RefillRequestStatus.Approved;
        await database.SaveChangesAsync(cancellationToken);
        return Ok(request);
    }
}

public sealed record ImportRequest(string Payload);

public sealed record CommandRequest(string Command);