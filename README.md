# Test Databricks ODBC .NET Docker

A .NET 8 console application for testing and interacting with Databricks SQL Warehouse using the Simba Spark ODBC driver.

## Overview

This application demonstrates ODBC connectivity to Databricks SQL Warehouse from a containerized .NET environment. It supports two modes of operation:

- **Interactive Mode** (`--interactive` flag): Execute custom SQL queries interactively
- **Test Mode** (default): Periodically execute a suite of test queries to monitor connection health

## Prerequisites

- Docker (for running the container)
- .NET 8 SDK (for local development)
- Databricks SQL Warehouse with:
  - Hostname and Port
  - HTTP path to the warehouse
  - Personal access token

## Required Environment Variables

### `ODBC_CS`

The complete ODBC connection string for your Databricks SQL Warehouse. The format depends on where the application runs:

**For container execution** (Linux with bundled Simba Spark ODBC Driver):
```
Driver=/opt/simba/spark/lib/64/libsparkodbc_sb64.so;Host=<HOST>;Port=443;HTTPPath=<HTTP_PATH>;SSL=1;ThriftTransport=2;AuthMech=3;UID=token;PWD=<PAT>
```

**For local execution** (Windows with Simba Spark ODBC Driver installed):
```
Driver={Simba Spark ODBC Driver};Host=<HOST>;Port=443;HTTPPath=<HTTP_PATH>;SSL=1;ThriftTransport=2;AuthMech=3;UID=token;PWD=<PAT>
```

**Parameters:**
- `<HOST>`: Your Databricks workspace hostname (e.g., `dbc-abc123.cloud.databricks.com`)
- `<HTTP_PATH>`: The HTTP path to your SQL warehouse (e.g., `/sql/1.0/warehouses/xxxxxxx`)
- `<PAT>`: Your Databricks personal access token

## Building the Container

After making code changes, rebuild the Docker image:

```bash
docker build -t test-databricks-odbc:latest .
```

Or with a specific version tag:

```bash
docker build -t ghcr.io/lgiuliani80/test-databricks-odbc:v0.0.1 .
```

## Running the Container

### Test Mode (Default)

Executes a predefined set of test queries in continuous cycles:

```bash
docker run -e ODBC_CS="Driver=/opt/simba/spark/lib/64/libsparkodbc_sb64.so;Host=<HOST>;Port=443;HTTPPath=<HTTP_PATH>;SSL=1;ThriftTransport=2;AuthMech=3;UID=token;PWD=<PAT>" \
  test-databricks-odbc:latest
```

### Interactive Mode

Launch an interactive SQL shell:

```bash
docker run -it \
  -e ODBC_CS="Driver=/opt/simba/spark/lib/64/libsparkodbc_sb64.so;Host=<HOST>;Port=443;HTTPPath=<HTTP_PATH>;SSL=1;ThriftTransport=2;AuthMech=3;UID=token;PWD=<PAT>" \
  test-databricks-odbc:latest --interactive
```

### Using Environment File

Create a `.env` file with your credentials:

```env
ODBC_CS=Driver=/opt/simba/spark/lib/64/libsparkodbc_sb64.so;Host=dbc-abc123.cloud.databricks.com;Port=443;HTTPPath=/sql/1.0/warehouses/xxxxxxx;SSL=1;ThriftTransport=2;AuthMech=3;UID=token;PWD=dapi...
```

Then run:

```bash
docker run --env-file .env test-databricks-odbc:latest
```

**⚠️ Security Note:** Never commit `.env` files with credentials to version control. Add `.env` to `.gitignore`.

## Operation Modes

### Test Mode (Default)

Runs indefinitely, executing a predefined set of test queries in cycles with random intervals:

```sql
SELECT current_database() AS current_db
SHOW TABLES
SELECT rand()
SELECT COUNT(*) AS total_rows FROM information_schema.tables
SELECT cos(rand()) AS cos_random_value, current_timestamp() as now_time
```

