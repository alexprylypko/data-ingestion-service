using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using TransactionApi.Application.Commands;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Application.Queries;
using TransactionApi.Application.Validators;
using TransactionApi.Infrastructure.Data;
using TransactionApi.Options;

namespace TransactionApi.Extensions;

/// <summary>
/// Registers Transaction API services through focused extension methods.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string ConnectionStringsSectionName = "ConnectionStrings";
    private const long MaxRequestBodySizeBytes = 104_857_600;

    /// <summary>
    /// Adds all application options, services, repositories, handlers, and MVC dependencies.
    /// </summary>
    /// <param name="services">Service collection being configured.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTransactionApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddTransactionApiOptions(configuration)
            .AddTransactionApiMvc()
            .AddTransactionApiDataAccess()
            .AddTransactionApiApplication();

        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = MaxRequestBodySizeBytes);
        return services;
    }

    /// <summary>
    /// Configures Kestrel limits required by the Transaction API.
    /// </summary>
    /// <param name="webHostBuilder">The web host builder being configured.</param>
    /// <returns>The updated web host builder.</returns>
    public static IWebHostBuilder AddTransactionApiWebHost(this IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MaxRequestBodySizeBytes);
        return webHostBuilder;
    }

    private static IServiceCollection AddTransactionApiOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ConnectionStringsOptions>()
            .Bind(configuration.GetSection(ConnectionStringsSectionName))
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.WriteConnection),
                "WriteConnection connection string is required.")
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.ReadConnection),
                "ReadConnection connection string is required.")
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddTransactionApiMvc(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<TransactionInputDtoValidator>();
        return services;
    }

    private static IServiceCollection AddTransactionApiDataAccess(this IServiceCollection services)
    {
        services.AddSingleton<IWriteDbConnectionFactory>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value;
            return new PostgresWriteConnectionFactory(options.WriteConnection);
        });

        services.AddSingleton<IReadDbConnectionFactory>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value;
            return new PostgresReadConnectionFactory(options.ReadConnection);
        });

        services.AddScoped<ITransactionWriteRepository, TransactionWriteRepository>();
        services.AddScoped<ITransactionReadRepository, TransactionReadRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        return services;
    }

    private static IServiceCollection AddTransactionApiApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<TransactionInputDto>, TransactionInputDtoValidator>();
        services.AddScoped<IValidator<CsvTransactionRow>, CsvTransactionRowValidator>();

        services.AddScoped<IngestTransactionCommandHandler>();
        services.AddScoped<IngestBatchCommandHandler>();
        services.AddScoped<GetCustomerTransactionsQueryHandler>();
        services.AddScoped<GetSummaryStatsQueryHandler>();
        return services;
    }
}
