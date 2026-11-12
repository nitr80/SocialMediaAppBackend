# SocialMediaAppBackend

A RESTful backend for a social media application, built with **ASP.NET Core** and **Entity Framework Core**. The API provides user authentication and endpoints for managing users and posts.

## Features

- User registration and login
- JWT-based authentication and authorization
- Access and refresh token handling
- Protected API endpoints
- Create, view and delete posts
- A feed of your posts and posts from users you follow
- Like and unlike posts
- Comments and comment likes
- Follow and unfollow users
- Search users by username
- Editable bio and profile picture upload
- Rate limiting on authentication endpoints
- PostgreSQL database persistence
- Entity Framework Core migrations
- Health check endpoint
- Structured JSON logging with correlation IDs
- Distributed tracing with OpenTelemetry (ASP.NET Core requests and Npgsql commands)
- Prometheus metrics at `/metrics` (request rate, error rate, p95/p99 latency)
- Pre-wired observability stack (Grafana, Prometheus, Loki, Alloy and Jaeger) for local development via Docker Compose

## Tech Stack

- **C#**
- **ASP.NET Core**
- **Entity Framework Core**
- **PostgreSQL**
- **JWT**
- **REST API**
- **OpenTelemetry** (traces, metrics and logs)
- **Grafana, Prometheus, Loki and Jaeger** (observability stack)
- **Docker Compose**

## Project Structure

```text
SocialMediaAppBackend/
├── Controllers/       # API controllers
├── Data/              # Database context and configuration
├── DTOs/              # Data transfer objects
├── Extensions/        # DI and pipeline extensions (logging, tracing, metrics)
├── Mappings/          # Entity to DTO mappings
├── Middleware/        # Correlation ID, exception handling and security headers
├── Migrations/        # EF Core database migrations
├── Models/            # Database entities
├── Options/           # Strongly-typed configuration options
├── Results/           # Result types returned by services
├── Services/          # Application and authentication logic
├── Tests/             # xUnit test suite
├── alloy/             # Alloy log collector configuration
├── grafana/           # Grafana provisioning (datasources)
├── jaeger/            # Jaeger storage configuration
├── loki/              # Loki configuration
├── prometheus/        # Prometheus scrape configuration
├── compose.yaml       # Docker Compose stack
└── Program.cs         # Application configuration and startup
```

## Authentication

The API uses **JSON Web Tokens (JWT)** for authentication.

After a successful login, the server provides an access token and a refresh token. Protected endpoints require a valid access token.

Refresh tokens are used to obtain a new access token when the current access token expires.

## API Overview

| Area | Endpoints |
| --- | --- |
| Authentication | `POST /Auth/Register`, `/Auth/Login`, `/Auth/Refresh`, `/Auth/Logout` |
| Users | `GET /Users/{id}`, `GET /Users?search=`, `PATCH /Users/me/Bio`, `PATCH /Users/me/ProfilePicture` |
| Posts | `GET /Posts`, `GET /Posts/{id}`, `POST /Posts`, `DELETE /Posts/{id}` |
| Post likes | `GET/POST/DELETE /Posts/{postId}/Likes`, `GET /Posts/me/Likes` |
| Comments | `GET/POST /Posts/{postId}/Comments` |
| Comment likes | `GET/POST/DELETE /Comments/{commentId}/Likes`, `GET /Comments/me/{postId}/Likes` |
| Follows | `POST/DELETE /Users/{userId}/Follows`, `GET /Users/{userId}/Follows/status` |
| Health | `GET /health/live`, `GET /health/ready`, `GET /health` (ready alias) |
| Metrics | `GET /metrics` (Prometheus; internal only) |

All endpoints except registration, login, refresh and health require a valid access token.

## Database

The application uses **PostgreSQL** with **Entity Framework Core** for data persistence.

Database changes are managed using EF Core migrations.

To apply the existing migrations:

```bash
dotnet ef database update
```

To create a new migration after changing the data model:

```bash
dotnet ef migrations add <MigrationName>
```

## Running the API

You can run the API in one of two ways:

### Option 1: Start a separate PostgreSQL instance and run the project with `dotnet run`

#### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- Entity Framework Core CLI
- A running PostgreSQL instance

#### Steps

1. Clone the repository and navigate to the project directory:

```bash
git clone <repository-url>
cd <repository-directory>
```

2. Restore dependencies:

```bash
dotnet restore
```

