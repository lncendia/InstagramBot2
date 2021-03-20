using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class EnterDurationCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!message.Text.Contains(':'))
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Неверный формат!", replyMarkup: Keyboards.Back);
                return;
            }

            if (!int.TryParse(message.Text.Split(':')[0], out var lowerDelay))
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Неверный формат!", replyMarkup: Keyboards.Back);
                return;
            }

            if (!int.TryParse(message.Text.Split(':')[1], out var upperDelay))
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Неверный формат!", replyMarkup: Keyboards.Back);
                return;
            }

            if (upperDelay < lowerDelay)
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Верхняя граница не может быть больше нижней!", replyMarkup: Keyboards.Back);
                return;
            }

            if (upperDelay > 300)
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Интервал не может быть больше 5 минут!", replyMarkup: Keyboards.Back);
                return;
            }

            user.CurrentWorks.ForEach(_ => _.SetDuration(lowerDelay, upperDelay));
            await client.SendTextMessageAsync(message.From.Id,
                 "С какого поста начать отработку?", replyMarkup: Keyboards.SetOffset);
            user.State = State.setOffset;
        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && user.State == State.setDuration;
        }
    }
}