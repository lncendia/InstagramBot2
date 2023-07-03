using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Working;
using InstagramApiSharp.Classes;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class AcceptLogInQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        var login = await Operation.CheckLoginAsync(user.EnterData, true);
        if (login?.Value == InstaLoginResult.Success)
        {
            if (user.EnterData != null)
            {
                user.EnterData.StateData = await user.EnterData.Api.GetStateDataAsStringAsync();
                await using var db = new Db();
                db.Update(user);
                db.Add(user.EnterData);
                user.EnterData = null;
                await db.SaveChangesAsync();
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Успешно.");
            }
            else
            {
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Ошибка.");
            }

            await client.DeleteMessageAsync(query.From.Id,
                query.Message.MessageId);
            user.State = State.main;
        }
        else
        {
            await client.AnswerCallbackQueryAsync(query.Id,
                "Произошла ошибка, попробуйте еще раз.");
        }
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data == "acceptEntry" && user.State == State.challengeRequired;
    }
}