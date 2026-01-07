
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Sobee.Domain.Data;
using Sobee.Domain.Identity;
using sobee_API.Services;

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
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("http://localhost:4200") // Angular dev server
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                    // .AllowCredentials(); // only if you ever use cookies; not needed for bearer tokens
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

            // Configure Authentication to use Bearer Tokens (JWT)
            // This allows the API to read the "Authorization: Bearer <token>" header
            builder.Services.AddAuthentication()
                .AddBearerToken(IdentityConstants.BearerScheme);

            // Enable Authorization (allows use of [Authorize] attribute)
            builder.Services.AddAuthorization();

            builder.Services.AddScoped<AdminSeedService>();

            // ==========================================
            // 3. API & SWAGGER CONFIGURATION
            // ==========================================
            builder.Services.AddControllers();
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

            app.UseCors();

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

            if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Admin:SeedEnabled"))
            {
                using var scope = app.Services.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<AdminSeedService>();
                await seeder.SeedAsync();
            }

            app.Run();
        }
    }
}
