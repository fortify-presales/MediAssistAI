using System.Security.Claims;
using MediAssistAI.Security;

namespace MediAssistAI.Api.Security;

public sealed class HttpPatientContext(IHttpContextAccessor httpContextAccessor) : IPatientContext
{
    public string? Subject => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
}