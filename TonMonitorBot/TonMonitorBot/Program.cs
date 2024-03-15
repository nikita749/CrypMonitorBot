//author   :   @StarkGtsk nikita749
//version  :   v.1.0.0    





using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;


// Pre-assign menu text
const string EnteringMenu = "<b>Welcome to CrypMonitor bot.</b>\n\ntap to next to add/delete/see your wallets";
const string MainMenu = "<b>Choose the option</b>\n\n";
const string DeleteMenu = "<b>Delete wallet</b>\n\nEnter wallet address";
const string AddWalletMenu = "<b>Add wallet</b>\n\nEnter wallet address";

// Pre-assign button text
const string nextButton = "Next";
const string backButton = "Back";
const string addWalletButton = "Add wallet";
const string myWallets = "My wallets";
const string deleteWallet = "Delete wallet";

//Tutorial text
const string tutorial = "Welcome to CrypMonitor!\n/menu - to add or see your wallets";

// Build keyboards
InlineKeyboardMarkup EnteringMenuMarkup = new(InlineKeyboardButton.WithCallbackData(nextButton));
InlineKeyboardMarkup DeleteMenuMarkup = new(InlineKeyboardButton.WithCallbackData("Enter wallet address"));
InlineKeyboardMarkup MainMenuMarkup = new(
    new[] {
        new[] { InlineKeyboardButton.WithCallbackData(backButton) },
        new[] { InlineKeyboardButton.WithCallbackData(addWalletButton)},
        new[] { InlineKeyboardButton.WithCallbackData(myWallets)},
        new[] { InlineKeyboardButton.WithCallbackData(deleteWallet) }
    }
);

//parcing items
HttpClient client = new HttpClient();
List<string> wallets = new List<string>();
string url;
HashSet<string> seenTransactions = new HashSet<string>();



//bot logic: ==========================



var bot = new TelegramBotClient("7087719731:AAG60yS7CjQQs3IbEsWeksnAYEngwTqgWbE");

using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool, so we use cancellation token
bot.StartReceiving(
    updateHandler: HandleUpdate,
    pollingErrorHandler: HandleError,
    cancellationToken: cts.Token
);

// Tell the user the bot is online
Console.WriteLine("Start listening for updates. Press enter to stop");
Console.ReadLine();


//start pacing
await UpdateTransactions();


// Send cancellation request to stop the bot
cts.Cancel();

// Each time a user interacts with the bot, this method is called
async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
{
    switch (update.Type)
    {
        // A message was received
        case UpdateType.Message:
            await HandleMessage(update.Message!);
            break;

        // A button was pressed
        case UpdateType.CallbackQuery:
            await HandleButton(update.CallbackQuery!);
            break;
    }
}

async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
{
    await Console.Error.WriteLineAsync(exception.Message);
}

async Task HandleMessage(Message msg)
{
    var user = msg.From;
    var text = msg.Text ?? string.Empty;

    if (user is null)
        return;

    // Print to console
    Console.WriteLine($"{user.Username} wrote {text}");

    // When we get a command, we react accordingly
    if (text.StartsWith("/"))
    {
        await HandleCommand(user.Id, text);
    }
    else
    {   // This is equivalent to forwarding, without the sender's name
        await bot.CopyMessageAsync(user.Id, user.Id, msg.MessageId);
    }
}

async Task HandleWalletAddress(Message msg, bool flagDeleteorAdd) {

    var user = msg.From;
    var text = msg.Text ?? string.Empty;



    if (user is null) {
        return;
    }

    Console.WriteLine($"{user.Username} wrote {text}");

    string pattern = @"^(0|-1):([a-f0-9]{64}|[A-F0-9]{64})$";

    bool isMatch = Regex.IsMatch(text, pattern);

    if (flagDeleteorAdd)
    {

        if (isMatch)
        {
            //Add to database.
        }
        else
        {
            return;
        }
    }
    else {
        if (isMatch){
            //Delete from database.
        }
        else {
            return;
        }
    }
}



async Task HandleCommand(long userId, string command)
{
    switch (command)
    {
        case "/menu":
            await SendMenu(userId);
            break;
        case "/start":
            await bot.SendTextMessageAsync
                (
                userId,
                tutorial
                );
            break;
    }

    await Task.CompletedTask;
}

async Task SendMenu(long userId)
{
    await bot.SendTextMessageAsync(
        userId,
        EnteringMenu,
        parseMode: ParseMode.Html,
        replyMarkup: EnteringMenuMarkup
    );
}

async Task HandleButton(CallbackQuery query)
{
    string text = string.Empty;
    InlineKeyboardMarkup markup = new(Array.Empty<InlineKeyboardButton>());

    if (query.Data == nextButton)
    {
        text = MainMenu;
        markup = MainMenuMarkup;
    }
    else if (query.Data == backButton)
    {
        text = EnteringMenu;
        markup = EnteringMenuMarkup;
    }
    else if (query.Data == deleteWallet) {
        text = DeleteMenu;
        
    }
    else if (query.Data == addWalletButton) {
        text = AddWalletMenu;
        
    }

    // Close the query to end the client-side loading animation
    await bot.AnswerCallbackQueryAsync(query.Id);

    // Replace menu text and keyboard
    await bot.EditMessageTextAsync(
        query.Message!.Chat.Id,
        query.Message.MessageId,
        text,
        ParseMode.Html,
        replyMarkup: markup
    );
}


static async Task UpdateTransactions()
{
    while (true)
    {
        try
        {
            foreach (string i in wallets)
            {
                url = $"https://tonscan.org/address/{i}";
                string html = await client.GetStringAsync(url);
                ParseTransactions(html, i);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении или парсинге HTML: {ex.Message}");
        }

        
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}

static void ParseTransactions(string html, string wallet)
{
    HtmlDocument doc = new HtmlDocument();
    doc.LoadHtml(html);

    var transactionElements = doc.DocumentNode.SelectNodes("//div[@class='transaction']");

    if (transactionElements == null)
    {
        Console.WriteLine($"transactions from {wallet} not found");
        return;
    }

    foreach (var transactionElement in transactionElements)
    {
        string hash = transactionElement.SelectSingleNode(".//span[@class='hash']")?.InnerText.Trim();

        if (string.IsNullOrEmpty(hash) || seenTransactions.Contains(hash))
            continue;

        seenTransactions.Add(hash);

        string date = transactionElement.SelectSingleNode(".//span[@class='date']")?.InnerText.Trim();
        string amount = transactionElement.SelectSingleNode(".//span[@class='amount']")?.InnerText.Trim();

        Console.WriteLine($"Новая транзакция: Hash: {hash}, Date: {date}, Amount: {amount}");
    }
}
