# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["HakemYorumlari.csproj", "."]
RUN dotnet restore "HakemYorumlari.csproj"

# Copy all source code
COPY . .

# Build the application
RUN dotnet build "HakemYorumlari.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "HakemYorumlari.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables for Cloud Run
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy Google credentials file
COPY hakemyorumlama-2bf8fa35cf41.json /app/

ENTRYPOINT ["dotnet", "HakemYorumlari.dll"]
