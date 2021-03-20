using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Model;
using Insta.Working;
using InstagramApiSharp.Classes;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class ReLogInQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.State != State.main)
            {
                await client.AnswerCallbackQueryAsync(query.Id, "Вы должны быть в главное меню.");
                return;
            }

            user.State = State.block;
            Instagram inst = user.Instagrams.FirstOrDefault(_ => _.Id == int.Parse(query.Data[8..]));
            if (inst == null)
            {
                user.State = State.main;
                await client.AnswerCallbackQueryAsync(query.Id, "Инстаграм не найден.");
                await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
                return;
            }

            if (inst.IsDeactivated)
            {
                user.State = State.main;
                await client.AnswerCallbackQueryAsync(query.Id, "Инстаграм деактивирован.");
                return;
            }

            if (!await Operation.LogOutAsync(user, inst))
            {
                {
                    user.State = State.main;
                    await client.AnswerCallbackQueryAsync(query.Id,
                        "Ошибка. Возможно на аккаунте запущены отложенные отработки.", showAlert: true);
                    return;
                }
            }

            var currentUser = inst.Api.GetLoggedUser();
            user.EnterData = new Instagram
                {User = user, Username = currentUser.UserName, Password = currentUser.Password};
            var login = await Operation.CheckLoginAsync(user.EnterData);
            if (await MainBot.Login(user, login))
            {
                await using Db db = new Db();
                db.Update(user);
                db.Add(user.EnterData);
                user.EnterData = null;
                user.State = State.main;
                await db.SaveChangesAsync();
            }
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.StartsWith("reLogIn");
        }
    }
}