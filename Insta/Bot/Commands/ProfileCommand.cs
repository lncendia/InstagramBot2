using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class ProfileCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        await client.SendTextMessageAsync(message.Chat.Id,
            $"<b>Ваш Id:</b> {user.Id}\n<b>Бонусный счет:</b> {user.Bonus}₽\n<b>Реферальная ссылка:</b> https://telegram.me/LikeChatVip_bot?start={user.Id}",
            parseMode: ParseMode.Html, disableWebPagePreview: true, replyMarkup: Keyboards.Subscribes);
    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && message.Text == "🗒 Мой профиль" && user.State == State.main;
    }
}