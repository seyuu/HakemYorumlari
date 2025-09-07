# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HakemYorumlari.csproj", "."]
RUN dotnet restore "HakemYorumlari.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "HakemYorumlari.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "HakemYorumlari.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# .NET uygulamasını kopyala
COPY --from=publish /app/publish .

# JSON dosyasını ayrıca kopyala (COPY . . zaten kopyalıyor ama emin olmak için)
COPY hakemyorumlama-2bf8fa35cf41.json /app/

# Cloud Run için gerekli environment variables
ENV ASPNETCORE_URLS=http://*:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
ENV GOOGLE_APPLICATION_CREDENTIALS=/app/hakemyorumlama-2bf8fa35cf41.json

EXPOSE $PORT
ENTRYpoint ["dotnet", "HakemYorumlari.dll"]