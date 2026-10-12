namespace ArchivioLessicale.API.Common.Links;

public interface ILinkDescriptor
{
    string Path { get; }
}

public record EmailConfirmationLink(Guid UserId, string EmailConfirmationToken) : ILinkDescriptor
{
    public string Path => "auth/confirm-email";
}