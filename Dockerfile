# Aşama 1: Uygulamayı derleme
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve bağımlılıkları geri yükle
COPY ["HakemYorumlari.csproj", "./"]
RUN dotnet restore "HakemYorumlari.csproj"

# Tüm kaynak kodunu kopyala
COPY . .
WORKDIR "/src"

# Uygulamayı derle ve yayınla
RUN dotnet publish "HakemYorumlari.csproj" -c Release -o /app/publish --no-restore

# Aşama 2: Çalışma zamanı imajı
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Derlenmiş ve yayınlanmış çıktıları önceki aşamadan kopyala
COPY --from=build /app/publish .

# Uygulamanın dinleyeceği portu ve URL'i ayarla
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "HakemYorumlari.dll"]
    