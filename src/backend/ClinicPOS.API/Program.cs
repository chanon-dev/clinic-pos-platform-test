using System.Text;
using ClinicPOS.API.Middleware;
using ClinicPOS.Application.Appointments.Commands;
using ClinicPOS.Application.Appointments.Queries;
using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Patients.Commands;
using ClinicPOS.Application.Patients.Queries;
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
using RabbitMQ.Client;
using StackExchange.Redis;

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

// Application handlers
builder.Services.AddScoped<CreatePatientHandler>();
builder.Services.AddScoped<ListPatientsHandler>();
builder.Services.AddScoped<CreateAppointmentHandler>();
builder.Services.AddScoped<ListAppointmentsHandler>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<AssignRoleHandler>();
builder.Services.AddScoped<AssociateUserBranchesHandler>();
builder.Services.AddScoped<AuthenticateUserHandler>();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
