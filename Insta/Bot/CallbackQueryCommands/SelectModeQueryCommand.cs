using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class SelectModeQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.CurrentWorks.Count == 0)
            {
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Вы не выбрали ни одного аккаунта.");
                return;
            }

            user.State = State.setMode;
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            await client.SendTextMessageAsync(query.From.Id,
                "Выберите режим.", replyMarkup: Keyboards.SelectMode);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "selectMode" && user.State == State.selectAccounts;
        }
    }
}