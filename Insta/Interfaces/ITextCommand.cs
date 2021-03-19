using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Interfaces
{
    public interface ITextCommand
    {
        public Task Execute(TelegramBotClient client, User user, Message message);
        public bool Compare(Message message, User user);
    }
}