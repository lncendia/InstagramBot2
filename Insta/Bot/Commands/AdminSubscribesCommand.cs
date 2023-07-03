using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class AdminSubscribesCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        if (user.State == State.main || user.State == State.mailingAdmin)
        {
            await client.SendTextMessageAsync(user.Id, "Добро пожаловать в панель подписок.",
                replyMarkup: new ReplyKeyboardRemove());
            await client.SendTextMessageAsync(user.Id,
                "Введите id человека и дату окончания подписки (111111111 11.11.2011).\nДля стандартного времени действия введите \"s\" (111111111 s).",
                replyMarkup: Keyboards.Main);
            user.State = State.subscribesAdmin;
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
        return message.Type == MessageType.Text && message.Text == "/subscribes" && (user.State == State.main || user.State == State.subscribesAdmin) && (user.Id == 346978522 || user.Id == 921976182);
    }
}