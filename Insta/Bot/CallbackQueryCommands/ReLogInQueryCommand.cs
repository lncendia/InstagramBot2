using System;
using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Model;
using Insta.Working;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class ReLogInQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        if (user.State != State.main)
        {
            await client.AnswerCallbackQueryAsync(query.Id, "Вы должны быть в главное меню.");
            return;
        }

        var inst = user.Instagrams.FirstOrDefault(_ => _.Id == int.Parse(query.Data[8..]));
        if (inst == null)
        {
            await client.AnswerCallbackQueryAsync(query.Id, "Инстаграм не найден.");
            return;
        }

        if (inst.IsDeactivated)
        {
            await client.AnswerCallbackQueryAsync(query.Id, "Инстаграм деактивирован.");
            return;
        }
        if (inst.Block > DateTime.Now)
        {
            await client.AnswerCallbackQueryAsync(query.Id,
                $"Вы сможете перезайти в этот аккаунт через {(inst.Block - DateTime.Now):g}.", true);
            return;
        }

        user.State = State.block;
        if (!await Operation.LogOutAsync(user, inst))
        {
            {
                user.State = State.main;
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Ошибка. Возможно на аккаунте запущены отложенные отработки.", true);
                return;
            }
        }

        var currentUser = inst.Api.GetLoggedUser();
        user.EnterData = new Instagram
            {User = user, Username = currentUser.UserName, Password = currentUser.Password};
        var login = await Operation.CheckLoginAsync(user.EnterData);
        user.State = State.main;
        if (await MainBot.Login(client, user, login))
        {
            await using var db = new Db();
            db.Update(user);
            db.Add(user.EnterData);
            user.EnterData = null;
            await db.SaveChangesAsync();
        }
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data.StartsWith("reLogIn");
    }
}