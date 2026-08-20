using ArchivioLessicale.API.Models;
using CSharpFunctionalExtensions;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface IArticleParserService
{
    Task<Result<Article>> ParseAsync(string articleUrl);
    Task<Result<bool>> CanParseAsync(string articleUrl);
}