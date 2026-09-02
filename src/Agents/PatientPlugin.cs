using MediAssistAI.Security;
using Microsoft.SemanticKernel;

namespace MediAssistAI.Agents;

public sealed class PatientPlugin(IPatientContext patientContext)
{
    [KernelFunction]
    public string GetPatient() => patientContext.Subject is null ? "No authenticated synthetic patient." : "Authenticated synthetic patient.";
}