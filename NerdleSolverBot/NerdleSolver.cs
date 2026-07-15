using NerdleSolverApp;
using NerdleSolverApp.Data;
using NerdleSolverApp.Extensions;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

var stage = Stage.ShowAll;
var botToken = File.ReadAllText("botId.txt");
var nerdle = new Nerdle();
var botClient = new TelegramBotClient(botToken);
var receiverOptions = new ReceiverOptions { AllowedUpdates = [] };

using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"Bot started: @{me.Username}");
Console.ReadLine();
cts.Cancel();

async Task HandleUpdateAsync(
    ITelegramBotClient botClient,
    Update update,
    CancellationToken cancellationToken)
{
    var chatId = update.Message?.Chat.Id;

    if (update.Message?.Photo != null)
    {
        try
        {
            nerdle = await ParseImage();
            await SendMessage($"Must: {nerdle.Expected}\r\nForbidden: {nerdle.Unexpected}\r\nPattern: {nerdle.Pattern}");

            var keysToSolveMin = Constants.Symbols.Length - 1;
            if (nerdle.Expected.Length + nerdle.Unexpected.Length >= keysToSolveMin)
            {
                await SendMessage("Enough information collected. Searching for a solution...");
                await Calculate();
            }
        }
        catch (Exception ex)
        {
            await SendMessage($"Error parsing image: {ex.Message}");
            await SendAsFile("exception", ex.StackTrace?.Split("\r\n"));
        }
        return;
    }

    if (update.Message?.Text == null)
        return;

    var text = update.Message.Text.Trim().ToLower();

    switch (text)
    {
        case "/cancel":
            stage = Stage.ShowAll;
            await SendMessage($"Must: {nerdle.Expected}\r\nForbidden: {nerdle.Unexpected}\r\nPattern: {nerdle.Pattern}");
            return;

        case "/must":
            stage = Stage.Expected;
            await SendMessage($"Must: {nerdle.Expected}");
            return;

        case "/forbidden":
            stage = Stage.Unexpected;
            await SendMessage($"Forbidden: {nerdle.Unexpected}");
            return;

        case "/pattern":
            stage = Stage.Pattern;
            await SendMessage($"Pattern: {nerdle.Pattern}");
            return;

        case "/calculate":
            await Calculate();
            return;
    }

    switch (stage)
    {
        case Stage.Expected:
            var isValidExpected = await IsValidInput(text);
            if (isValidExpected)
            {
                nerdle.Expected = text.OnlyUnique();
            }
            break;

        case Stage.Unexpected:
            var isValidUnexpected = await IsValidInput(text);
            if (isValidUnexpected)
            {
                nerdle.Unexpected = text.OnlyUnique();
            }
            break;

        case Stage.Pattern:
            var isValidPattern = await IsValidPattern(text);
            if (isValidPattern)
            {
                nerdle.Pattern = text.OnlyUnique();
            }
            break;
    }

    // --- local functions ---

    async Task<bool> IsValidInput(string text)
    {
        var pattern = @"^[0-9+\-*/=]+$";
        return text.Length is > 0 and <= 20 && Regex.IsMatch(text, pattern);
    }

    async Task<bool> IsValidPattern(string text)
    {
        return text.Length is > 0 and <= 100 && text.IsValidRegex();
    }

    async Task Calculate()
    {
        stage = Stage.ShowAll;
        var results = nerdle.Solve().ToList();

        await SendMessage($"Found {results.Count} solution(s).");
        if (results.Count < 35)
        {
            await SendMessage(string.Join("\r\n", results));
        }
        else
        {
            await SendAsFile("solutions", results);
        }
    }

    // --- local functions ---

    async Task<Nerdle> ParseImage()
    {
        var photo = update.Message!.Photo!.Last();
        var file = await botClient.GetFile(photo.FileId);

        using var stream = new MemoryStream();
        await botClient.DownloadFile(file.FilePath!, stream);
        stream.Position = 0;

        var parser = new ImageParser(stream);
        var (expected, unexpected, pattern) = parser.Parse();

        return new Nerdle(expected, unexpected, pattern);
    }

    // --- local functions ---

    async Task SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        await botClient.SendMessage(
            chatId!,
            message,
            cancellationToken: cancellationToken);
    }

    // --- local functions ---

    async Task SendAsFile(string filename, IEnumerable<string>? lines)
    {
        if (lines is null) return;

        var bytes = Encoding.UTF8.GetBytes(string.Join("\n", lines));
        using (var stream = new MemoryStream(bytes))
        {
            await botClient.SendDocument(
               chatId!,
               document: new InputFileStream(stream, $"{filename}.txt"),
               caption: filename,
               cancellationToken: cancellationToken);
        }
    }

    // --- local functions ---
}

static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine(exception);
    return Task.CompletedTask;
}