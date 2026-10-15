using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests
{
    public class RaceDayWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName =
            $"RaceDayTestDatabase_{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                var testSettings = new Dictionary<string, string?>
                {
                    ["Jwt:Key"] =
                        "RaceDayTestSigningKey-OnlyForAutomatedTests-2026!",
                    ["Jwt:Issuer"] = "RaceDay.Tests",
                    ["Jwt:Audience"] = "RaceDay.Tests"
                };

                configuration.AddInMemoryCollection(testSettings);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<RaceDayContext>();
                services.RemoveAll<DbContextOptions<RaceDayContext>>();

                services.AddDbContext<RaceDayContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