**Timing:**
- Each query is followed by a random delay (1-750ms)
- Cycles are separated by random intervals (1-100 seconds)
- Useful for monitoring long-running connection stability

### Interactive Mode

Launches an interactive SQL shell (`dbsql>` prompt) where you can:

```
dbsql> SELECT current_database();
dbsql> SHOW TABLES;
dbsql> SELECT * FROM my_table LIMIT 10;
dbsql> quit
```

Commands:
- `quit` or `exit`: Close the connection and exit

## Local Development

### Prerequisites

- .NET 8 SDK installed
- Simba Spark ODBC Driver configured (Windows)
- Databricks SQL Warehouse connection details

### Build

```bash
dotnet build
```

### Run

Set the environment variable and run:

```bash
# Test mode
set ODBC_CS=Driver={Simba Spark ODBC Driver};Host=...
dotnet run

# Interactive mode
dotnet run -- --interactive
```

## Project Structure

```
TestDatabricksODBC/
├── Program.cs                       # Main application logic
├── TestDatabricksODBC.csproj        # Project configuration
├── Properties/
│   ├── launchSettings.json          # Debug and Docker launch profiles
│   └── PublishProfiles/
│       └── ghcr.io_lgiuliani80.pubxml  # Container registry publishing config
├── Dockerfile                       # Multi-stage Docker build
└── .gitignore                       # Git ignore rules
```

## Publishing to Container Registry

The project includes a publish profile for GitHub Container Registry (ghcr.io):

```bash
# Build and push to ghcr.io
dotnet publish -c Release /p:PublishProfile="ghcr.io_lgiuliani80"
```

Or manually:

```bash
docker build -t ghcr.io/lgiuliani80/test-databricks-odbc:v0.0.1 .
docker push ghcr.io/lgiuliani80/test-databricks-odbc:v0.0.1
```

**Prerequisites:**
- Logged in to Docker registry: `docker login ghcr.io`
- GitHub token with `write:packages` permission

## Technical Details

### Dependencies

- **.NET Runtime:** 8.0 (base image: `mcr.microsoft.com/dotnet/runtime:8.0`)
- **.NET SDK:** 8.0 (build image: `mcr.microsoft.com/dotnet/sdk:8.0`)
- **NuGet Packages:**
  - `System.Data.Odbc` (v8.0.1)
  - `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` (v1.22.1)
- **System Libraries:**
  - `unixodbc-dev`
  - `libgssapi-krb5-2`
- **ODBC Driver:** Simba Spark ODBC Driver v2.9.4.1013 (bundled in image)

### Docker Image Layers

The Dockerfile uses a multi-stage build:

1. **base:** .NET 8 runtime + system dependencies + ODBC driver
2. **build:** .NET 8 SDK + project restore + build
3. **publish:** Project publish step
4. **final:** Runtime layer + published application

## Troubleshooting

### Connection Failed

**Error:** `Failed to connect: ...`

- Verify `ODBC_CS` environment variable is set correctly
- Ensure Databricks workspace is accessible from your network
- Check that your personal access token is valid and not expired
- Verify SQL Warehouse is running and accessible

### ODBC Driver Not Found

**Error:** `Can't open lib 'libsparkodbc_sb64.so' ...`

- In container: Ensure driver path is `/opt/simba/spark/lib/64/libsparkodbc_sb64.so`
- Locally: Ensure Simba Spark ODBC Driver is installed on your system
- Rebuild the container: `docker build -t test-databricks-odbc:latest .`

### No Rows Returned

Some queries may return no rows depending on your warehouse configuration. The application displays `(No rows returned)` in such cases.

### Connection Timeout

If queries hang or timeout:
- Check your network connectivity to Databricks
- Verify the SQL Warehouse is not paused
- Check Databricks logs for server-side issues

## License

This project is provided as-is for testing and evaluation purposes.

## Author

Luigi Giuliani ([@lgiuliani80](https://github.com/lgiuliani80))
