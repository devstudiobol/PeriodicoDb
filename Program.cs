// --- USINGS ---
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PeriodicoUpdate.Data;
using PeriodicoUpdate.Models;
using PeriodicoUpdate.Services;
using System.Text;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// CONFIG PORT PARA RENDER
// ----------------------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// ----------------------------
// SERVICES
// ----------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----------------------------
// CORS
// ----------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder
            .WithOrigins("*") // o tu frontend específico
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ----------------------------
// DATABASE
// ----------------------------
var connectionString = builder.Configuration.GetConnectionString("Connection");

builder.Services.AddDbContext<DBconexion>(options =>
    options.UseNpgsql(connectionString));

// ----------------------------
// CLOUDINARY
// ----------------------------
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddSingleton(provider =>
{
    var settings = provider.GetRequiredService<IOptions<CloudinarySettings>>().Value;

    if (string.IsNullOrEmpty(settings.CloudName) ||
        string.IsNullOrEmpty(settings.ApiKey) ||
        string.IsNullOrEmpty(settings.ApiSecret))
    {
        throw new InvalidOperationException("No se encontraron las credenciales de Cloudinary.");
    }

    var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
    return new Cloudinary(account);
});

builder.Services.AddScoped<IPhotoService, PhotoService>();

// ----------------------------
// JWT
// ----------------------------
var jwtKey = builder.Configuration["Jwt:Key"] ?? "default_dev_key_change_me";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// --- BUILD APP ---
var app = builder.Build();

try { using (var scope = app.Services.CreateScope()) { var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>(); if (dbContext.Database.IsRelational()) { dbContext.Database.Migrate(); } } } catch (Exception ex) { Console.WriteLine($"Error applying migrations: {ex.Message}"); }
  
// ----------------------------
// PIPELINE
// ----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "API funcionando en Render 🚀");

// ----------------------------
// RUN
// ----------------------------
app.Run();
