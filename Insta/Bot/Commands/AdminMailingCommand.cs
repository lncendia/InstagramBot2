using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class AdminMailingCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        if (user.State == State.main || user.State == State.subscribesAdmin)
        {
            await client.SendTextMessageAsync(user.Id, "Добро пожаловать в панель рассылки.",
                replyMarkup: new ReplyKeyboardRemove());
            await client.SendTextMessageAsync(user.Id,
                "Введите сообщение, которое хотите разослать.",
                replyMarkup: Keyboards.Main);
            user.State = State.mailingAdmin;
        }
        else
        {
            await client.SendTextMessageAsync(user.Id, "Вы вышли из панели рассылки.",
                replyMarkup: Keyboards.MainKeyboard);
            user.State = State.main;
        }

    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && message.Text == "/mailing" &&
               (user.State == State.main || user.State == State.mailingAdmin) &&
               (user.Id == 346978522 || user.Id == 921976182);
    }
}