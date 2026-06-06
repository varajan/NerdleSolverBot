using NerdleSolver;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

var fileStream = File.OpenRead("p1.jpg");
var parser = new ImageParser(fileStream);
var parse = parser.Parse();
return;

var stage = Stage.ShowAll;
var botToken = "...:...";
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
    if (update.Message?.Text == null)
        return;

    var chatId = update.Message.Chat.Id;
    var text = update.Message.Text.Trim().ToLower();

    if (update.Message.Photo != null)
    {
        var photo = update.Message.Photo.Last();
        var file = await botClient.GetFile(photo.FileId);

        using var stream = new MemoryStream();

        await botClient.DownloadFile(file.FilePath!, stream);
        stream.Position = 0;

        var parser = new ImageParser(stream);
        var (expected, unexpected, pattern) = parser.Parse();
        nerdle.Expected = expected;
        nerdle.Unexpected = unexpected;
        nerdle.Pattern = pattern;

        await SendMessage($"Must: {nerdle.Expected}\r\nForbidden: {nerdle.Unexpected}\r\nPattern: {nerdle.Pattern}");
        return;
    }

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
            stage = Stage.ShowAll;
            var results = nerdle.Solve().ToList();

            await SendMessage($"Found {results.Count} solution(s).");
            if (results.Count < 35)
            {
                await SendMessage(string.Join(", ", results));
            }
            else
            {
                await SendAsFile(results);
            }
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

    async Task SendMessage(string message)
    {
        await botClient.SendMessage(
            chatId,
            message,
            cancellationToken: cancellationToken);
    }

    // --- local functions ---

    async Task SendAsFile(List<string> lines)
    {
        var bytes = Encoding.UTF8.GetBytes(string.Join("\n", lines));
        using (var stream = new MemoryStream(bytes))
        {
            await botClient.SendDocument(
               chatId,
               document: new InputFileStream(stream, "solution.txt"),
               caption: $"{lines.Count} Solutions",
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