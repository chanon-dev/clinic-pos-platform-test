using System.Text;
using ClinicPOS.API.Middleware;
using ClinicPOS.API.Services;
using ClinicPOS.Application.Appointments.Commands;
using ClinicPOS.Application.Appointments.Queries;
using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Patients.Commands;
using ClinicPOS.Application.Patients.Queries;
using ClinicPOS.Application.PatientVisits.Commands;
using ClinicPOS.Application.PatientVisits.Queries;
using ClinicPOS.Application.Users.Commands;
using ClinicPOS.Application.Users.Queries;
using ClinicPOS.Infrastructure.Auth;
using ClinicPOS.Infrastructure.Caching;
using ClinicPOS.Infrastructure.Messaging;
using ClinicPOS.Infrastructure.Persistence;
using ClinicPOS.Infrastructure.Persistence.Repositories;
using ClinicPOS.Infrastructure.Persistence.Seeder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using StackExchange.Redis;
using Scalar.AspNetCore;
using System.Text.Json;

var HealthCheckJsonOptions = new JsonSerializerOptions { WriteIndented = true };

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=db;Port=5432;Database=clinicpos;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// RabbitMQ
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory { HostName = rabbitHost };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// Tenant context (scoped)
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// Repositories
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IPatientVisitRepository, PatientVisitRepository>();

// Application handlers
builder.Services.AddScoped<CreatePatientHandler>();
builder.Services.AddScoped<ListPatientsHandler>();
builder.Services.AddScoped<CreateAppointmentHandler>();
builder.Services.AddScoped<ListAppointmentsHandler>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<AssignRoleHandler>();
builder.Services.AddScoped<AssociateUserBranchesHandler>();
builder.Services.AddScoped<AuthenticateUserHandler>();
builder.Services.AddScoped<RecordVisitHandler>();
builder.Services.AddScoped<GetVisitHistoryHandler>();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClinicPOS-SuperSecret-Key-2026-Min32Chars!!";
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClinicPOS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ClinicPOS",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql")
    .AddRedis(redisConnectionString, name: "redis")
    .AddRabbitMQ(sp => sp.GetRequiredService<IConnection>(), name: "rabbitmq");

// RabbitMQ Consumer
builder.Services.AddHostedService<AppointmentNotificationConsumer>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-migrate and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Clinic POS API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
    });
}

app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds + "ms"
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds + "ms"
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, HealthCheckJsonOptions));
    }
});

app.Run();
