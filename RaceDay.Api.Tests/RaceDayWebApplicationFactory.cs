using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<RaceDayContext>();
                services.RemoveAll<
                    DbContextOptions<RaceDayContext>>();

                services.AddDbContext<RaceDayContext>(
                    options =>
                        options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}