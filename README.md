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

## Development admin seeding and role-based auth

The API seeds an initial Admin user and the `Admin`/`Customer` roles on startup in Development
(or when `Admin:SeedEnabled=true` is set). Provide the admin credentials via configuration or
environment variables:

```bash
export Admin__Email="admin@example.com"
export Admin__Password="ChangeMe123!"
export Admin__FirstName="Dev"
export Admin__LastName="Admin"
```

PowerShell:

```powershell
$env:Admin__Email = "admin@example.com"
$env:Admin__Password = "ChangeMe123!"
$env:Admin__FirstName = "Dev"
$env:Admin__LastName = "Admin"
```

### Register and login with Identity endpoints

Register a new user:

```bash
curl -X POST http://localhost:<api-port>/register \\
  -H "Content-Type: application/json" \\
  -d '{\"email\":\"user@example.com\",\"password\":\"ChangeMe123!\"}'
```

Login to receive a bearer token:

```bash
curl -X POST http://localhost:<api-port>/login \\
  -H "Content-Type: application/json" \\
  -d '{\"email\":\"admin@example.com\",\"password\":\"ChangeMe123!\"}'
```

PowerShell login example:

```powershell
$response = Invoke-RestMethod -Method Post -Uri http://localhost:<api-port>/login `
  -ContentType "application/json" `
  -Body '{"email":"admin@example.com","password":"ChangeMe123!"}'
$token = $response.accessToken
```

### Call the admin-only endpoint

```bash
curl http://localhost:<api-port>/api/admin/ping \\
  -H "Authorization: Bearer <access_token>"
```

PowerShell:

```powershell
Invoke-RestMethod -Method Get -Uri http://localhost:<api-port>/api/admin/ping `
  -Headers @{ Authorization = "Bearer $token" }
```
