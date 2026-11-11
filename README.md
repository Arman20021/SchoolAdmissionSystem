# Admission System

A containerized admission management application built with **ASP.NET
Core 9**, **Entity Framework Core**, **PostgreSQL 16**, **React**, and
**Docker Compose**.

The project is designed to run with one Docker command. A local
installation of Node.js, the .NET SDK, or PostgreSQL is not required to
run the containerized application. **Docker Desktop with Docker
Compose** is required.

## Table of Contents

-   [Project Overview](#project-overview)
-   [Technology Stack](#technology-stack)
-   [Application URLs](#application-urls)
-   [Requirements](#requirements)
-   [Run the Project with Docker](#run-the-project-with-docker)
-   [Environment Variables](#environment-variables)
-   [Docker Services](#docker-services)
-   [Database and Migrations](#database-and-migrations)
-   [Useful Docker Commands](#useful-docker-commands)
-   [Scalar API Documentation](#scalar-api-documentation)
-   [Frontend Configuration](#frontend-configuration)
-   [Admission Hold Configuration](#admission-hold-configuration)
-   [Concurrency Testing with k6](#concurrency-testing-with-k6)
-   [Data Persistence and Reset](#data-persistence-and-reset)
-   [Troubleshooting](#troubleshooting)
-   [Project Structure](#project-structure)
-   [Security Notes](#security-notes)

## Project Overview

The Admission System provides an admission workflow with class and seat
management. The ASP.NET Core backend exposes HTTP APIs, and the React
frontend provides the user interface.

The current implementation includes:

-   Admission class management.
-   Class seat statistics and availability.
-   Temporary admission holds with an expiry time.
-   Hold confirmation and cancellation workflows.
-   Student identity handling through the frontend request header.
-   Background processing for hold expiration.
-   Database constraints and indexes for admission data integrity.
-   A k6 script (`booking-test.js`) for concurrent booking tests.

The backend uses PostgreSQL through Entity Framework Core and the Npgsql
provider. The frontend communicates with the backend through Axios.

## Technology Stack

  Area                            Technology
  ------------------------------- -------------------------
  Backend API                     ASP.NET Core 9
  Backend language                C#
  ORM and migrations              Entity Framework Core 9
  Database                        PostgreSQL 16
  PostgreSQL provider             Npgsql
  Frontend                        React 19
  Frontend language               TypeScript
  Frontend build tool             Vite
  HTTP client                     Axios
  Frontend web server             Nginx
  Container runtime               Docker
  Multi-container orchestration   Docker Compose
  Load testing                    Grafana k6

## Application URLs

After the Docker services have started, use these addresses:

  Application                URL
  -------------------------- -----------------------------------------
  React frontend             <http://localhost:5173>
  ASP.NET Core backend       <http://localhost:8080>
  Scalar API documentation   <http://localhost:8080/scalar>
  OpenAPI document           <http://localhost:8080/openapi/v1.json>

PostgreSQL is not published on a host port in the Compose configuration.
It is accessible to the backend over the private Docker network.

## Requirements

Install:

-   [Docker Desktop](https://www.docker.com/products/docker-desktop/)
-   Docker Compose v2 (included with current Docker Desktop releases)

Make sure Docker Desktop is running before executing Docker commands.

You do not need to install these locally just to run the containerized
application:

-   Node.js or npm
-   .NET SDK
-   PostgreSQL
-   Nginx

These dependencies are provided by the Docker images.

## Run the Project with Docker

### 1. Clone the repository

Replace the placeholder with the actual Git repository URL:

``` bash
git clone YOUR_REPOSITORY_URL
cd AdmissionSystem
```

### 2. Optional: create a local `.env` file

The Compose configuration has development defaults, so a `.env` file is
not required for a basic local run.

To customize settings, copy the example file:

**Windows CMD:**

``` cmd
copy .env.example .env
```

**PowerShell, macOS, or Linux:**

``` bash
cp .env.example .env
```

Edit `.env` and set the values you want. Do not commit the real `.env`
file.

### 3. Validate the Compose configuration (optional)

``` bash
docker compose config
```

This prints the resolved Compose configuration. Review it before
starting services, especially if you supplied a custom `.env` file.

### 4. Build and start all services

To start in the foreground and see logs:

``` bash
docker compose up --build
```

To start in the background:

``` bash
docker compose up -d --build
```

On the first run, Docker downloads the required base images and builds
the backend and frontend images. This can take several minutes depending
on internet speed and computer performance. Later starts normally reuse
cached images and build layers.

### 5. Open the application

When the services are ready:

-   Frontend: <http://localhost:5173>
-   Backend: <http://localhost:8080>
-   Scalar: <http://localhost:8080/scalar>

Check service status:

``` bash
docker compose ps
```

The backend waits for PostgreSQL's health check before starting. The
frontend starts after the backend container has started.

## Environment Variables

The root `docker-compose.yml` reads the following variables. Values
shown are the development defaults in the Compose configuration.

  ----------------------------------------------------------------------------------
  Variable                        Default                    Purpose
  ------------------------------- -------------------------- -----------------------
  `POSTGRES_USER`                 `postgres`                 PostgreSQL username

  `POSTGRES_PASSWORD`             `admission_dev_password`   PostgreSQL password

  `POSTGRES_DB`                   `admission_db`             PostgreSQL database
                                                             name

  `HOLD_DURATION_SECONDS`         `600`                      Temporary admission
                                                             hold duration, in
                                                             seconds

  `HOLD_CHECK_INTERVAL_SECONDS`   `1`                        Background
                                                             hold-expiration check
                                                             interval, in seconds

  `FRONTEND_ORIGIN`               `http://localhost:5173`    Allowed frontend origin
                                                             for backend CORS

  `VITE_API_BASE_URL`             `http://localhost:8080`    Backend URL embedded
                                                             into the frontend at
                                                             build time
  ----------------------------------------------------------------------------------

### Example `.env`

``` dotenv
POSTGRES_USER=postgres
POSTGRES_PASSWORD=change_this_for_your_environment
POSTGRES_DB=admission_db

HOLD_DURATION_SECONDS=600
HOLD_CHECK_INTERVAL_SECONDS=1

FRONTEND_ORIGIN=http://localhost:5173
VITE_API_BASE_URL=http://localhost:8080
```

**Important:** `.env.example` is a template. Docker Compose
automatically reads a file named `.env` from the project directory; it
does not automatically read `.env.example`.

### Change a setting

1.  Stop or recreate the affected service as appropriate.
2.  Edit the root `.env` file.
3.  Recreate or rebuild the affected service.

After changing `VITE_API_BASE_URL`, rebuild the frontend because Vite
embeds `VITE_` variables during the frontend build:

``` bash
docker compose up -d --build frontend
```

After changing backend environment variables:

``` bash
docker compose up -d --force-recreate backend
```

The official PostgreSQL image applies `POSTGRES_USER`,
`POSTGRES_PASSWORD`, and `POSTGRES_DB` when initializing an empty data
directory. Changing these values does not automatically change
credentials or rename a database in an already-initialized persistent
volume. Handle database credential changes deliberately.

## Docker Services

The root `docker-compose.yml` defines three services.

### `postgres`

-   Uses `postgres:16-alpine`.
-   Stores database files in the named volume `postgres_data`.
-   Uses `pg_isready` as its health check.
-   Is available to other Compose services by hostname `postgres` on
    port `5432`.
-   Does not publish a database port to the host.

### `backend`

-   Builds from `backend/Admission.Api/Dockerfile`.
-   Uses the .NET 9 SDK image to build and the ASP.NET Core 9 runtime
    image to run.
-   Listens on port `8080`.
-   Is published on host port `8080`.
-   Connects to PostgreSQL using the Compose service hostname
    `postgres`.
-   Receives hold settings through environment variables.
-   Applies pending Entity Framework Core migrations during application
    startup.

### `frontend`

-   Builds the React application using Node.js.
-   Serves the built static files using Nginx.
-   Is published on host port `5173` (container port `80`).
-   Uses `VITE_API_BASE_URL` at build time to determine the API URL used
    by the browser.

## Database and Migrations

The backend is configured to apply pending Entity Framework Core
migrations during startup using:

``` csharp
await dbContext.Database.MigrateAsync();
```

A fresh PostgreSQL database can therefore be initialized from the
migrations included in the repository when the backend starts.

Keep migration files in source control. When the database model changes,
create and review a new EF Core migration and commit it with the code.

The backend's database connection string is supplied through
`DATABASE_CONNECTION_STRING`. In Docker, the hostname must be
`postgres`, not `localhost`, because the backend connects to PostgreSQL
over the Compose network.

Example connection string:

``` text
Host=postgres;Port=5432;Database=admission_db;Username=postgres;Password=your_password
```

## Useful Docker Commands

Run commands from the repository root.

  -------------------------------------------------------------------------------
  Task                                Command
  ----------------------------------- -------------------------------------------
  Start and build all services        `docker compose up --build`

  Start in background                 `docker compose up -d --build`

  Show service status                 `docker compose ps`

  Show all service logs               `docker compose logs -f`

  Show backend and frontend logs only `docker compose logs -f backend frontend`

  Show PostgreSQL logs                `docker compose logs -f postgres`

  Stop services and keep database     `docker compose down`
  volume                              

  Rebuild backend                     `docker compose build backend`

  Rebuild frontend                    `docker compose build frontend`

  Restart backend                     `docker compose restart backend`

  Restart frontend                    `docker compose restart frontend`
  -------------------------------------------------------------------------------

To follow only application logs:

``` bash
docker compose logs -f backend frontend
```

To stop the application without deleting persistent database data:

``` bash
docker compose down
```

## Scalar API Documentation

Scalar is hosted by the ASP.NET Core backend. It is not a separate
Docker service.

1.  Start the application:

    ``` bash
    docker compose up -d
    ```

2.  Open Scalar:

    <http://localhost:8080/scalar>

3.  Use the available API operations to inspect and send requests to the
    backend.

The OpenAPI JSON document is available at:

<http://localhost:8080/openapi/v1.json>

The backend maps OpenAPI and Scalar in the `Docker` environment. If
Scalar is unavailable, inspect the backend logs:

``` bash
docker compose logs --tail=200 backend
```

## Frontend Configuration

The frontend uses Axios. Its API base URL is read from:

``` text
VITE_API_BASE_URL
```

The default local Docker value is:

``` text
http://localhost:8080
```

This value is embedded into the frontend bundle during the Vite build.
If it changes, rebuild the frontend image:

``` bash
docker compose up -d --build frontend
```

The browser must use a URL that it can reach. Do not set the
browser-facing API URL to `http://backend:8080`; the hostname `backend`
is for communication between containers, not for the user's browser.

## Admission Hold Configuration

The hold duration is controlled by:

``` text
HOLD_DURATION_SECONDS
```

Default:

``` dotenv
HOLD_DURATION_SECONDS=600
```

This represents 600 seconds (10 minutes). For a 60-second hold:

``` dotenv
HOLD_DURATION_SECONDS=60
```

The background expiration check interval is configured through:

``` dotenv
HOLD_CHECK_INTERVAL_SECONDS=1
```

Compose passes this value to the ASP.NET Core configuration key
`AdmissionHold:CheckIntervalSeconds`.

A hold's expiration timestamp determines when it expires. The background
expiration service processes expired holds. Class availability should
reflect expired holds according to the backend repository and service
logic.

After changing hold settings, recreate the backend container:

``` bash
docker compose up -d --force-recreate backend
```

## Concurrency Testing with k6

The repository includes `booking-test.js` for load testing the booking
endpoint.

Install k6 separately on the machine used for load testing, or run it
from a k6 container. k6 is a testing tool and is not required to run the
application itself.

Before testing:

-   Make sure the Docker application is running.
-   Make sure the target class exists.
-   Make sure the class has the intended number of available seats.
-   Use valid, unique student IDs as expected by the API.
-   Use a development/test database, not production data.

Example command from the repository root (Windows CMD):

``` cmd
k6 run -e CLASS_ID=YOUR_CLASS_ID -e VUS=50 booking-test.js
```

Review the script before running it to confirm the request URL, payload,
and environment-variable names. The expected business outcome depends on
the target class's available seat count and the script's checks.

## Data Persistence and Reset

PostgreSQL data is stored in the named Docker volume:

``` text
postgres_data
```

Stopping or removing containers does not normally remove this named
volume.

### Stop the application and preserve data

``` bash
docker compose down
```

### Start again with the existing data

``` bash
docker compose up -d
```

### Permanently delete the Compose database volume

**Destructive:** the following command removes Compose-managed volumes,
including PostgreSQL data:

``` bash
docker compose down -v
```

Do not use `-v` unless you intentionally want to erase the database and
start with a fresh database. Back up important data first.

## Troubleshooting

### Frontend cannot reach the backend

-   Confirm the backend container is running:

    ``` bash
    docker compose ps
    ```

-   Check backend logs:

    ``` bash
    docker compose logs --tail=200 backend
    ```

-   Confirm `VITE_API_BASE_URL` is `http://localhost:8080` for a browser
    accessing the application locally.

-   If you changed `VITE_API_BASE_URL`, rebuild the frontend:

    ``` bash
    docker compose up -d --build frontend
    ```

### Backend cannot connect to PostgreSQL

-   Check PostgreSQL status and logs:

    ``` bash
    docker compose ps
    docker compose logs --tail=200 postgres
    ```

-   In the backend connection string, use `Host=postgres;Port=5432`, not
    `Host=localhost`.

-   Confirm the database name, username, and password match the
    initialized PostgreSQL database.

### Scalar does not open

-   Open <http://localhost:8080/scalar>.

-   Check that the backend is running in the `Docker` environment.

-   Inspect backend logs:

    ``` bash
    docker compose logs --tail=200 backend
    ```

### Port is already in use

If ports `5173` or `8080` are occupied, stop the other application using
that port or change the host-side port mapping in `docker-compose.yml`.
If you change the frontend host port, also update `FRONTEND_ORIGIN` to
match the new browser origin and recreate the services.

### Rebuild after code changes

``` bash
docker compose up -d --build
```

## Project Structure

The relevant high-level structure is:

``` text
AdmissionSystem/
├── .env.example
├── .gitignore
├── .dockerignore
├── docker-compose.yml
│
├── backend/
│   ├── Admission.Api/
│   │   ├── Dockerfile
│   │   └── ...
│   ├── Admission.BLL/
│   │   └── ...
│   └── Admission.DAL/
│       └── ...
│
├── frontend/
│   ├── Dockerfile
│   ├── .dockerignore
│   ├── nginx.conf
│   ├── package.json
│   ├── package-lock.json
│   └── src/
│
└── booking-test.js
```

## Security Notes

-   The default PostgreSQL password is for local development only.
    Change it for any shared or deployed environment.
-   Never commit `.env`, real passwords, production connection strings,
    API keys, or other secrets.
-   Commit `.env.example` with safe placeholder/development values.
-   Frontend `VITE_` variables are public because they are included in
    the browser bundle. Never put secrets in them.
-   PostgreSQL is intentionally not exposed on a host port by the
    Compose configuration described here.
-   Use HTTPS, secure secrets management, backups, and an appropriately
    secured deployment configuration before exposing the application to
    the public internet.

------------------------------------------------------------------------

## Quick Start

``` bash
git clone YOUR_REPOSITORY_URL
cd AdmissionSystem
docker compose up -d --build
```

Then open:

-   **Frontend:** <http://localhost:5173>
-   **Backend:** <http://localhost:8080>
-   **Scalar API:** <http://localhost:8080/scalar>

To view frontend and backend logs:

``` bash
docker compose logs -f frontend backend
```

To stop the application while preserving PostgreSQL data:

``` bash
docker compose down
```
