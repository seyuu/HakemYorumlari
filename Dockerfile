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

# --- YENİ EKLENEN BÖLÜM ---
# Gerekli araçları (yt-dlp ve ffmpeg) kurmak için root kullanıcısına geçiyoruz.
# Microsoft'un .NET imajları varsayılan olarak yetkisi kısıtlı 'app' kullanıcısını kullanır.
USER root

# Paket listelerini güncelliyor ve gerekli araçları kuruyoruz.
# --no-install-recommends ile sadece gerekli bağımlılıkları kurarak imaj boyutunu optimize ediyoruz.
RUN apt-get update && apt-get install -y --no-install-recommends \
    yt-dlp \
    ffmpeg \
    # Kurulum sonrası paket önbelleğini temizleyerek imaj boyutunu daha da küçültüyoruz.
    && rm -rf /var/lib/apt/lists/*

# Güvenlik best practice'i olarak, kurulumdan sonra tekrar yetkisi kısıtlı 'app' kullanıcısına dönüyoruz.
USER app
# --- YENİ BÖLÜM SONU ---

# .NET uygulamasını kopyala
COPY --from=publish /app/publish .

# JSON dosyasını ayrıca kopyala
COPY hakemyorumlama-2bf8fa35cf41.json /app/

# Cloud Run için gerekli environment variables
ENV ASPNETCORE_URLS=http://*:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
ENV GOOGLE_APPLICATION_CREDENTIALS=/app/hakemyorumlama-2bf8fa35cf41.json

EXPOSE $PORT
ENTRYPOINT ["dotnet", "HakemYorumlari.dll"]