# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HakemYorumlari.csproj", "."]
RUN dotnet restore "HakemYorumlari.csproj"
COPY . .
RUN dotnet build "HakemYorumlari.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "HakemYorumlari.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Cloud Run için gerekli environment variables
ENV ASPNETCORE_URLS=http://*:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "HakemYorumlari.dll"]