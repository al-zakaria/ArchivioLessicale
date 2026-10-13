using Microsoft.AspNetCore.WebUtilities;

namespace ArchivioLessicale.API.Common.Links;

public class LinkService(LinksOptions linkOptions)
{
    private readonly string _baseAddress = linkOptions.BaseAddress;
    
    public string GenerateLink<TLink>(TLink descriptor) where TLink : ILinkDescriptor
    {
        var url = $"{_baseAddress}/{descriptor.Path.TrimStart('/')}";

        var routeValues = new RouteValueDictionary(descriptor);

        routeValues.Remove(nameof(descriptor.Path));

        if (routeValues.Count == 0)
            return url;

        var queryParams = routeValues.ToDictionary(
            k => k.Key,
            v => v.Value?.ToString()
        );
        
        return QueryHelpers.AddQueryString(url, queryParams);
    }
}