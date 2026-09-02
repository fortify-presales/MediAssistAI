using System.Security.Cryptography;
using System.Text;
using MediAssistAI.Agents;
using MediAssistAI.Api.Security;
using MediAssistAI.Data;
using MediAssistAI.Security;
using MediAssistAI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
	?? throw new InvalidOperationException("JWT configuration is required.");
var signingKey = jwtOptions.SigningKey;

if (string.IsNullOrWhiteSpace(signingKey))
{
	if (!builder.Environment.IsDevelopment())
	{
		throw new InvalidOperationException("Jwt:SigningKey must be configured outside Development.");
	}

	signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.EnableAnnotations());
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddMediAssistKernel(builder.Configuration, builder.Environment);
builder.Services.AddDbContext<MediAssistDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("MediAssist")));
builder.Services.AddScoped<IPrescriptionRepository, EfPrescriptionRepository>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IRefillRepository, EfRefillRepository>();
builder.Services.AddScoped<IRefillService, RefillService>();
builder.Services.AddScoped<PrescriptionAgent>();
builder.Services.AddScoped<PatientPlugin>();
builder.Services.AddScoped<PrescriptionPlugin>();
builder.Services.AddScoped<MedicationPlugin>();
builder.Services.AddScoped<UnsafePatientDataPlugin>();
builder.Services.AddScoped<UnsafeAdministrationPlugin>();
builder.Services.AddScoped<UnsafePrescriptionAgent>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPatientContext, HttpPatientContext>();
builder.Services.AddHealthChecks().AddDbContextCheck<MediAssistDbContext>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true, ValidIssuer = jwtOptions.Issuer, ValidateAudience = true,
		ValidAudience = jwtOptions.Audience, ValidateLifetime = true, ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
	};
});
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(PatientPolicies.ReadPrescription, policy => policy.RequireAuthenticatedUser());
	options.AddPolicy(PatientPolicies.RequestRefill, policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
	var database = scope.ServiceProvider.GetRequiredService<MediAssistDbContext>();
	await database.Database.EnsureCreatedAsync();
	await SyntheticDataSeeder.SeedAsync(database);
}

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program;
