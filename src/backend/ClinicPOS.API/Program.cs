using System.Text;
using ClinicPOS.API.Middleware;
using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Patients.Commands;
using ClinicPOS.Application.Patients.Queries;
using ClinicPOS.Application.Users.Commands;
using ClinicPOS.Application.Users.Queries;
using ClinicPOS.Infrastructure.Auth;
using ClinicPOS.Infrastructure.Persistence;
using ClinicPOS.Infrastructure.Persistence.Repositories;
using ClinicPOS.Infrastructure.Persistence.Seeder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=db;Port=5432;Database=clinicpos;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Tenant context (scoped)
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// Repositories
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();

// Application handlers
builder.Services.AddScoped<CreatePatientHandler>();
builder.Services.AddScoped<ListPatientsHandler>();
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
