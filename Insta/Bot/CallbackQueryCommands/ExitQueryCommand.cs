﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Insta.Interfaces;
using Insta.Working;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class ExitQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        if (query.Data.StartsWith("exit"))
        {
            var instagram = user.Instagrams.FirstOrDefault(_ => _.Id == int.Parse(query.Data[5..]));
            if (instagram == null)
            {
                await client.AnswerCallbackQueryAsync(query.Id, "Инстаграм не найден.");
                await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            }
            else if (instagram.Block > DateTime.Now)
            {
                await client.AnswerCallbackQueryAsync(query.Id,
                    $"Вы сможете выйти из этого аккаунта через {(instagram.Block - DateTime.Now):g}.", true);
            }
            else
            {
                var message = (await Operation.LogOutAsync(user, instagram)) ? "Успешно." : "Ошибка.";
                await client.AnswerCallbackQueryAsync(query.Id, message);
            }
        }
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data.StartsWith("exit");
    }
}