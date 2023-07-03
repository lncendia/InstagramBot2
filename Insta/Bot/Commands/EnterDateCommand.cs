using System;
using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class EnterDateCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        if (TimeSpan.TryParse(message.Text, out var timeSpan))
        {
            var timeEnter = DateTime.Today.Add(timeSpan);
            if (timeEnter.CompareTo(DateTime.Now) <= 0)
            {
                timeEnter = timeEnter.AddDays(1);
            }

            user.CurrentWorks.ForEach(_ => _?.StartAtTimeAsync(timeEnter));
            foreach (var work in user.CurrentWorks.Where(work => work != null))
            {
                await client.SendTextMessageAsync(message.From.Id,
                    $"Отработка на аккаунте {work.Instagram.Username} по хештегу #{work.Hashtag} начнётся в {timeEnter:HH:mm:ss}.",
                    replyMarkup: Keyboards.Cancel(work.Id));
            }

            user.CurrentWorks.Clear();
            user.State = State.main;
        }
        else
        {
            await client.SendTextMessageAsync(message.From.Id,
                "Неверный формат времени.", replyMarkup:Keyboards.Back("date"));
        }
    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && user.State == State.setDate;
    }
}