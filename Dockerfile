    # Aşama 1: Uygulamayı derleme
    FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
    WORKDIR /src

    # Proje dosyalarını kopyala ve bağımlılıkları geri yükle
    COPY ["HakemYorumlari.csproj", "HakemYorumlari/"]
    RUN dotnet restore "HakemYorumlari/HakemYorumlari.csproj"

    # Tüm kaynak kodunu kopyala
    COPY . "HakemYorumlari/"
    WORKDIR "/src/HakemYorumlari"

    # Uygulamayı derle ve yayınla
    # Bu adım, view'ları ve diğer statik dosyaları yayın çıktısına dahil etmelidir.
    # --no-self-contained ve -p:PublishReadyToRun=false ekleyerek Cloud Run için optimize ederiz.
    RUN dotnet publish "HakemYorumlari.csproj" -c Release -o /app/publish --no-restore \
        -p:UseAppHost=false \
        -p:PublishTrimmed=true \
        -p:PublishSingleFile=true \
        -p:EnableCompressionInSingleFile=true

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
    