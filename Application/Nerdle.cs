using System.Data;
using System.Text.RegularExpressions;

namespace NerdleSolver.Application;

internal class Nerdle
{
    private const int Length = 8;
    private readonly string[] Operations = { "+", "-", "*", "/" };
    private readonly string[] Symbols = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+", "-", "*", "/", "=" };

    public string Expected { get; set; } = string.Empty;
    public string Unexpected { get; set; } = string.Empty;
    public string Pattern { get; set; } = ".*";

    private DataTable? _calculator;
    private DataTable Calculator => _calculator ??= new();

    private bool IsValidExpression(string expression)
    {
        try
        {
            var parts = expression.Split('=');
            if (parts.Length != 2) return false;

            if (!IsValidLeft(parts[0])) return false;
            if (!IsValidRight(parts[1])) return false;

            var left = Calculator.Compute(parts[0], null).ToString();
            var right = parts[1];

            return left == right;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidLeft(string expression)
    {
        if (Operations.All(x => !expression.Contains(x))) return false;

        var numbers = Regex.Split(expression, @"[+\-*/]");
        return !numbers.Any(x => x.Length == 0 || (x.Length > 1 && x.StartsWith("0")));
    }

    private bool IsValidRight(string expression)
    {
        if (Operations.Any(expression.Contains)) return false;
        return true;
    }

    public IEnumerable<string> Solve()
    {
        var candidates = GenerateCandidates();
        return candidates.Where(c => ContainsAllExpected(c) && MatchesPattern(c) && IsValidExpression(c));
    }

    private bool ContainsAllExpected(string candidate)
    {
        return Expected.All(candidate.Contains);
    }

    private bool MatchesPattern(string candidate)
    {
        return Regex.IsMatch(candidate, Pattern);
    }

    private IEnumerable<string> GenerateCandidates()
    {
        var symbols = Symbols.ToList().Where(s => !Unexpected.Contains(s)).ToArray();
        var totalCombinations = (int)Math.Pow(symbols.Length, Length);
        for (int i = 0; i < totalCombinations; i++)
        {
            var candidate = new char[Length];
            int temp = i;
            for (int j = 0; j < Length; j++)
            {
                candidate[j] = symbols[temp % symbols.Length][0];
                temp /= symbols.Length;
            }
            yield return new string(candidate);
        }
    }
}
