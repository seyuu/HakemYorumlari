# Aşama 1: Uygulamayı derleme
# .NET SDK imajını kullanarak uygulamayı derleriz.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Proje dosyalarını kopyala ve bağımlılıkları geri yükle
# Bu adım, proje dosyasını (örneğin HakemYorumlari.csproj) kopyalar.
# Sadece .csproj dosyasını kopyalayarak bağımlılıkları önbelleğe alabiliriz,
# böylece kaynak kod değişse bile bağımlılıklar tekrar indirilmez.
COPY ["HakemYorumlari.csproj", "HakemYorumlari/"]
RUN dotnet restore "HakemYorumlari/HakemYorumlari.csproj"

# Tüm kaynak kodunu kopyala
# Projenin geri kalan kaynak kodunu kopyalarız.
COPY . "HakemYorumlari/"
WORKDIR "/src/HakemYorumlari"

# Uygulamayı derle ve yayınla
# Uygulamayı Release modunda derler ve /app/publish dizinine yayınlar.
# --no-restore, bağımlılıkların zaten geri yüklendiğini belirtir.
RUN dotnet publish "HakemYorumlari.csproj" -c Release -o /app/publish --no-restore

# Aşama 2: Çalışma zamanı imajı
# ASP.NET çalışma zamanı imajını kullanarak nihai, hafif imajı oluştururuz.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Derlenmiş ve yayınlanmış çıktıları önceki aşamadan kopyala
# Sadece çalıştırılabilir dosyaları kopyalarız, SDK ve derleme araçları nihai imaja dahil edilmez.
COPY --from=build /app/publish .

# Uygulamanın dinleyeceği portu ve URL'i ayarla
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Uygulamayı başlat
# Uygulamanızın ana çalıştırılabilir dosyasını belirtiriz.
# "HakemYorumlari" sizin projenizin adıdır, bu ismi doğru yazdığınızdan emin olun.
ENTRYPOINT ["dotnet", "HakemYorumlari.dll"]