3. Configure your connection string and JWT key (see [Configuration](#configuration)).

4. Apply the database migrations:

```bash
dotnet ef database update
```

5. Start the development server:

```bash
dotnet run
```

The API will be available on the development URL displayed in the terminal.

### Option 2: Start the project using Docker Compose

#### Prerequisites

- [Docker](https://www.docker.com/get-started)

#### Steps

1. Clone the repository and navigate to the project directory:

```bash
git clone <repository-url>
cd <repository-directory>
```

2. Create a `.env` file in the project root with the required secrets:

```bash
JWT_SECRET=<your-secret-key>
POSTGRES_PASSWORD=<your-postgres-password>

# Blob storage (S3-compatible, e.g. Cloudflare R2)
BLOB_BUCKET_NAME=<your-bucket>
BLOB_ACCESS_KEY=<your-access-key>
BLOB_SECRET_KEY=<your-secret-key>
# Optional
BLOB_SERVICE_URL=<your-s3-endpoint>
BLOB_PUBLIC_URL=<your-public-base-url>

# Grafana (optional; defaults to admin/admin)
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=admin
```

3. Start the project using Docker Compose:

```bash
docker compose up --build
```

This starts the full stack defined in `compose.yaml`: PostgreSQL, a one-shot EF Core migration job, the API, Caddy (reverse proxy on ports 80/443) and the observability backend (Jaeger, Prometheus, Loki, Alloy and Grafana). Grafana is available at `http://localhost:3000`.

## Configuration

Application configuration can be provided through `appsettings.json`.

For local development, create a `.env` file in the project root with a JWT signing key and a PostgreSQL connection string:

```bash
JwtOptions__Key=<your-secret-key>
ConnectionStrings__Default=Host=localhost;Port=5432;Database=socialmediaapp;Username=postgres;Password=postgres
```

In the Docker Compose stack, a one-shot `migration` service applies the migrations (via the `--migrate` flag) before the API starts. The app targets **PostgreSQL 18**.

## Observability

The API emits logs, metrics and traces. `docker compose up` also starts a matching observability backend that is pre-provisioned and wired together in Grafana:

| Signal | Component | Internal endpoint |
| --- | --- | --- |
| Traces | Jaeger (Badger storage) | `http://jaeger:16686` |
| Metrics | Prometheus scraping `api:8080/metrics` every 15s | `http://prometheus:9090` |
| Logs | Alloy (Docker logs) → Loki | `http://loki:3100` |
| UI | Grafana | `http://localhost:3000` |

Only Grafana (`:3000`) and Caddy (`:80`/`:443`) are published to the host; the other services are reachable over the internal `app-network` or through Grafana.

### Distributed tracing

The API is instrumented with **OpenTelemetry**. It emits traces for:

- Incoming ASP.NET Core HTTP requests (excluding `/health*` and `/metrics`).
- Npgsql database commands issued through EF Core.

Each trace carries `service.name`, `service.version`, `deployment.environment` and `service.instance.id` resource attributes.

Configure an OTLP exporter by setting `OpenTelemetry:OtlpEndpoint` in `appsettings.json`, or the standard environment variable:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

- gRPC endpoint example: `http://localhost:4317`
- HTTP/protobuf endpoint example: `http://localhost:4318`

When no OTLP endpoint is configured:

- In **Development**, traces are printed to the console using the console exporter.
- In other environments, no exporter is registered and a startup warning is logged.

Trace IDs are surfaced in logs as `TraceId`/`SpanId`, and every request receives an `X-Correlation-ID` response header (client-supplied values are reused when valid).

### Inspecting traces with Docker Compose

`docker compose up` starts a [Jaeger](https://www.jaegertracing.io/) instance alongside the API. The API exports to it via `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317`, so traces are captured automatically.

Jaeger is not published to the host. Open Grafana at `http://localhost:3000`, go to **Explore**, select the **Jaeger** datasource, choose the `SocialMediaAppBackend` service and inspect request timelines (including Npgsql spans). Traces are stored on disk using Badger (the `jaeger-data` volume), so they survive container restarts.

### Logs

The API writes structured JSON logs to stdout (Serilog `CompactJsonFormatter`). In the Compose stack, [Alloy](https://grafana.com/docs/alloy/latest/) tails container logs through the Docker socket and pushes them to [Loki](https://grafana.com/docs/loki/latest/), where they can be queried from Grafana.

Every request log line includes `CorrelationId`, `TraceId`/`SpanId` (emitted as `@tr`/`@sp`) and request metadata, so logs can be correlated with traces.

### Metrics

OpenTelemetry metrics are exposed in Prometheus format at `GET /metrics`. The histogram buckets for `http.server.request.duration` are tuned for accurate p95/p99 latency.

Expected metric names:

- `http_server_request_duration_seconds_bucket`
- `http_server_request_duration_seconds_sum`
- `http_server_request_duration_seconds_count`
- `http_server_active_requests`

The `/metrics` request itself is not counted (`DisableHttpMetrics()`).

Example PromQL:

```promql
# Request rate by route and method
sum by (http_route, http_request_method) (
  rate(http_server_request_duration_seconds_count[5m])
)

# 5xx error rate
sum by (http_route, http_request_method) (
  rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])
)

# p95 latency
histogram_quantile(
  0.95,
  sum by (le, http_route, http_request_method) (
    rate(http_server_request_duration_seconds_bucket[5m])
  )
)

# p99 latency
histogram_quantile(
  0.99,
  sum by (le, http_route, http_request_method) (
    rate(http_server_request_duration_seconds_bucket[5m])
  )
)
```

### Grafana

[Grafana](https://grafana.com/grafana/) is pre-provisioned with the three datasources (`Prometheus`, `Loki` and `Jaeger`) so all three signals can be explored side by side at `http://localhost:3000`.

- **Prometheus** is the default datasource. Exemplar storage is enabled and metric exemplars carry the `trace_id` label, which links a metric point to its trace in Jaeger.
- **Loki** stores the API's JSON logs. Each log line carries the `@tr` trace ID, surfaced as a derived field.

The default credentials are `admin`/`admin`; override them with `GRAFANA_ADMIN_USER` and `GRAFANA_ADMIN_PASSWORD`.

> **Security:** `/metrics` is not protected by authentication. Do not expose it publicly. Prometheus scrapes it over the internal Docker network. The bundled `Caddyfile` returns `404` for public `/metrics` requests.

## Tests

Run the test suite with:

```bash
dotnet run --project Tests/SocialMediaAppBackend.Tests.csproj --no-build
```

## Related Project

This backend is used by the corresponding React frontend:

**Social Media Frontend:** `https://github.com/nitr80/SocialMediaAppFrontend`

## Purpose

This project was developed as a personal project to gain practical experience with **backend development, REST APIs, authentication, database management, Docker, and Entity Framework Core**.
