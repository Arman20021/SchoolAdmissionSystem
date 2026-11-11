
# ==========================================
# Stage 1: Build ASP.NET Core Application
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files first for dependency caching
COPY backend/Admission.Api/Admission.Api.csproj backend/Admission.Api/
COPY backend/Admission.BLL/Admission.BLL.csproj backend/Admission.BLL/
COPY backend/Admission.DAL/Admission.DAL.csproj backend/Admission.DAL/

# Restore dependencies
RUN dotnet restore backend/Admission.Api/Admission.Api.csproj

# Copy backend source code
COPY backend/ backend/

# Publish application
WORKDIR /src/backend/Admission.Api

RUN dotnet publish Admission.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# ==========================================
# Stage 2: Production Runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Admission.Api.dll"]
