# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Gerekli paketleri yükle (ffmpeg vb. için)
RUN apt-get update && \
    apt-get install -y \
        ffmpeg \
        curl \
        ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Temp dizinini oluştur
RUN mkdir -p /tmp/hakemyorumlari && \
    chmod 777 /tmp/hakemyorumlari

# Copy published app
COPY --from=build /app/out .

# Google credentials dosyasını kopyala (eğer build time'da mevcutsa)
# Not: Production'da Secret Manager veya Environment Variable kullanmak daha güvenli
# Bu dosyalar Cloud Build'de Secret Manager'dan gelecek

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV PORT=8080
ENV TZ=Europe/Istanbul
ENV LANG=tr_TR.UTF-8

# Cloud Run'ın beklediği port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "HakemYorumlari.dll"]