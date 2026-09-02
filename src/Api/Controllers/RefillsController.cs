using MediAssistAI.Security;
using MediAssistAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAssistAI.Api.Controllers;

[ApiController]
[Route("api/refills")]
[Authorize(Policy = PatientPolicies.RequestRefill)]
public sealed class RefillsController(IPatientContext patientContext, IRefillService refillService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RefillRequestResult>> Create(RefillRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(patientContext.Subject))
        {
            return Forbid();
        }

        var result = await refillService.RequestAsync(patientContext.Subject, request.PrescriptionId, cancellationToken);
        return result.Status == "NotFound" ? NotFound() : Ok(result);
    }
}

public sealed record RefillRequestDto(Guid PrescriptionId);