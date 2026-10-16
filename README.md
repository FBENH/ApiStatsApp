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
| `Demo:Activa` | `true` creates the demo league and simulates its games one after another, only while someone is watching it (default `false`) |
| `Demo:Slug` | Public address of the demo league (default `liga-demo`) |
| `Demo:Usuario` / `Demo:Clave` | Account that owns the demo league. Without a password nobody can log in with it |
| `Demo:SegundosEntreJugadas` | Pause between simulated plays (default `4`, about 25 minutes per game) |
| `Demo:MinutosEntrePartidos` | Pause between demo games (default `5`) |

In Azure App Service, set them as App Settings using `__` instead of `:` (for example `AppSettings__Secreto`).

## Deploy

The demo runs on the free tiers of Azure:

1. **Database**: an Azure SQL Database with the free offer. Copy its ADO.NET connection string.
2. **API**: an App Service on Linux, plan F1, runtime .NET 10. In *Configuration*:
   - App settings `ConnectionStrings__default`, `AppSettings__Secreto` and `Demo__Activa=true`.
   - Turn on *Web sockets* for SignalR.
   - On the database server, allow access from Azure services.
3. **GitHub**: add the secret `AZURE_WEBAPP_PUBLISH_PROFILE` (downloaded from the App Service overview; basic auth publishing credentials must be enabled). The `Deploy to Azure` workflow publishes every merge to `main` to the app `statsappapi` (override it with the variable `AZURE_WEBAPP_NAME`).

The demo league only plays while someone is watching: a request to its public pages or a SignalR connection to it starts the next game, and it stops 10 minutes after the last visit. When nobody is watching it doesn't touch the database, so the serverless database can pause.

On F1 the app goes to sleep when nobody uses it. To keep it awake, ping `GET /salud`, which doesn't touch the database. If the app restarts in the middle of a demo game, that game is played again.

## Tests

```bash
dotnet test
```

Docker must be running: the tests start a SQL Server container.

## Database changes

Change the entities in `src/apiBask/Models/Entidades.cs` or `BasketContext.cs`, then add a migration:

```bash
dotnet tool restore
cd src/apiBask
dotnet ef migrations add <Name> -o Migrations
```
