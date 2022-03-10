using System;
using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class EnterSubscribeDataCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            string[] data = message.Text.Split(' ');
                if (data.Length != 2)
                {
                    await client.SendTextMessageAsync(user.Id, "Неверные данные.", replyMarkup:Keyboards.Main);
                    return;
                }

                if (!int.TryParse(data[0], out int x))
                {
                    await client.SendTextMessageAsync(user.Id, "Неверный id.", replyMarkup:Keyboards.Main);
                    return;
                }

                DateTime date;
                if (data[1] == "s") date = DateTime.Now.AddDays(30);
                else
                {
                    if (!DateTime.TryParse(data[1], out date))
                    {
                        await client.SendTextMessageAsync(user.Id, "Неверно введена дата.", replyMarkup:Keyboards.Main);
                        return;
                    }

                    if (date.CompareTo(DateTime.Now) <= 0)
                    {
                        await client.SendTextMessageAsync(user.Id, "Неверно введена дата.", replyMarkup:Keyboards.Main);
                        return;
                    }
                }

                await using Db db = new Db();
                var user2 = BotSettings.Users.ToList().FirstOrDefault(_ => _.Id == x);
                if (user2 == null)
                {
                    await client.SendTextMessageAsync(user.Id, "Пользователь не найден.", replyMarkup:Keyboards.Main);
                    return;
                }

                db.Update(user2);
                db.Add(new Subscribe {User = user2, EndSubscribe = date});
                var inst = user2.Instagrams.ToList().FirstOrDefault(_ => _.IsDeactivated);
                if (inst != null)
                    inst.IsDeactivated = false;
                await db.SaveChangesAsync();
                await client.SendTextMessageAsync(user.Id,
                    "Успешно. Вы в главном меню.",
                    replyMarkup: Keyboards.MainKeyboard);
                user.State = State.main;
                await client.SendTextMessageAsync(user2.Id,
                    $"Администратор активировал вам подписку до {date:D}");
            }


        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && user.State == State.subscribesAdmin;
        }
    }
}