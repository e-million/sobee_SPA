
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Sobee.Domain.Data;
using Sobee.Domain.Identity;
using Sobee.Domain.Repositories;
using sobee_API.DTOs.Common;
using sobee_API.Middleware;
using sobee_API.Configuration;
using sobee_API.Services;
using sobee_API.Services.Interfaces;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace sobee_API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new RenderedCompactJsonFormatter());
            });

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

            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
                .AddDbContextCheck<ApplicationDbContext>(
                    name: "database_identity",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "ready" })
                .AddDbContextCheck<SobeecoredbContext>(
                    name: "database_core",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "ready" });

            builder.Services.AddScoped<GuestSessionService>();
            builder.Services.AddScoped<RequestIdentityResolver>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IPromoRepository, PromoRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IGuestSessionRepository, GuestSessionRepository>();
            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
            builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IAdminPromoRepository, AdminPromoRepository>();
            builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
            builder.Services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
            builder.Services.AddScoped<IAdminAnalyticsRepository, AdminAnalyticsRepository>();
            builder.Services.AddScoped<ISystemHealthRepository, SystemHealthRepository>();
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            builder.Services.AddScoped<IFavoriteService, FavoriteService>();
            builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
            builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
            builder.Services.AddScoped<IAdminPromoService, AdminPromoService>();
            builder.Services.AddScoped<IAdminUserService, AdminUserService>();
            builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
            builder.Services.AddScoped<IHomeService, HomeService>();

            var rateLimitMeter = new Meter("sobee_API.RateLimiting");
            var rateLimitCounter = rateLimitMeter.CreateCounter<long>("rate_limit_rejections");


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
            builder.Services.Configure<TaxSettings>(builder.Configuration.GetSection("TaxSettings"));

            static string ResolvePartitionKey(HttpContext context)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirstValue("sub");

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    return $"user:{userId}";
                }

                var remoteIp = context.Connection.RemoteIpAddress?.ToString();
                return string.IsNullOrWhiteSpace(remoteIp) ? "ip:unknown" : $"ip:{remoteIp}";
            }

            static FixedWindowRateLimiterOptions CreateAuthLimiterOptions()
                => new()
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                };

            static FixedWindowRateLimiterOptions CreateWriteLimiterOptions()
                => new()
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                };

            static FixedWindowRateLimiterOptions CreateGlobalLimiterOptions()
                => new()
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                };

            static FixedWindowRateLimiterOptions CreateAdminLimiterOptions()
                => new()
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                };

            static bool IsWriteMethod(string method)
                => HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

            static string ResolvePrincipalKey(HttpContext context)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirstValue("sub");

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    return $"user:{userId}";
                }

                var userName = context.User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    return $"user:{userName}";
                }

                if (context.Request.Headers.TryGetValue(GuestSessionService.SessionIdHeaderName, out var sessionIdValues))
                {
                    var sessionId = sessionIdValues.ToString();
                    if (!string.IsNullOrWhiteSpace(sessionId))
                    {
                        return $"guest:{sessionId}";
                    }
                }

                var remoteIp = context.Connection.RemoteIpAddress?.ToString();
                return string.IsNullOrWhiteSpace(remoteIp) ? "ip:unknown" : $"ip:{remoteIp}";
            }

            static string ResolvePolicyName(HttpContext context)
            {
                var path = context.Request.Path;
                if (path.StartsWithSegments("/api/admin"))
                {
                    return "AdminPolicy";
                }

                if (path.StartsWithSegments("/api/auth") || path.StartsWithSegments("/login"))
                {
                    return "AuthPolicy";
                }

                if ((path.StartsWithSegments("/api/cart") || path.StartsWithSegments("/api/orders"))
                    && IsWriteMethod(context.Request.Method))
                {
                    return "WritePolicy";
                }

                return "GlobalPolicy";
            }

            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("AuthPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ResolvePartitionKey(context),
                        _ => CreateAuthLimiterOptions()));

                options.AddPolicy("WritePolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ResolvePartitionKey(context),
                        _ => CreateWriteLimiterOptions()));

                options.AddPolicy("AdminPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ResolvePartitionKey(context),
                        _ => CreateAdminLimiterOptions()));

                options.AddPolicy("GlobalPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ResolvePartitionKey(context),
                        _ => CreateGlobalLimiterOptions()));

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    return ResolvePolicyName(context) switch
                    {
                        "AuthPolicy" => RateLimitPartition.GetFixedWindowLimiter(
                            ResolvePartitionKey(context),
                            _ => CreateAuthLimiterOptions()),
                        "WritePolicy" => RateLimitPartition.GetFixedWindowLimiter(
                            ResolvePartitionKey(context),
                            _ => CreateWriteLimiterOptions()),
                        "AdminPolicy" => RateLimitPartition.GetFixedWindowLimiter(
                            ResolvePartitionKey(context),
                            _ => CreateAdminLimiterOptions()),
                        _ => RateLimitPartition.GetFixedWindowLimiter(
                            ResolvePartitionKey(context),
                            _ => CreateGlobalLimiterOptions())
                    };
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var httpContext = context.HttpContext;

                    // OnRejectedContext does not expose PolicyName in this target.
                    // Since GlobalLimiter selects by path/method, compute it consistently here.
                    var policy = ResolvePolicyName(httpContext);

                    rateLimitCounter.Add(
                        1,
                        new KeyValuePair<string, object?>("policy", policy),
                        new KeyValuePair<string, object?>("method", httpContext.Request.Method),
                        new KeyValuePair<string, object?>("path", httpContext.Request.Path.Value ?? "unknown"));

                    if (httpContext.Response.HasStarted)
                        return;

                    httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    httpContext.Response.ContentType = "application/json";

                    // Optional (nice-to-have): emit Retry-After if available
                    // (metadata presence depends on limiter implementation)
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                        if (seconds > 0)
                            httpContext.Response.Headers.RetryAfter = seconds.ToString();
                    }

                    var payload = JsonSerializer.Serialize(new
                    {
                        error = "Too many requests.",
                        code = "RATE_LIMITED"
                    });

                    await httpContext.Response.WriteAsync(payload, cancellationToken);
                };

            });


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

                    static bool IsBodyBound(ParameterDescriptor p)
                    {
                        // Only treat [FromBody] (or inferred body for complex types) as "body".
                        // If BindingSource is null, we do NOT assume body.
                        return p.BindingInfo?.BindingSource == BindingSource.Body;
                    }

                    var bodyParamNames = context.ActionDescriptor.Parameters
                        .Where(IsBodyBound)
                        .Select(p => p.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    string NormalizeKey(string key)
                    {
                        // Binder-level errors often come through as "" or "$"
                        if (string.IsNullOrWhiteSpace(key) || key == "$")
                            return "body";

                        // If the key is exactly the name of a BODY parameter, map to "body"
                        if (bodyParamNames.Contains(key))
                            return "body";

                        // Strip "request." prefix ONLY for BODY-bound params
                        foreach (var bodyParam in bodyParamNames)
                        {
                            var prefix = $"{bodyParam}.";
                            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                key = key.Substring(prefix.Length);
                                break;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(key))
                            return "body";

                        // Convert dotted segments to camelCase but preserve bracket indexes: Items[0].ProductId -> items[0].productId
                        var parts = key.Split('.');
                        for (var i = 0; i < parts.Length; i++)
                        {
                            var part = parts[i];
                            if (string.IsNullOrWhiteSpace(part))
                                continue;

                            var bracketIndex = part.IndexOf('[');
                            var namePart = bracketIndex >= 0 ? part[..bracketIndex] : part;
                            var suffix = bracketIndex >= 0 ? part[bracketIndex..] : string.Empty;

                            var camelName = string.IsNullOrWhiteSpace(namePart)
                                ? namePart
                                : namingPolicy.ConvertName(namePart);

                            parts[i] = $"{camelName}{suffix}";
                        }

                        return string.Join('.', parts);
                    }

                    var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

                    foreach (var entry in context.ModelState)
                    {
                        if (entry.Value == null || entry.Value.Errors.Count == 0)
                            continue;

                        var normalizedKey = NormalizeKey(entry.Key);

                        var messages = entry.Value.Errors
                            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Validation failed." : e.ErrorMessage)
                            .ToArray();

                        if (errors.TryGetValue(normalizedKey, out var existing))
                            errors[normalizedKey] = existing.Concat(messages).ToArray();
                        else
                            errors[normalizedKey] = messages;
                    }

                    var response = new ApiErrorResponse(
                        "Validation failed.",
                        "VALIDATION_ERROR",
                        new { errors }
                    );

                    return new BadRequestObjectResult(response);
                };
            }); builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation();
                    metrics.AddMeter(rateLimitMeter.Name);
                    metrics.AddPrometheusExporter();
                });

            // Customize Swagger to support JWT Bearer Authentication
            builder.Services.AddSwaggerGen(c =>
            {
                c.CustomSchemaIds(type => type.FullName);
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
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler(handler =>
                {
                    handler.Run(async context =>
                    {
                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                        var feature = context.Features.Get<IExceptionHandlerFeature>();
                        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value)
                            ? value?.ToString()
                            : null;

                        if (feature?.Error != null)
                        {
                            logger.LogError(
                                feature.Error,
                                "Unhandled exception {ExceptionType}: {Message}. CorrelationId: {CorrelationId}",
                                feature.Error.GetType().FullName,
                                feature.Error.Message,
                                correlationId ?? "unknown");
                        }

                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json";

                        var payload = JsonSerializer.Serialize(new
                        {
                            error = "An unexpected error occurred.",
                            code = "SERVER_ERROR"
                        });

                        await context.Response.WriteAsync(payload);
                    });
                });
            }

            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.Use(async (context, next) =>
            {
                var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value)
                    ? value?.ToString()
                    : null;

                using (LogContext.PushProperty("CorrelationId", correlationId ?? context.TraceIdentifier))
                {
                    await next();
                }
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    var correlationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value)
                        ? value?.ToString()
                        : null;

                    diagnosticContext.Set("CorrelationId", correlationId ?? httpContext.TraceIdentifier);
                    diagnosticContext.Set("Principal", ResolvePrincipalKey(httpContext));
                    diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());

                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirstValue("sub");
                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        diagnosticContext.Set("UserId", userId);
                    }

                    var userName = httpContext.User.Identity?.Name;
                    if (!string.IsNullOrWhiteSpace(userName))
                    {
                        diagnosticContext.Set("UserName", userName);
                    }

                    if (httpContext.Request.Headers.TryGetValue(GuestSessionService.SessionIdHeaderName, out var sessionIdValues))
                    {
                        var sessionId = sessionIdValues.ToString();
                        if (!string.IsNullOrWhiteSpace(sessionId))
                        {
                            diagnosticContext.Set("GuestSessionId", sessionId);
                        }
                    }

                    var endpoint = httpContext.GetEndpoint();
                    var routePattern = endpoint?.Metadata.GetMetadata<RouteEndpoint>()?.RoutePattern.RawText;
                    if (!string.IsNullOrWhiteSpace(routePattern))
                    {
                        diagnosticContext.Set("RouteTemplate", routePattern);
                    }
                };
            });

            app.UseCors("AngularClient");

            //  IMPORTANT: These must be in this order (AuthN before AuthZ)
            app.UseAuthentication(); // "Who are you?"
            app.UseRateLimiter();
            app.UseAuthorization();  // "Are you allowed here?"

            // ==========================================
            // 5. ENDPOINTS
            // ==========================================

            // Expose the Identity endpoints (/register, /login, /refresh)
            app.MapIdentityApi<ApplicationUser>();

            static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
            {
                context.Response.ContentType = "application/json";

                var payload = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString()
                    }),
                    timestampUtc = DateTime.UtcNow
                };

                return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }

            // A test endpoint to verify your token is working
            app.MapGet("/api/secure-test", (System.Security.Claims.ClaimsPrincipal user) =>
            {
                return Results.Ok($"Success! You are logged in as: {user.Identity?.Name}");
            })
            .RequireAuthorization(); // This locks the endpoint

            // Map standard controllers
            app.MapControllers();

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = WriteHealthCheckResponse
            }).DisableRateLimiting();

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = WriteHealthCheckResponse
            }).DisableRateLimiting();

            app.MapPrometheusScrapingEndpoint("/metrics").DisableRateLimiting();

            app.Run();
        }
    }
}
