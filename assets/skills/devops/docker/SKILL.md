---
name: docker
description: >
  Docker standards for .NET 10 projects. Covers multi-stage Dockerfiles,
  docker-compose configuration, .dockerignore, and container best practices.
  Use when creating or modifying any Docker-related infrastructure file.
---

# Docker — TemperAI Standards

## Dockerfile — multi-stage build for .NET 10

Always use a multi-stage build with separate `build` and `runtime` stages.

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore dependencies
COPY *.sln ./
COPY src/Api/*.csproj src/Api/
COPY src/Application/*.csproj src/Application/
COPY src/Domain/*.csproj src/Domain/
COPY src/Infrastructure/*.csproj src/Infrastructure/
RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet publish src/Api/*.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN useradd -m -s /bin/bash appuser
USER appuser

# Copy published output
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "ProjectName.Api.dll"]
```

### Dockerfile rules

- Always use multi-stage builds — never ship the SDK to production.
- Always copy `.csproj` files first and run `dotnet restore` before copying the rest — enables Docker layer caching.
- Always run as a non-root user in the runtime stage.
- Always expose ports 8080 (HTTP) and 8081 (HTTPS).
- Always use `--no-restore` in the publish step since restore was already done.
- Always use the correct project file path matching the actual project structure.

## docker-compose.yml — API + database

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: projectname-api
    ports:
      - "5000:8080"
      - "5001:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORTS=8081
      - ConnectionStrings__Default=Host=db;Port=5432;Database=projectname_db;Username=postgres;Password=postgres
    depends_on:
      db:
        condition: service_healthy
    restart: unless-stopped
    networks:
      - projectname-network

  db:
    image: postgres:17-alpine
    container_name: projectname-db
    environment:
      - POSTGRES_DB=projectname_db
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped
    networks:
      - projectname-network

volumes:
  postgres-data:

networks:
  projectname-network:
    driver: bridge
```

### docker-compose rules

- Always include a healthcheck for the database service.
- Always use `depends_on` with `condition: service_healthy` for the API.
- Always use named volumes for database persistence.
- Always use a custom network to isolate services.
- Always adapt the database image to the project's choice (SQL Server, PostgreSQL, SQLite).
- Always use environment variable names matching the .NET configuration system (double underscore for nested config).

### SQL Server variant

```yaml
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: projectname-db
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - mssql-data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q 'SELECT 1' -C"]
      interval: 10s
      timeout: 5s
      retries: 5
```

### SQLite variant

For SQLite: no database container needed — the database file is mounted as a volume on the API container.

## .dockerignore

```
**/.git
**/.github
**/.vs
**/.vscode
**/bin
**/obj
**/node_modules
**/TestResults
**/TestResults/**
.gitignore
*.md
!README.md
```

### .dockerignore rules

- Always exclude `bin`, `obj`, `.git`, `.vs`, `.vscode`.
- Always exclude test results and node modules.
- Always exclude markdown files except `README.md`.
