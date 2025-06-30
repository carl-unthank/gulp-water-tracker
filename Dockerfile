# Build stage for Blazor WASM Client
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-client
WORKDIR /src

# Copy client project files
COPY ["Gulp.Client/Gulp.Client.csproj", "Gulp.Client/"]
COPY ["Gulp.Shared/Gulp.Shared.csproj", "Gulp.Shared/"]

# Restore client dependencies
RUN dotnet restore "Gulp.Client/Gulp.Client.csproj"

# Copy client source code
COPY Gulp.Client/ Gulp.Client/
COPY Gulp.Shared/ Gulp.Shared/

# Build and publish client
WORKDIR "/src/Gulp.Client"
RUN dotnet publish "Gulp.Client.csproj" -c Release -o /app/client/publish

# Build stage for API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-api
WORKDIR /src

# Copy project files
COPY ["Gulp.Api/Gulp.Api.csproj", "Gulp.Api/"]
COPY ["Gulp.Infrastructure/Gulp.Infrastructure.csproj", "Gulp.Infrastructure/"]
COPY ["Gulp.Shared/Gulp.Shared.csproj", "Gulp.Shared/"]

# Restore dependencies
RUN dotnet restore "Gulp.Api/Gulp.Api.csproj"

# Copy source code
COPY Gulp.Api/ Gulp.Api/
COPY Gulp.Infrastructure/ Gulp.Infrastructure/
COPY Gulp.Shared/ Gulp.Shared/

# Build and publish API
WORKDIR "/src/Gulp.Api"
RUN dotnet publish "Gulp.Api.csproj" -c Release -o /app/api/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy API
COPY --from=build-api /app/api/publish .

# Copy Blazor WASM client to wwwroot
COPY --from=build-client /app/client/publish/wwwroot ./wwwroot

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "Gulp.Api.dll"]
