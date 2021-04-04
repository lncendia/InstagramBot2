using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class SelectAccountsListQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.State != State.main)
            {
                await client.AnswerCallbackQueryAsync(query.Id, "Вы должны быть в главное меню.");
                return;
            }

            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            if (user.Instagrams.Count == 0 && user.Instagrams.All(_=>_.IsDeactivated))
            {
                await client.SendTextMessageAsync(user.Id,
                    "У вас нет подходящих аккаунтов."); 
                return;
            }

            await client.SendTextMessageAsync(query.From.Id,
                "Нажмите на нужные аккаунты.", replyMarkup: Keyboards.Select(user));
            user.State = State.selectAccounts;
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "startWorking";
        }
    }
}