using Google.Cloud.Speech.V1;
using System.Diagnostics;
using System.Text.Json;

namespace HakemYorumlari.Services
{
    public class MatchRelevanceResult
    {
        public bool IsRelevantToMatch { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> DetectedTeams { get; set; } = new();
        public List<string> DetectedPositions { get; set; } = new();
    }

    public class AIVideoAnalysisService : IDisposable
    {
        private readonly ILogger<AIVideoAnalysisService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SpeechClient? _speechClient;
        private readonly string _tempDirectory;

        public AIVideoAnalysisService(ILogger<AIVideoAnalysisService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "HakemYorumlari");
            
            // Google Cloud Speech-to-Text client'ını başlat
            try
            {
                _speechClient = SpeechClient.Create();
                _logger.LogInformation("Google Cloud Speech-to-Text client başarıyla başlatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google Cloud Speech-to-Text client başlatılamadı, sadece Whisper kullanılacak");
            }

            // Temp dizinini oluştur
            Directory.CreateDirectory(_tempDirectory);
        }

        /// <summary>
        /// Video'dan AI ile transcript çeker (önce Whisper, sonra Google Cloud)
        /// </summary>
        public async Task<List<(TimeSpan Offset, string Text)>> ExtractTranscriptFromVideo(string videoId)
        {
            var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
            _logger.LogInformation($"Video {videoId} için AI transcript çekimi başlatılıyor");

            try
            {
                // 1. Önce Whisper'ı dene (ücretsiz)
                var whisperResult = await ExtractWithWhisper(videoUrl, videoId);
                if (whisperResult.Any())
                {
                    _logger.LogInformation($"Whisper ile {whisperResult.Count} segment elde edildi");
                    return whisperResult;
                }

                // 2. Whisper başarısızsa Google Cloud'u dene
                if (_speechClient != null)
                {
                    var googleResult = await ExtractWithGoogleCloud(videoUrl, videoId);
                    if (googleResult.Any())
                    {
                        _logger.LogInformation($"Google Cloud ile {googleResult.Count} segment elde edildi");
                        return googleResult;
                    }
                }

                _logger.LogWarning($"Video {videoId} için hiçbir AI servisi transcript üretemedi");
                return new List<(TimeSpan Offset, string Text)>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Video {videoId} AI transcript hatası: {ex.Message}");
                return new List<(TimeSpan Offset, string Text)>();
            }
        }

        /// <summary>
        /// Whisper ile transcript çeker
        /// </summary>
        private async Task<List<(TimeSpan Offset, string Text)>> ExtractWithWhisper(string videoUrl, string videoId)
        {
            try
            {
                _logger.LogInformation($"Video {videoId} için Whisper analizi başlatılıyor");

                // 1. Ses dosyasını çıkar
                var audioPath = await ExtractAudioFromVideo(videoUrl, videoId);
                if (string.IsNullOrEmpty(audioPath) || !File.Exists(audioPath))
                {
                    _logger.LogWarning($"Video {videoId} için ses dosyası çıkarılamadı");
                    return new List<(TimeSpan Offset, string Text)>();
                }

                // 2. Whisper ile transkript oluştur
                var transcriptPath = Path.Combine(_tempDirectory, $"{videoId}_whisper.json");
                var whisperCommand = $"whisper \"{audioPath}\" --model medium --language Turkish --output_format json --output_dir \"{_tempDirectory}\" --verbose False";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "whisper",
                    Arguments = $"\"{audioPath}\" --model medium --language Turkish --output_format json --output_dir \"{_tempDirectory}\" --verbose False",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        // JSON dosyasını oku ve parse et
                        var jsonFiles = Directory.GetFiles(_tempDirectory, $"*{videoId}*.json");
                        if (jsonFiles.Any())
                        {
                            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
                            var result = ParseWhisperJson(jsonContent);
                            
                            // Geçici dosyaları temizle - DÜZELTME: string array yerine params kullan
                            var filesToClean = new List<string> { audioPath };
                            filesToClean.AddRange(jsonFiles);
                            CleanupTempFiles(filesToClean.ToArray());
                            
                            return result;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Whisper hatası: {error}");
                    }
                }

                return new List<(TimeSpan Offset, string Text)>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Whisper transcript hatası: {ex.Message}");
                return new List<(TimeSpan Offset, string Text)>();
            }
        }

