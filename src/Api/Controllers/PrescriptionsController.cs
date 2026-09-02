using MediAssistAI.Security;
using MediAssistAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAssistAI.Api.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize(Policy = PatientPolicies.ReadPrescription)]
public sealed class PrescriptionsController(IPatientContext patientContext, IPrescriptionService prescriptionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrescriptionSummary>>> Get(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(patientContext.Subject))
        {
            return Forbid();
        }
        return Ok(await prescriptionService.GetForPatientAsync(patientContext.Subject, cancellationToken));
    }
}
