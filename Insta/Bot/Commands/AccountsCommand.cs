using System;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class AccountsCommand : ITextCommand
{
    private readonly Random _rnd = new();
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        foreach (var x in user.Instagrams)
        {
            await client.SendTextMessageAsync(message.Chat.Id,
                $"{Keyboards.Emodji[_rnd.Next(0, Keyboards.Emodji.Length)]} Аккаунт {x.Username}",
                replyMarkup: Keyboards.Exit(x.Id));
        }

        if (user.Instagrams.Count < user.Subscribes.Count)
            await client.SendTextMessageAsync(message.Chat.Id,
                "Вы можете добавить аккаунт",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("➕ Добавить аккаунт", "enterData")));
        else
        {
            await client.SendTextMessageAsync(message.Chat.Id,
                "Оплатите подписку, чтобы добавить аккаунт.", replyMarkup: Keyboards.MainKeyboard);
        }

    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && message.Text == "🌇 Мои аккаунты" && user.State == State.main;
    }
}