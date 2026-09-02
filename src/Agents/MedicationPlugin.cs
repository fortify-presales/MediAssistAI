using Microsoft.SemanticKernel;

namespace MediAssistAI.Agents;

public sealed class MedicationPlugin
{
    [KernelFunction]
    public string GetMedicationInformation(string medicationName) =>
        medicationName.Equals("metformin", StringComparison.OrdinalIgnoreCase)
            ? "Metformin information is synthetic demonstration content only and is not clinical advice."
            : "No curated synthetic medication information is available for that medication.";
}