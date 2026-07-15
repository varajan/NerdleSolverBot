using System.Text;
using System.Text.RegularExpressions;

namespace NerdleSolverApp.Extensions;

public static class StringExtensions
{
    public static string OnlyUnique(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var seen = new HashSet<char>();
        var result = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (seen.Add(c))
                result.Append(c);
        }

        return result.ToString();
    }

    public static bool IsValidRegex(this string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            _ = new Regex(pattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
