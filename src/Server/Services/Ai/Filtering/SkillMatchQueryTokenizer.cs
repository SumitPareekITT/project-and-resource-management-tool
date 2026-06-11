namespace ProjectResourceManagement.Server.Services.Ai.Filtering;

public static class SkillMatchQueryTokenizer
{
    public static IReadOnlyList<string> Tokenize(string query)
    {
        return query
            .Split([' ', ',', '.', ';', ':', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToLowerInvariant())
            .Where(token => token.Length >= 2)
            .Distinct()
            .ToList();
    }
}
