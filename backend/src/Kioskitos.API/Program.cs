using System.Text;
using Kioskitos.API.Middleware;
using Kioskitos.Application.Auth;
using Kioskitos.Infrastructure;
using Kioskitos.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Kioskitos.Application.GoogleCalendar;
using Kioskitos.Application.Clients;
using Kioskitos.Application.Projects;
using Kioskitos.Application.StageNames;
using Kioskitos.Application.Tags;
using System.Text.Json.Serialization;
using Kioskitos.Application.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    // Enums travel as text in the JSON ("InProgress" instead of 1).
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<GoogleLoginService>();
builder.Services.AddScoped<ConnectGoogleCalendarService>();
builder.Services.AddScoped<RegisterClientService>();
builder.Services.AddScoped<ClientQueryService>();
builder.Services.AddScoped<CreateProjectService>();
builder.Services.AddScoped<ProjectQueryService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<StageNameService>();
builder.Services.AddScoped<UserProfileService>();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key),
        "Jwt:Key must be configured.")
    .Validate(options => options.Key.Length >= 32,
        "Jwt:Key must contain at least 32 characters.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer),
        "Jwt:Issuer must be configured.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
        "Jwt:Audience must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<GoogleOptions>()
    .Bind(builder.Configuration.GetSection("Google"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId),
        "Google:ClientId must be configured.")
        .Validate(options => !string.IsNullOrWhiteSpace(options.ClientSecret),
        "Google:ClientSecret must be configured.")
    .ValidateOnStart();


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Configured lazily so that dotnet ef and the tests do not fail when Jwt__Key is missing.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        if (string.IsNullOrWhiteSpace(jwt.Key))
        {
            throw new InvalidOperationException("Jwt__Key must be configured.");
        }

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Kioskitos API",
        Version = "v1",
        Description = "Kioskitos system API."
    });
});

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kioskitos API v1");
    });
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}