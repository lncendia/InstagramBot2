﻿using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class HelpCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        user.CurrentWorks.ForEach(_ => user.Works.Remove(_));
        user.CurrentWorks.Clear();
        user.EnterData = null;
        user.State = State.main;
        await client.SendTextMessageAsync(message.Chat.Id,
            $"За поддержкой вы можете обратиться к {BotSettings.Cfg.NameSupport}.");
    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && message.Text == "🤝 Поддержка" && user.State != State.block;
    }
}