        /// <summary>
        /// Google Cloud Speech-to-Text ile transcript çeker
        /// </summary>
        private async Task<List<(TimeSpan Offset, string Text)>> ExtractWithGoogleCloud(string videoUrl, string videoId)
        {
            if (_speechClient == null)
            {
                _logger.LogWarning("Google Cloud Speech client mevcut değil");
                return new List<(TimeSpan Offset, string Text)>();
            }

            try
            {
                _logger.LogInformation($"Video {videoId} için Google Cloud Speech analizi başlatılıyor");

                // 1. Ses dosyasını çıkar (WAV formatında)
                var audioPath = await ExtractAudioFromVideo(videoUrl, videoId, "wav");
                if (string.IsNullOrEmpty(audioPath) || !File.Exists(audioPath))
                {
                    return new List<(TimeSpan Offset, string Text)>();
                }

                // 2. Google Cloud Speech konfigürasyonu
                var config = new RecognitionConfig
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    SampleRateHertz = 16000,
                    LanguageCode = "tr-TR",
                    EnableWordTimeOffsets = true,
                    EnableAutomaticPunctuation = true,
                    Model = "latest_long",
                    UseEnhanced = true
                };

                var audio = RecognitionAudio.FromFile(audioPath);
                
                // 3. Uzun ses dosyaları için LongRunningRecognize kullan - DÜZELTME: Result property'si
                var operation = await _speechClient.LongRunningRecognizeAsync(config, audio);
                var response = await operation.PollUntilCompletedAsync();

                var segments = new List<(TimeSpan Offset, string Text)>();

                // DÜZELTME: response.Result.Results kullan
                foreach (var result in response.Result.Results)
                {
                    foreach (var alternative in result.Alternatives)
                    {
                        if (alternative.Words.Any())
                        {
                            // Kelime bazında zaman bilgisi
                            foreach (var word in alternative.Words)
                            {
                                var offset = TimeSpan.FromSeconds(word.StartTime.Seconds) +
                                           TimeSpan.FromMilliseconds(word.StartTime.Nanos / 1000000);
                                segments.Add((offset, word.Word));
                            }
                        }
                        else
                        {
                            // Sadece metin varsa
                            segments.Add((TimeSpan.Zero, alternative.Transcript));
                        }
                    }
                }

                // Geçici dosyayı temizle
                CleanupTempFiles(audioPath);

                return segments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Google Cloud Speech transcript hatası: {ex.Message}");
                return new List<(TimeSpan Offset, string Text)>();
            }
        }

        /// <summary>
        /// YouTube videosundan ses çıkarır
        /// </summary>
        private async Task<string> ExtractAudioFromVideo(string videoUrl, string videoId, string format = "mp3")
        {
            try
            {
                var outputPath = Path.Combine(_tempDirectory, $"{videoId}.{format}");
                
                // yt-dlp ile ses çıkarma
                var arguments = format == "wav" 
                    ? $"-x --audio-format wav --audio-quality 0 --postprocessor-args \"-ar 16000\" -o \"{outputPath}\" \"{videoUrl}\""
                    : $"-x --audio-format mp3 --audio-quality 0 -o \"{outputPath}\" \"{videoUrl}\"";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0 && File.Exists(outputPath))
                    {
                        _logger.LogInformation($"Ses dosyası başarıyla çıkarıldı: {outputPath}");
                        return outputPath;
                    }
                    else
                    {
                        _logger.LogWarning($"yt-dlp hatası: {error}");
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ses çıkarma hatası: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Whisper JSON çıktısını parse eder
        /// </summary>
        private List<(TimeSpan Offset, string Text)> ParseWhisperJson(string jsonContent)
        {
            var segments = new List<(TimeSpan Offset, string Text)>();

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                if (document.RootElement.TryGetProperty("segments", out var segmentsElement))
                {
                    foreach (var segment in segmentsElement.EnumerateArray())
                    {
                        if (segment.TryGetProperty("start", out var startElement) &&
                            segment.TryGetProperty("text", out var textElement))
                        {
                            var start = startElement.GetDouble();
                            var text = textElement.GetString()?.Trim();

                            if (!string.IsNullOrEmpty(text))
                            {
                                segments.Add((TimeSpan.FromSeconds(start), text));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Whisper JSON parse hatası");
            }

            return segments;
        }

        /// <summary>
        /// Geçici dosyaları temizler
        /// </summary>
        private void CleanupTempFiles(params string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Geçici dosya silinemedi: {filePath}");
                }
            }
        }

        /// <summary>
        /// Transcript'i analiz ederek maçla ilgili olup olmadığını belirler
        /// </summary>
        public async Task<MatchRelevanceResult> AnalyzeTranscriptForMatch(string transcript, string matchInfo)
        {
            try
            {
                _logger.LogInformation("Transcript analizi başlatılıyor: {MatchInfo}", matchInfo);
                
                // Basit kural tabanlı analiz (gelecekte AI ile geliştirilebilir)
                var result = new MatchRelevanceResult();
                
                if (string.IsNullOrEmpty(transcript) || string.IsNullOrEmpty(matchInfo))
                {
                    return result;
                }
                
                var transcriptLower = transcript.ToLowerInvariant();
                var matchInfoLower = matchInfo.ToLowerInvariant();
                
                // Takım adlarını parse et
                var teams = ParseTeamNames(matchInfoLower);
                result.DetectedTeams = teams.Where(team => transcriptLower.Contains(team)).ToList();
                
                // Pozisyon kelimelerini tespit et
                var positionKeywords = new[] { "penaltı", "kırmızı kart", "sarı kart", "ofsayt", "var", "faul" };
                result.DetectedPositions = positionKeywords.Where(pos => transcriptLower.Contains(pos)).ToList();
                
                // Güven skoru hesapla
                double teamScore = result.DetectedTeams.Count > 0 ? 0.5 : 0.0;
                double positionScore = result.DetectedPositions.Count > 0 ? 0.3 : 0.0;
                double contextScore = ContainsMatchContext(transcriptLower) ? 0.2 : 0.0;
                
                result.ConfidenceScore = teamScore + positionScore + contextScore;
                result.IsRelevantToMatch = result.ConfidenceScore > 0.4;
                
                _logger.LogInformation("Transcript analizi tamamlandı. Güven skoru: {Score}, İlgili: {IsRelevant}", 
                    result.ConfidenceScore, result.IsRelevantToMatch);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transcript analizi hatası: {MatchInfo}", matchInfo);
                return new MatchRelevanceResult();
            }
        }
        
        private List<string> ParseTeamNames(string matchInfo)
        {
            var teams = new List<string>();
            
            // Farklı ayırıcıları dene
            var separators = new[] { " vs ", " - ", "-", "–", "—" };
            
            foreach (var separator in separators)
            {
                if (matchInfo.Contains(separator))
                {
                    var parts = matchInfo.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        teams.Add(parts[0].Trim());
                        teams.Add(parts[1].Trim());
                        break;
                    }
                }
            }
            
            return teams;
        }
        
        private bool ContainsMatchContext(string transcript)
        {
            var contextKeywords = new[] { "maç", "müsabaka", "karşılaşma", "hakem", "dakika" };
            return contextKeywords.Any(keyword => transcript.Contains(keyword));
        }

        // DÜZELTME: IDisposable interface'ni implement et
        public void Dispose()
        {
            // SpeechClient'ın Dispose metodu yok, sadece null check yap
            // Google Cloud client'ları genellikle IDisposable implement etmez
        }
    }
}