# StatsApp API

REST API for StatsApp, an app to record basketball stats live during a game: teams, players, games and per-player and per-team box scores. The Angular frontend lives in [StatsAppFront](https://github.com/FBENH/StatsAppFront).

## Stack

- **Backend**: ASP.NET Core Web API on .NET 10, JWT authentication.
- **Database**: SQL Server with Entity Framework Core 10, code first. Migrations are applied on startup.
- **Images**: Azure Blob Storage (team logos and player photos).
- **Tests**: xUnit integration tests against a real SQL Server started with Testcontainers.

## Running locally

Requirements: .NET 10 SDK and Docker.

1. Start SQL Server:

   ```bash
   docker run -d --name statsapp-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Local_dev_123' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
   ```

2. Set the configuration with user secrets (never in `appsettings.json`):

   ```bash
   cd src/apiBask
   dotnet user-secrets set "ConnectionStrings:default" "Server=localhost,1433;Database=BasketDB;User ID=sa;Password=Local_dev_123;TrustServerCertificate=True"
   dotnet user-secrets set "AppSettings:Secreto" "<random string of at least 32 characters>"
   # Optional, only needed for image upload:
   dotnet user-secrets set "ConnectionStrings:Storage" "<Azure Storage connection string>"
   ```

3. Run it with `dotnet run`. The database is created on the first start and Swagger is at `/swagger`.

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:default` | SQL Server connection string |
| `AppSettings:Secreto` | JWT signing key, at least 32 characters |
| `ConnectionStrings:Storage` | Azure Storage connection string for images |
| `Storage:Container` | Blob container name (default `apibasket`) |

In Azure App Service, set them as App Settings using `__` instead of `:` (for example `AppSettings__Secreto`).

## Tests

```bash
dotnet test
```

Docker must be running: the tests start a SQL Server container.

## Database changes

Change the entities in `src/apiBask/Models` or `BasketContext.cs`, then add a migration:

```bash
dotnet tool restore
cd src/apiBask
dotnet ef migrations add <Name> -o Migrations
```
