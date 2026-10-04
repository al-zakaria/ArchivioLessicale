using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt {get;set;} = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt {get;set;} = default;

    public static ApplicationUser Create(string email)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
        };
    }

    private ApplicationUser() { }
}

public class Profile
{
    public Guid Id {get; set;}
    public string NickName {get;set;} = string.Empty;
    public string DisplayName {get;set;} = string.Empty;
    public string FirstName {get;set; } =string.Empty;
    public string LastName {get;set;} = string.Empty;
    public UserGrade Grade {get;set;} = UserGrade.A1;
    public int NumberOfWordsStudied {get;set;} = 0;
    public int NumberOfWordsLearned {get;set;} = 0;

    public static Profile Create(
        Guid id,
        string nickName, 
        string displayName,
        string firstName,
        string lastName,
        UserGrade grade,
        int numberOfWordsStudied,
        int numberOfWordsLearned
    )
    {
        return new Profile
        {
            Id = id,
            NickName = nickName,
            DisplayName = displayName,
            FirstName = firstName,
            LastName = lastName,
            Grade = grade,
            NumberOfWordsStudied = numberOfWordsStudied,  
            NumberOfWordsLearned = numberOfWordsLearned
        };
    }

    private Profile() { }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserGrade
{
    A1,
    A2,
    B1,
    B2,
    C1,
    C2
}