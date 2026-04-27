using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using TransactionApi.Application.Commands;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Application.Queries;
using TransactionApi.Application.Validators;
using TransactionApi.Infrastructure.Data;

namespace TransactionApi;

/// <summary>Configures services and the HTTP pipeline for the Transaction API.</summary>
public sealed class Startup
{
    private readonly IConfiguration _configuration;

    /// <summary>Initialises <see cref="Startup"/> with the resolved application configuration.</summary>
    public Startup(IConfiguration configuration)
        => _configuration = configuration;

    /// <summary>Registers all application services into the DI container.</summary>
    public void ConfigureServices(IServiceCollection services, IWebHostBuilder webHostBuilder)
    {
        var writeCs = _configuration.GetConnectionString("WriteConnection")
            ?? throw new InvalidOperationException("WriteConnection connection string is required.");
        var readCs = _configuration.GetConnectionString("ReadConnection")
            ?? throw new InvalidOperationException("ReadConnection connection string is required.");

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<TransactionInputDtoValidator>();

        services.AddSingleton<IWriteDbConnectionFactory>(_ => new PostgresWriteConnectionFactory(writeCs));
        services.AddSingleton<IReadDbConnectionFactory>(_ => new PostgresReadConnectionFactory(readCs));

        services.AddScoped<ITransactionWriteRepository, TransactionWriteRepository>();
        services.AddScoped<ITransactionReadRepository, TransactionReadRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IValidator<TransactionInputDto>, TransactionInputDtoValidator>();
        services.AddScoped<IValidator<CsvTransactionRow>, CsvTransactionRowValidator>();

        services.AddScoped<IngestTransactionCommandHandler>();
        services.AddScoped<IngestBatchCommandHandler>();
        services.AddScoped<GetCustomerTransactionsQueryHandler>();
        services.AddScoped<GetSummaryStatsQueryHandler>();

        webHostBuilder.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 104_857_600);
        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 104_857_600);
    }

    /// <summary>Builds the HTTP request pipeline.</summary>
    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
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
