using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class StartNowQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await client.DeleteMessageAsync(query.From.Id,
                query.Message.MessageId);
            user.CurrentWorks.ForEach(async x => await x.StartAsync());
            user.CurrentWorks.Clear();
            user.State = State.main;
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "startNow" && user.State == State.setTimeWork;
        }
    }
}