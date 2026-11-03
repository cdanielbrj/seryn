namespace Seryn.Server.Auth;

public static class Credentials
{
    public static bool Valid(string? username, string? password) =>
        !string.IsNullOrWhiteSpace(username) && username.Trim().Length <= 64 &&
        !username.Any(char.IsControl) && !string.IsNullOrEmpty(password) && password.Length <= 128;

    public static bool ValidInitial(string? username, string? password) =>
        Valid(username, password) && !string.IsNullOrWhiteSpace(password) && password.Length >= 16;
}
