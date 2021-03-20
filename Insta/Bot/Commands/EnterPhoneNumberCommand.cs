using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Working;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class EnterPhoneNumberCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            bool isRight = await Operation.SubmitPhoneChallengeRequiredAsync(user.EnterData.Api,
                message.Text);
            if (isRight)
            {
                user.State = State.challengeRequiredAccept;
                await client.SendTextMessageAsync(message.From.Id,
                    "Код отправлен. Введите код из сообщения.", replyMarkup: Keyboards.Main);
            }
            else
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Ошибка. Неверный номер.", replyMarkup: Keyboards.Main);
            }

        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && user.State == State.challengeRequiredPhoneCall;
        }
    }
}