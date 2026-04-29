using TransactionApi.Extensions;
using TransactionApi.Middleware;

namespace TransactionApi;

/// <summary>
/// Configures services and the HTTP pipeline for the Transaction API.
/// </summary>
public sealed class Startup
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes <see cref="Startup"/> with the resolved application configuration.
    /// </summary>
    public Startup(IConfiguration configuration)
        => _configuration = configuration;

    /// <summary>
    /// Registers all application services into the DI container.
    /// </summary>
    public void ConfigureServices(IServiceCollection services, IWebHostBuilder webHostBuilder)
    {
        services.AddTransactionApiServices(_configuration);
        webHostBuilder.AddTransactionApiWebHost();
    }

    /// <summary>
    /// Builds the HTTP request pipeline.
    /// </summary>
    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }
}
