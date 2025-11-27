// --- AÑADIDOS PARA CLOUDINARY ---
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PeriodicoUpdate.Data;
using PeriodicoUpdate.Models;
using PeriodicoUpdate.Services;
using System.Text;
// ---------------------------------

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// --- CAMBIO A POSTGRESQL ---
builder.Services.AddDbContext<DBconexion>(option =>
    option.UseNpgsql(builder.Configuration.GetConnectionString("Connection")));
// ---------------------------

// --- CONFIGURACIÓN DE CLOUDINARY ---
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddSingleton(provider =>
{
    var settings = provider.GetRequiredService<IOptions<CloudinarySettings>>().Value;

    if (string.IsNullOrEmpty(settings.CloudName) ||
        string.IsNullOrEmpty(settings.ApiKey) ||
        string.IsNullOrEmpty(settings.ApiSecret))
    {
        throw new InvalidOperationException("No se encontraron las credenciales de Cloudinary en la configuración.");
    }

    Account account = new Account(
        settings.CloudName,
        settings.ApiKey,
        settings.ApiSecret
    );

    return new Cloudinary(account);
});

builder.Services.AddScoped<IPhotoService, PhotoService>();
// --- FIN DE CLOUDINARY ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Habilitar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Cors", policy =>
    {
        policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();
    });
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

var app = builder.Build();

// --- SWAGGER ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("Cors");
app.MapControllers();
app.MapGet("/", () => "API funcionando en Render");

// --- MIGRACIONES AUTOMÁTICAS EN ARRANQUE ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DBconexion>();
    db.Database.Migrate();

    // Opcional: Seed de datos iniciales
    if (!db.Categorias.Any())
    {
        db.Categorias.Add(new Categoria { Nombre = "General", Activo = true });
        db.SaveChanges();
    }
}

// --- PUERTO RENDER ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://*:{port}");
