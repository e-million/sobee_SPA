# Sobee API - Local SQL Server (Docker)

## Run SQL Server in Docker

1. Copy the example env file and set a strong SA password:

   ```bash
   cp .env.example .env
   # edit .env and set SA_PASSWORD
   ```

2. Start SQL Server 2022:

   ```bash
   docker compose up -d
   ```

SQL Server will be available on `localhost:1433` and data will persist in the `mssql_data` volume.

## Configure the API connection string

The API reads the connection string from `ConnectionStrings__Sobee` first. Set it to match your
Docker SQL Server instance:

```bash
export ConnectionStrings__Sobee="Server=localhost,1433;Database=sobee;User Id=sa;Password=<password>;TrustServerCertificate=True;"
```

On Windows PowerShell:

```powershell
$env:ConnectionStrings__Sobee = "Server=localhost,1433;Database=sobee;User Id=sa;Password=<password>;TrustServerCertificate=True;"
```

If the env var is not set, the API falls back to the value in `sobee_API/sobee_API/appsettings.json`.
Update the placeholder `<password>` in that file for local use if needed (do not commit real passwords).

## Verify connectivity

Start the API and call the ping endpoint:

```bash
curl http://localhost:<api-port>/api/home/ping
```

A successful response looks like:

```json
{ "status": "ok", "db": true }
```
