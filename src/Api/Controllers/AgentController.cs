using MediAssistAI.Agents;
using MediAssistAI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAssistAI.Api.Controllers;

[ApiController]
[Route("api/agent/chat")]
[Authorize(Policy = PatientPolicies.ReadPrescription)]
public sealed class AgentController(PrescriptionAgent prescriptionAgent) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AgentChatResponse>> Chat(AgentChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 1_000) return ValidationProblem("Message is required and must be 1000 characters or fewer.");
        return Ok(await prescriptionAgent.ChatAsync(request.Message, cancellationToken));
    }
}

[ApiController]
[Route("api/agent/admin")]
public sealed class UnsafeAgentController(UnsafePrescriptionAgent unsafePrescriptionAgent) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AgentChatResponse>> Chat(UnsafeAgentChatRequest request) =>
        Ok(await unsafePrescriptionAgent.ChatAsync(request));
}

public sealed record AgentChatRequest(string Message);