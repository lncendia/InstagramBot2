using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class EnterHashtagCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!message.Text.All(_ => char.IsLetterOrDigit(_) || "_".Contains(_)))
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Хештег может содержать только буквы и цифры! Введите хештег заново.", replyMarkup: Keyboards.Back("selectMode"));
                return;
            }

            user.CurrentWorks.ForEach(_ => _.SetHashtag(message.Text));
            user.State = State.setDuration;
            await client.SendTextMessageAsync(message.From.Id,
                "Введите пределы интервала в секундах. (<strong>Пример:</strong> <em>30:120</em>).\nРекомендуемые параметры нижнего предела:\nНоввый аккаунт: <code>120 секунд.</code>\n3 - 6 месяцев: <code>90 секунд.</code>\nБольше года: <code>72 секунды.</code>\n",
                replyMarkup: Keyboards.Back("interval"), parseMode: ParseMode.Html);
        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && user.State == State.setHashtag;
        }
    }
}