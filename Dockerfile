# syntax=docker/dockerfile:1

# ---- Build stage: has the full .NET SDK, is discarded after publishing ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first, so the (slow) restore layer is cached
# and re-run only when the dependencies change, not on every code change.
COPY src/TaskTracker.Api/TaskTracker.Api.csproj src/TaskTracker.Api/
RUN dotnet restore src/TaskTracker.Api/TaskTracker.Api.csproj

COPY src/ src/
RUN dotnet publish src/TaskTracker.Api/TaskTracker.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---- Runtime stage: only the ASP.NET runtime, much smaller than the SDK image ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The SQLite file lives in /data. Mount a volume there to keep data between container runs.
# APP_UID is the non-root "app" user that ships with the official .NET images.
RUN mkdir /data && chown $APP_UID /data

COPY --from=build /app/publish .

# Never run as root inside the container.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Default="Data Source=/data/tasktracker.db"

EXPOSE 8080
VOLUME /data

ENTRYPOINT ["dotnet", "TaskTracker.Api.dll"]
