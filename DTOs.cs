namespace ProfiApi;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(
    string Email, string Password, string Phone,
    string LastName, string Firstname,string? MiddleName);

public record RefreshRequest(string RefreshToken);
public record AuthResponce(string AccessToken, string RefreshToken, string Role);
public record UpdateProfileRequest(
    string LastName,
    string FirstName,
    string? MiddleName,
    string? Phone
);

public record EducationRequest(
    int? EduInstitutionId,
    int? EduTypeId,
    DateOnly DateStart,
    DateOnly? DateEnd
);

public record ExperoienceRequest(
    int? CompanyId, int? Position, string? Description, int? EmpTypeId,
    DateOnly DateStart, DateOnly? DateEnd
);

public record ExportRequest(string Format, int Expiry, bool Anonymous);
public record SkillRequest(int TechnologyId, short SkillLevel);
public record ConfirmationRequest(int SkillId, int TargetUserId, string? Message);

public record ShortlistRequest(string Name, string? Description);
public record ShortlistAddCandidateRequest(int UserId, string? Note);
public record PinSetupRequest(string Pin);
public record PinLoginRequest(int UserId, string Pin);

public record FileRequest(string format, int date, bool anonim);


