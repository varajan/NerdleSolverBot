namespace NerdleSolverApp.Data;

public static class Constants
{
    public static readonly int NerdleLength = 8;
    public static readonly string[] Operations = { "+", "-", "*", "/", "=" };
    public static readonly string[] Keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
    public static readonly string[] Symbols = [.. Keys.Union(Operations)];
}
