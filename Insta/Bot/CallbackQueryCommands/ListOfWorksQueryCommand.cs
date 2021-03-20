using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class ListOfWorksQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.State != State.main)
            {
                await client.AnswerCallbackQueryAsync(query.Id, "Вы должны быть в главное меню.");
                return;
            }

            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            if (user.Works.Count == 0)
            {
                await client.SendTextMessageAsync(query.From.Id,
                    "У вас нет активных отработок.");
            }

            foreach (var x in user.Works.ToList())
            {
                var str = x.IsStarted ? "Уже началась" : "Еще не началась";
                await client.SendTextMessageAsync(query.From.Id,
                    $"Аккаунт {x.Instagram.Username}. Хештег #{x.Hashtag}. {str}. {x.GetInformation()}",
                    replyMarkup: Keyboards.Cancel(x.Id));
            }
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "stopWorking";
        }
    }
}