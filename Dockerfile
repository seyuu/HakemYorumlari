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

# Python ve Whisper kurulumu
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# yt-dlp kurulumu
RUN pip3 install yt-dlp

# Whisper kurulumu
RUN pip3 install openai-whisper

# Copy application files
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "HakemYorumlari.dll"]