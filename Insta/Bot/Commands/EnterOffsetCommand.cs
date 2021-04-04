using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class EnterOffsetCommand:ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!int.TryParse(message.Text, out var offset))
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Неверный формат!", replyMarkup: Keyboards.Back("offsetSelect"));
                return;
            }

            if (offset > 1020)
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Слишком большой сдвиг!", replyMarkup: Keyboards.Back("offsetSelect"));
                return;
            }
            user.CurrentWorks.ForEach(_ => _.SetOffset(offset));
            user.State = State.setTimeWork;
            await client.SendTextMessageAsync(user.Id,
                "Выбирете, когда хотите начать.", replyMarkup: Keyboards.StartWork);
        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && user.State == State.enterOffset;
        }
    }
}