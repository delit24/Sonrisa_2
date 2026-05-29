using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AlertSystem.Api.Data;
using AlertSystem.Api.Middleware;
using AlertSystem.Api.Models.Entities;
using AlertSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Services ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PreferenceService>();
builder.Services.AddScoped<ChannelService>();

// --- Authentication (JWT) ---
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// --- CORS ---
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",") ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- Controllers & OpenAPI ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Middleware Pipeline ---
app.MapOpenApi();

app.UseCors("Frontend");

// API Key middleware must come before auth for internal endpoints
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- Database Migration & Seed ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();

    // Seed admin user if database is empty
    if (!await context.Users.AnyAsync())
    {
        var adminEmail = builder.Configuration["AdminSeed:Email"] ?? "admin@alertsystem.local";
        var adminPassword = builder.Configuration["AdminSeed:Password"] ?? "Admin123!";

        context.Users.Add(new User
        {
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            FullName = "System Admin",
            Role = "admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        Console.WriteLine($"✅ Admin user seeded: {adminEmail}");
    }
}

Console.WriteLine("🚀 Alert System API started on http://localhost:5000");
app.Run();
