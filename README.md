# TransactionApi

## 1. How to Run (Docker - recommended)

```bash
git clone <repo>
cd data-ingestion-service
docker-compose up --build
# API:     http://localhost:8080
# Swagger: http://localhost:8080/swagger
```

## 2. How to Run (local development)

```bash
# Start only the database:
docker-compose up db -d

# Run the API:
cd TransactionApi
dotnet run
```

## 3. How to Run Tests

```bash
dotnet test
```

## 4. How to Run the Coverage Report

Install the report generator once:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Linux / macOS:

```bash
./scripts/run-coverage.sh
```

Windows (PowerShell):

```powershell
.\scripts\run-coverage.ps1
```

The script:

- Collects Cobertura coverage data scoped to the `Application` layer
- Fails the build if line coverage drops below 80%
- Generates a human-readable HTML report at `coverage-report/index.html`
- Prints a text summary to the terminal

## 5. Architecture Description

- `Program.cs` is the entry point only and delegates startup work to `Startup.cs`.
- `Startup.cs` owns all dependency injection registration and HTTP pipeline configuration.
- CQRS is enforced through `ITransactionWriteRepository` and `ITransactionReadRepository`, each backed by dedicated connection factories and connection strings.
- Database provider specifics are isolated to `IReadDbConnectionFactory` and `IWriteDbConnectionFactory` implementations, so switching providers only requires replacing those classes and the driver package.
- CSV ingestion streams rows via CsvHelper `IAsyncEnumerable`, so large batch files are not loaded into memory at once.
- Duplicate protection uses both a handler pre-check and the database `UNIQUE` constraint for race-condition safety.

## 6. Trade-offs Considered

- No MediatR: direct handler injection keeps the call graph explicit and avoids an extra framework dependency.
- No EF Core: Dapper provides full SQL control and predictable performance for a write-heavy ingestion path.
- Separate read and write connection strings: production can route queries to a replica without code changes, while development can point both to the same database.
- Rejected batch rows stay in memory: that is acceptable for low rejection rates and keeps the implementation simple.

## 7. What You'd Do Differently With More Time

- Add an idempotency-key header to `POST /ingest/transaction`
- Add per-client rate limiting
- Add OpenTelemetry tracing across handlers and repositories
- Add integration tests with Testcontainers and a real PostgreSQL instance
- Add queue-backed ingestion for high-throughput real-time processing
- Add a background batch-processing workflow with status polling
