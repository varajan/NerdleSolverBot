using NerdleSolver;
using System.Text;
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
            await ParseImage();
            await SendMessage($"Must: {nerdle.Expected}\r\nForbidden: {nerdle.Unexpected}\r\nPattern: {nerdle.Pattern}");
            //await Calculate();
            // todo: uncomment when pattern is not empty
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
            nerdle.Expected = text;
            break;

        case Stage.Unexpected:
            nerdle.Unexpected = text;
            break;

        case Stage.Pattern:
            nerdle.Pattern = text;
            break;
    }

    // --- local functions ---

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

    async Task ParseImage()
    {
        var photo = update.Message!.Photo!.Last();
        var file = await botClient.GetFile(photo.FileId);

        using var stream = new MemoryStream();
        await botClient.DownloadFile(file.FilePath!, stream);
        stream.Position = 0;

        var parser = new ImageParser(stream);
        var (expected, unexpected, pattern) = parser.Parse();
        nerdle.Expected = expected;
        nerdle.Unexpected = unexpected;
        nerdle.Pattern = pattern;
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