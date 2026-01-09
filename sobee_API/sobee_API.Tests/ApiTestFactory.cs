using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sobee.Domain.Data;
using System;
using System.Linq;

namespace sobee_API.Tests;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var coreDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SobeecoredbContext>));
            if (coreDescriptor != null)
            {
                services.Remove(coreDescriptor);
            }

            var identityDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (identityDescriptor != null)
            {
                services.Remove(identityDescriptor);
            }

            services.AddDbContext<SobeecoredbContext>(options =>
                options.UseInMemoryDatabase($"Sobeecoredb-{Guid.NewGuid()}"));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"ApplicationDb-{Guid.NewGuid()}"));
        });
    }
}
