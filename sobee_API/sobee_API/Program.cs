
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Sobee.Domain.Data;
using Sobee.Domain.Identity;
using sobee_API.DTOs.Common;
using sobee_API.Services;
using System.Text.Json;

namespace sobee_API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ==========================================
            // 0. CORS (allow Angular dev client)
            // ==========================================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AngularClient", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:4200",
                            "https://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        // Critical: allow Angular to read your session headers
                        .WithExposedHeaders(
                            GuestSessionService.SessionIdHeaderName,
                            GuestSessionService.SessionSecretHeaderName);
                });
            });





            // ==========================================
            // 1. DATABASE CONNECTION
            // ==========================================
            // Connects to SQL Server using the "Sobee" string from appsettings.json
            // 1. DATABASE CONNECTION
            var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Sobee");
            var connectionString = !string.IsNullOrWhiteSpace(envConnectionString)
                ? envConnectionString
                : builder.Configuration.GetConnectionString("Sobee")
                    ?? throw new InvalidOperationException("Connection string 'Sobee' not found.");

            // Register the Identity DbContext (ApplicationDbContext) with the DI container.
            // - Tells ASP.NET Core: "Whenever something needs ApplicationDbContext, create one using this config."
            // - options.UseSqlServer(connectionString) tells EF Core to:
            //      * Use SQL Server as the database provider
            //      * Use the connection string named "MillionDb" from appsettings.json
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });


            // Register the main application DbContext (SobeecoredbContext) with the DI container.
            // - This context handles your shop/e-commerce tables (products, orders, cart, etc.).
            // - It's also configured to use SQL Server with the same connection string,
            //   so both contexts point at the same database (different tables, same DB).
            builder.Services.AddDbContext<SobeecoredbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            builder.Services.AddScoped<GuestSessionService>();
            builder.Services.AddScoped<RequestIdentityResolver>();


            // ==========================================
            // 2. IDENTITY & AUTHENTICATION
            // ==========================================
            // Configure IdentityCore with AppUser.
            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Development password settings (easy to remember)
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;

                // Ensure emails are unique in the database
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()                 // Enable Role management
            .AddEntityFrameworkStores<ApplicationDbContext>() // Store users in SQL Server via EF Core
            .AddSignInManager()                       // Essential for sign-in functionality
            .AddDefaultTokenProviders()               // Generates tokens for password reset, email confirmation
            .AddApiEndpoints();                       //  CRITICAL: Enables the new Identity API endpoints

            builder.Services.AddHostedService<sobee_API.Services.RoleSeedService>();


            // Configure Authentication to use Bearer Tokens (JWT)
            // This allows the API to read the "Authorization: Bearer <token>" header
            builder.Services.AddAuthentication(options =>
            {
                // Make Identity bearer tokens the default for [Authorize]
                options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
                options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
            })
                .AddBearerToken(IdentityConstants.BearerScheme);

            builder.Services.AddAuthorization();


            // ==========================================
            // 3. API & SWAGGER CONFIGURATION
            // ==========================================
            builder.Services.AddControllers();
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var namingPolicy = JsonNamingPolicy.CamelCase;
                    var bodyParameterNames = context.ActionDescriptor.Parameters
                        .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
                        .Select(p => p.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    string NormalizeKey(string key)
                    {
                        if (string.IsNullOrWhiteSpace(key) || key == "$")
                            return "body";

                        if (bodyParameterNames.Contains(key))
                            return "body";

                        foreach (var parameterName in bodyParameterNames)
                        {
                            var prefix = $"{parameterName}.";
                            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                key = key.Substring(prefix.Length);
                                break;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(key))
                            return "body";

                        var parts = key.Split('.');
                        for (var i = 0; i < parts.Length; i++)
                        {
                            var part = parts[i];
                            if (string.IsNullOrWhiteSpace(part))
                                continue;

                            var bracketIndex = part.IndexOf('[');
                            var namePart = bracketIndex >= 0 ? part[..bracketIndex] : part;
                            var suffix = bracketIndex >= 0 ? part[bracketIndex..] : string.Empty;
                            var camelName = string.IsNullOrWhiteSpace(namePart) ? namePart : namingPolicy.ConvertName(namePart);
                            parts[i] = $"{camelName}{suffix}";
                        }

                        return string.Join('.', parts);
                    }

                    var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

                    foreach (var entry in context.ModelState)
                    {
                        if (entry.Value == null || entry.Value.Errors.Count == 0)
                            continue;

                        var key = NormalizeKey(entry.Key);
                        var messages = entry.Value.Errors
                            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Validation failed." : e.ErrorMessage)
                            .ToArray();

                        if (errors.TryGetValue(key, out var existing))
                            errors[key] = existing.Concat(messages).ToArray();
                        else
                            errors[key] = messages;
                    }

                    var response = new ApiErrorResponse("Validation failed.", "VALIDATION_ERROR", new
                    {
                        errors
                    });

                    return new BadRequestObjectResult(response);
                };
            });
            builder.Services.AddEndpointsApiExplorer();

            // Customize Swagger to support JWT Bearer Authentication
            builder.Services.AddSwaggerGen(c =>
            {
                // This adds the "Padlock" icon to Swagger UI
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            });

            var app = builder.Build();

            // Seed roles/admin user (dev-only / gated by config)
            await sobee_API.Services.IdentitySeedService.SeedAsync(
                app.Services,
                app.Configuration,
                app.Logger
            );


            // ==========================================
            // 4. HTTP REQUEST PIPELINE (Middleware)
            // ==========================================

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AngularClient");


            //  IMPORTANT: These must be in this order (AuthN before AuthZ)
            app.UseAuthentication(); // "Who are you?"
            app.UseAuthorization();  // "Are you allowed here?"

            // ==========================================
            // 5. ENDPOINTS
            // ==========================================

            // Expose the Identity endpoints (/register, /login, /refresh)
            app.MapIdentityApi<ApplicationUser>();

            // A test endpoint to verify your token is working
            app.MapGet("/api/secure-test", (System.Security.Claims.ClaimsPrincipal user) =>
            {
                return Results.Ok($"Success! You are logged in as: {user.Identity?.Name}");
            })
            .RequireAuthorization(); // This locks the endpoint

            // Map standard controllers
            app.MapControllers();

            app.Run();
        }
    }
}
