using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Io;
using AngleSharp.Io.Network;
using ArchivioLessicale.API.Models;
using ArchivioLessicale.API.Services.Interfaces;
using CSharpFunctionalExtensions;
using HttpMethod = System.Net.Http.HttpMethod;

namespace ArchivioLessicale.API.Services.Implementations;

public partial class ArticleParserService(HttpClient client, ILogger<ArticleParserService> logger) 
    : IArticleParserService
{
    private static readonly Regex AnsaUrlRegex = AnsaRegex();
    
    public async Task<Result<Article>> ParseAsync(string articleUrl)
    {
        var validationResult = await CanParseAsync(articleUrl);

        if (!validationResult.IsSuccess)
            return Result.Failure<Article>($"This link isn't valid. Message: {validationResult.Error}");
        
        var config = Configuration.Default
            .With(new HttpClientRequester(client))
            .WithDefaultLoader(new LoaderOptions
            {
                IsResourceLoadingEnabled = false
            });
        
        var context = BrowsingContext.New(config);

        try
        {
            using var document = await context.OpenAsync(articleUrl);
            
            var titleElement = document.QuerySelector("h1.post-single-title");
            var title = titleElement?.TextContent.Trim() ?? "Il titolo non è stato trovato";
            
            var subtitleElement = document.QuerySelector("div.summary");
            var subtitle = subtitleElement?.TextContent.Trim() ?? "Il subtitle non è stato trovato";
            
            var paragraphs = document.QuerySelectorAll("div.post-single-text.rich-text.news-txt p")
                .Select(paragraph => paragraph.TextContent.Trim())
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                .ToList();
            
            var rawText = string.Join(Environment.NewLine, paragraphs);

            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = title,
                Subtitle = subtitle,
                RawText = rawText,
                SourceUrl = articleUrl,
                CreatedAt = DateTime.UtcNow
            };
            
            logger.LogInformation("Parsed {ArticleUrl} and made article object", articleUrl);
            
            return article;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Si è verificato un errore durante il parsing dell'articolo dall'URL: " +
                                "{ArticleUrl}", articleUrl);

            return Result.Failure<Article>($"Errore di connessione o parsing con il sito: {ex.Message}");
        }
    }

    public async Task<Result<bool>> CanParseAsync(string articleUrl)
    {
        if (string.IsNullOrWhiteSpace(articleUrl))
        {
            logger.LogWarning("The URL({ArticleUrl}) is empty or null", articleUrl);
            return false;
        }

        if (!Uri.TryCreate(articleUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogWarning("Invalid URI({ArticleUrl}) format or non-protocol-compliant scheme", articleUrl);
            return false;
        }
        
        if (!AnsaUrlRegex.IsMatch(articleUrl))
        {
            logger.LogWarning("URL {ArticleUrl} does not conform to the article template of ANSA", articleUrl);
            return false;
        }

        try
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            using var response = await client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);

            if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
                return await CheckWithGetFallbackAsync(uri, cancellationTokenSource.Token);

            if (!response.IsSuccessStatusCode)
                return false;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                return false;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("The timeout for checking the URL's availability has been exceeded: {articleUrl}", 
                articleUrl);
            return Result.Failure<bool>($"The timeout for checking the URL's availability has been exceeded: " +
                                        $"{articleUrl}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Network error during link validation: {articleUrl}. Message: {Message}", 
                articleUrl, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning("There was an unexpected error during validation link: {articleUrl}. Message: {Message}", 
                articleUrl,  ex.Message);
            return false;
        }
        
        return true;
    }

    private async Task<Result<bool>> CheckWithGetFallbackAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            using var response = await client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result.Failure<bool>(response.ReasonPhrase);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Fallback request failed during link validation: {uri}. Message: {Message}",
                uri, ex.Message);
            return false;
        }
    }
    
    [GeneratedRegex(@"^https?:\/\/(www\.)?ansa\.it\/sito\/notizie\/[a-zA-Z0-9_-]+\/\d{4}\/\d{2}\/\d{2}\/.+_[a-f0-9-]+\.html$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "it-RU")]
    private static partial Regex AnsaRegex();
}