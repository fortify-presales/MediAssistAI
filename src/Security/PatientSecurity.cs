namespace MediAssistAI.Security;

public interface IPatientContext { string? Subject { get; } }

public sealed class JwtOptions
{
	public const string SectionName = "Jwt";
	public required string Issuer { get; init; }
	public required string Audience { get; init; }
	public string? SigningKey { get; init; }
}

public static class PatientPolicies
{
	public const string ReadPrescription = "Patient.ReadPrescription";
	public const string RequestRefill = "Patient.RequestRefill";
}