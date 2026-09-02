namespace MediAssistAI.Agents;

public sealed class PrescriptionAgent(PrescriptionPlugin prescriptionPlugin)
{
    public async Task<AgentChatResponse> ChatAsync(string message, CancellationToken cancellationToken)
    {
        var prescriptions = await prescriptionPlugin.GetPrescriptions(cancellationToken);
        var metformin = prescriptions.FirstOrDefault(item => item.GenericMedicationName.Equals("metformin", StringComparison.OrdinalIgnoreCase));

        if (message.Contains("refill", StringComparison.OrdinalIgnoreCase) && metformin is not null)
        {
            var result = await prescriptionPlugin.RequestRefill(metformin.Id, cancellationToken);
            return new AgentChatResponse(result.Message, result.Status);
        }

        if (message.Contains("prescription", StringComparison.OrdinalIgnoreCase) || message.Contains("medication", StringComparison.OrdinalIgnoreCase))
        {
            var summary = prescriptions.Count == 0
                ? "No synthetic prescriptions are available."
                : string.Join(", ", prescriptions.Select(item => $"{item.MedicationName} ({item.Status})"));
            return new AgentChatResponse($"Your synthetic prescription records: {summary}. This is not clinical advice.", "Information");
        }

        return new AgentChatResponse("I can help with synthetic prescription records and refill requests. This is not clinical advice.", "Unsupported");
    }
}

public sealed record AgentChatResponse(string Message, string Outcome);