using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anlasalamiyoruz.Application.Common.Interfaces;
using Anlasalamiyoruz.Application.Common.Models.AI;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Anlasalamiyoruz.Infrastructure.Services;

public class AiAnalysisService : IAiAnalysisService
{
    private readonly GoogleAI? _googleAI;
    private readonly ILogger<AiAnalysisService> _logger;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly bool _isConfigured;

    private const string ModelName = "gemini-2.0-flash";
    private const int MaxOutputTokens = 4096;
    private const float Temperature = 0.4f;
    private const int AiTimeoutSeconds = 25;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AiAnalysisService(ILogger<AiAnalysisService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var apiKey = configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Configuration missing: 'Gemini:ApiKey' is not set. " +
                               "All AI calls will return fallback responses.");
            _isConfigured = false;
        }
        else
        {
            _googleAI = new GoogleAI(apiKey: apiKey);
            _isConfigured = true;
        }

        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<GeminiApiException>()
                    .Handle<GeminiApiTimeoutException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Gemini API call failed. Retry attempt {AttemptNumber} of 2.",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<List<ClarifyingQuestionItem>> GenerateClarifyingQuestionsAsync(
        ClarifyingQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("AI service is not configured. Returning fallback questions for topic: {Topic}", request.Topic);
            return GenerateFallbackQuestions(request.Topic);
        }

        _logger.LogInformation(
            "Generating clarifying questions via Gemini for topic: {Topic}", request.Topic);

        const string systemPrompt = """
            Sen tarafsız bir arabulucusun. Çok kısa ve öz yanıt ver, gereksiz açıklama yapma, doğrudan JSON objesini döndür.

            KURALLAR:
            - Sadece geçerli JSON döndür. Markdown, açıklama veya ``` ekleme.
            - 2-3 soru üret: sol için en az 1, sağ için en az 1.
            - Sorular kısa ve açık uçlu olmalı.
            - targetSide yalnızca "Left", "Right" veya "Both" olabilir.

            FORMAT: {"questions":[{"questionText":"...","targetSide":"Left"},{"questionText":"...","targetSide":"Right"}]}
            """;

        var userPrompt = $"""
            Anlaşmazlık Konusu: {request.Topic}

            Sol Tarafın Görüşü:
            {request.LeftViewText}

            Sağ Tarafın Görüşü:
            {request.RightViewText}

            Her iki tarafın bağlamını derinleştirmek için netleştirici sorular üret.
            """;

        try
        {
            var rawJson = await ExecuteWithRetryAsync(
                ct => SendMessageAsync(systemPrompt, userPrompt, ct),
                cancellationToken);

            var response = DeserializeOrThrow<ClarifyingQuestionsResponse>(rawJson);

            _logger.LogInformation(
                "Generated {Count} clarifying questions.", response.Questions.Count);

            return response.Questions;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex,
                "Gemini AI unavailable for clarifying questions. Using fallback questions for topic: {Topic}",
                request.Topic);

            return GenerateFallbackQuestions(request.Topic);
        }
    }

    public async Task<DeepAnalysisResult> PerformDeepAnalysisAsync(
        DeepAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("AI service is not configured. Returning fallback analysis for topic: {Topic}", request.Topic);
            return GenerateFallbackAnalysis(request);
        }

        _logger.LogInformation(
            "Performing Gemini deep analysis for topic: {Topic}", request.Topic);

        const string systemPrompt = """
            Sen tarafsız bir anlaşmazlık analistisin. Çok kısa ve öz yanıt ver, gereksiz açıklama yapma, doğrudan JSON objesini döndür.

            KURALLAR:
            - Sadece geçerli JSON döndür. Markdown, açıklama veya ``` ekleme.
            - Tüm metin değerleri Türkçe olmalı.
            - emotions[].side: "left" veya "right". intensity: 1-5 tam sayı.
            - scores: leftScore ve rightScore 1-5 tam sayı (5=çok haklı).
            - leftPoints: Sol tarafa özgü argümanlar (3-4 kısa madde, sağla paylaşılan nokta olmamalı).
            - rightPoints: Sağa özgü argümanlar (3-4 kısa madde, solle paylaşılan nokta olmamalı).
            - bothPoints: Her ikisinin gerçek ortak zemini (en az 2 madde).
            - verdict: 2 cümle tarafsız değerlendirme.
            - suggestion: 2 cümle somut öneri.
            - actionSteps: 3-4 madde somut adım.

            FORMAT:
            {"emotions":[{"side":"left","emotion":"...","intensity":3},{"side":"right","emotion":"...","intensity":4}],"scores":{"leftScore":3,"rightScore":4,"description":"..."},"leftPoints":["..."],"rightPoints":["..."],"bothPoints":["..."],"verdict":"...","suggestion":"...","actionSteps":["..."]}
            """;

        var answersSection = BuildAnswersSection(request);

        var userPrompt = $"""
            Anlaşmazlık Konusu: {request.Topic}

            Sol Tarafın Ana Görüşü:
            {request.LeftViewText}

            Sağ Tarafın Ana Görüşü:
            {request.RightViewText}

            {answersSection}
            Bu anlaşmazlığı arabulucu gözüyle analiz et ve JSON formatında kapsamlı bir rapor sun.
            """;

        try
        {
            var rawJson = await ExecuteWithRetryAsync(
                ct => SendMessageAsync(systemPrompt, userPrompt, ct),
                cancellationToken);

            var result = DeserializeOrThrow<DeepAnalysisResult>(rawJson);

            _logger.LogInformation(
                "Deep analysis complete. Left score: {LeftScore}/5, Right score: {RightScore}/5.",
                result.Scores.LeftScore, result.Scores.RightScore);

            return result;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex,
                "Gemini AI unavailable for deep analysis. Using fallback analysis for topic: {Topic}",
                request.Topic);

            return GenerateFallbackAnalysis(request);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Fallback generators (used when AI is unavailable)
    // ──────────────────────────────────────────────────────────────────────────

    private static List<ClarifyingQuestionItem> GenerateFallbackQuestions(string topic)
    {
        return
        [
            new ClarifyingQuestionItem
            {
                QuestionText = $"\"{topic}\" konusundaki en temel endişeniz nedir? Sizi bu görüşe iten ana neden neydi?",
                TargetSide = "Left"
            },
            new ClarifyingQuestionItem
            {
                QuestionText = "Karşı tarafın hangi noktasını en az anlayışla karşılıyorsunuz ve neden?",
                TargetSide = "Left"
            },
            new ClarifyingQuestionItem
            {
                QuestionText = $"\"{topic}\" konusundaki en temel endişeniz nedir? Sizi bu görüşe iten ana neden neydi?",
                TargetSide = "Right"
            },
            new ClarifyingQuestionItem
            {
                QuestionText = "Ortak bir zemin bulunabilmesi için karşı tarafın hangi adımı atmasını beklersiniz?",
                TargetSide = "Right"
            }
        ];
    }

    private static DeepAnalysisResult GenerateFallbackAnalysis(DeepAnalysisRequest request)
    {
        return new DeepAnalysisResult
        {
            Emotions =
            [
                new EmotionItem { Side = "left", Emotion = "Kararlılık", Intensity = 3 },
                new EmotionItem { Side = "right", Emotion = "Kararlılık", Intensity = 3 }
            ],
            Scores = new ScoreItem
            {
                LeftScore = 3,
                RightScore = 3,
                Description = "Her iki taraf da konuya ilişkin güçlü bir bakış açısına sahip. Tam analiz için AI servisine tekrar erişim gereklidir."
            },
            LeftPoints =
            [
                "Sol taraf kendi bakış açısından tutarlı argümanlar sunmuştur.",
                "Görüşler detaylı ve düşünülmüş niteliktedir.",
                "Bu perspektif, konunun önemli bir boyutunu kapsamaktadır."
            ],
            RightPoints =
            [
                "Sağ taraf alternatif bir çerçeveden meşru argümanlar öne sürmüştür.",
                "Bu görüş, konunun farklı ama geçerli bir boyutunu temsil etmektedir.",
                "Perspektif, göz ardı edilmemesi gereken önemli noktalar içermektedir."
            ],
            BothPoints =
            [
                "Her iki taraf da konunun çözüme kavuşturulmasını istemektedir.",
                "Taraflar, birbirlerinin görüşlerine saygı göstererek iletişim kurmuşlardır.",
                $"\"{request.Topic}\" konusunun her iki taraf için de önemli olduğu açıktır."
            ],
            Verdict = "Her iki taraf da güçlü argümanlara sahip olmakla birlikte, bakış açıları birbirini tamamlayıcı nitelikte olabilir. Tam bir AI analizi için lütfen tekrar deneyin.",
            Suggestion = "Açık bir iletişim ortamında her iki tarafın da önceliklerini ve endişelerini paylaşması, ortak bir çözüm zemini oluşturulmasına yardımcı olacaktır.",
            ActionSteps =
            [
                "Her iki taraf birbirinin görüşünü yargılamadan dinlemeyi taahhüt etsin.",
                "Ortak hedeflerin neler olduğunu birlikte belirleyin.",
                "Anlaşmazlık noktalarını öncelik sırasına göre listeleyin.",
                "En az tartışmalı konudan başlayarak adım adım ilerleyin."
            ]
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string> SendMessageAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var generationConfig = new GenerationConfig
        {
            ResponseMimeType = "application/json",
            MaxOutputTokens = MaxOutputTokens,
            Temperature = Temperature
        };

        var systemInstruction = new Content
        {
            Parts = [new TextData { Text = systemPrompt }]
        };

        using var model = _googleAI!.GenerativeModel(
            model: ModelName,
            generationConfig: generationConfig,
            systemInstruction: systemInstruction);

        model.Timeout = TimeSpan.FromSeconds(AiTimeoutSeconds);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AiTimeoutSeconds));
        var requestOptions = new RequestOptions { Timeout = TimeSpan.FromSeconds(AiTimeoutSeconds) };

        _logger.LogInformation("Gemini request sent. Model: {Model}", ModelName);
        var sw = Stopwatch.StartNew();

        var response = await model.GenerateContent(
            userPrompt,
            requestOptions: requestOptions,
            cancellationToken: cts.Token);

        sw.Stop();
        _logger.LogInformation(
            "Gemini response received in {ElapsedMs}ms. Prompt tokens: {Prompt}, Candidate tokens: {Candidates}.",
            sw.ElapsedMilliseconds,
            response.UsageMetadata?.PromptTokenCount,
            response.UsageMetadata?.CandidatesTokenCount);

        var text = response.Text
            ?? throw new InvalidOperationException(
                "Gemini returned an empty response.");

        return StripMarkdownCodeFence(text);
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        return await _retryPipeline.ExecuteAsync(
            async ct => await action(ct),
            cancellationToken);
    }

    private static string StripMarkdownCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```"))
            return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline >= 0)
            trimmed = trimmed[(firstNewline + 1)..];

        if (trimmed.EndsWith("```"))
            trimmed = trimmed[..^3];

        return trimmed.Trim();
    }

    private static T DeserializeOrThrow<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Gemini response deserialized to null for type {typeof(T).Name}.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize Gemini response to {typeof(T).Name}. " +
                $"Raw JSON snippet: {json[..Math.Min(json.Length, 500)]}", ex);
        }
    }

    private static string BuildAnswersSection(DeepAnalysisRequest request)
    {
        if (request.LeftAnswers.Count == 0 && request.RightAnswers.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        if (request.LeftAnswers.Count > 0)
        {
            sb.AppendLine("Sol Tarafın Netleştirici Soru Cevapları:");
            foreach (var item in request.LeftAnswers)
                sb.AppendLine($"  Soru: {item.Question}\n  Cevap: {item.Answer}");
            sb.AppendLine();
        }

        if (request.RightAnswers.Count > 0)
        {
            sb.AppendLine("Sağ Tarafın Netleştirici Soru Cevapları:");
            foreach (var item in request.RightAnswers)
                sb.AppendLine($"  Soru: {item.Question}\n  Cevap: {item.Answer}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Internal deserialization wrapper
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class ClarifyingQuestionsResponse
    {
        [JsonPropertyName("questions")]
        public List<ClarifyingQuestionItem> Questions { get; set; } = new();
    }
}
