using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Insta.Enums;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Insta.Bot;

public class UpdateHandler : IUpdateHandler
{
    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var t = HandleAsync(botClient, update);
        return Task.CompletedTask;
    }

    private async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => MessageReceived(botClient, update.Message!),
            UpdateType.CallbackQuery => CallbackQueryReceived(botClient, update.CallbackQuery!),
            _ => Task.CompletedTask
        };
        await handler;
    }

    private static async Task CallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var user = BotSettings.Users.FirstOrDefault(x => x.Id == callbackQuery.From.Id);
        try
        {
            if (user == null) return;
            var command = BotSettings.CallbackQueryCommands.Find(_ => _.Compare(callbackQuery, user));
            if (command != null) await command.Execute(botClient, user, callbackQuery);
        }
        catch
        {
            Error(botClient, user);
        }
    }

    private async Task MessageReceived(ITelegramBotClient botClient, Message message)
    {
        var user = BotSettings.Users.FirstOrDefault(x => x.Id == message.From!.Id);
        try
        {
            var command = BotSettings.Commands.FirstOrDefault(_ => _.Compare(message, user));
            if (command != null) await command.Execute(botClient, user, message);
        }
        catch
        {
            Error(botClient, user);
        }
    }


    private static async void Error(ITelegramBotClient botClient, Model.User user)
    {
        if (user == null) return;
        user.CurrentWorks.ForEach(x => user.Works.Remove(x));
        user.EnterData = null;
        user.State = State.main;

        try
        {
            await botClient.SendTextMessageAsync(user.Id, "Произошла ошибка.", replyMarkup: Keyboards.MainKeyboard);
        }
        catch
        {
            // ignored
        }
    }
}