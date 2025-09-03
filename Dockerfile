FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore (for better layer caching)
COPY HakemYorumlari.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/out --no-restore /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
ENV PORT=8080

# Copy the Google Cloud credentials file
COPY hakemyorumlama-2bf8fa35cf41.json /app/hakemyorumlama-2bf8fa35cf41.json

# Set the environment variable for Google Application Credentials
ENV GOOGLE_APPLICATION_CREDENTIALS=/app/hakemyorumlama-2bf8fa35cf41.json
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "HakemYorumlari.dll"]