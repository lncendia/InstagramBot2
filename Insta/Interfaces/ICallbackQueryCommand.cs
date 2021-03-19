using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Interfaces
{
    public interface ICallbackQueryCommand
    {
        public Task Execute(TelegramBotClient client, User user, CallbackQuery query);
        public bool Compare(CallbackQuery query, User user);
    }
}