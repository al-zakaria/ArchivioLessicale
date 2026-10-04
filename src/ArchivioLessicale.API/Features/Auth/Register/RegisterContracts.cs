public record RegisterUserCommand(
    string Email,
    string Password,
    string NickName,
    string DisplayName,
    string FirstName, 
    string LastName, 
    UserGrade Grade,
    int NumberOfWordsStudied,
    int NumberOfWordsLearned,
    ClientMetaData ClientMetaData
);

public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string NickName,
    string DisplayName
);