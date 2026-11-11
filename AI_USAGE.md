# AI Usage

## AI Tool Used

-   **Tool:** ChatGPT (OpenAI).
-   **Purpose:** Assistance with understanding, implementing, debugging,
    testing, and documenting the Admission System.

## Parts Where AI Was Used

-   Backend: ASP.NET Core, Entity Framework Core, admission holds,
    confirmation, cancellation, and configuration.
-   Frontend: React/TypeScript behavior, API integration, and error
    messages.
-   Database and concurrency: PostgreSQL constraints, seat availability,
    and k6 load-test guidance.
-   Docker: Dockerfiles, Docker Compose configuration, environment
    variables, and startup guidance.
-   Documentation: `README.md` and `ASSUMPTIONS.md`.

## AI Errors or Issues Found

-   The supplied `Program.cs` contained a duplicate
    `AddDataAccess(connectionString)` registration. This was identified
    during review and reduced to one registration.
-   Some AI-generated configuration suggestions needed to be checked
    against the actual project structure and existing code before use.
-   Docker and frontend environment settings required careful
    verification, especially the browser-facing API URL and Vite
    build-time variables.

## How the Issues Were Detected

-   Reviewed generated code against the existing project files.
-   Ran `dotnet build` to check backend compilation.
-   Ran `npm run build` to check the frontend build.
-   Ran `docker compose config` and started the services with Docker
    Compose.
-   Reviewed application logs and used k6 to test concurrent booking
    requests.

## Responsibility and Verification

-   AI output was treated as assistance, not as automatically correct
    code.
-   Project changes were reviewed and tested before being accepted.
-   Database integrity, seat-capacity rules, and concurrency guarantees
    must be verified through backend and database tests.
