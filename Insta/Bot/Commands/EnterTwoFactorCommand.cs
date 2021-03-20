using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Working;
using InstagramApiSharp.Classes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class EnterTwoFactorCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            var x = await Operation.SendCodeTwoFactorAsync(user.EnterData.Api, message.Text);
            if (x == null || !x.Succeeded)
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Ошибка. Попробуйте войти ещё раз.");
                user.State = State.main;
                return;
            }

            switch (x.Value)
            {
                case InstaLoginTwoFactorResult.Success:
                {
                    user.EnterData.StateData = await user.EnterData.Api.GetStateDataAsStringAsync();
                    await using Db db = new Db();
                    db.Update(user);
                    db.Add(user.EnterData);
                    user.EnterData = null;
                    await db.SaveChangesAsync();
                    await client.SendTextMessageAsync(message.From.Id,
                        "Инстаграм успешно добавлен.");
                    user.State = State.main;
                    break;
                }
                case InstaLoginTwoFactorResult.InvalidCode:
                    await client.SendTextMessageAsync(message.From.Id,
                        "Неверный код, попробуйте еще раз.", replyMarkup: Keyboards.Main);
                    break;
                default:
                    await client.SendTextMessageAsync(message.From.Id,
                        "Ошибка. Попробуйте войти ещё раз.");
                    user.State = State.main;
                    break;
            }

        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && user.State == State.enterTwoFactorCode;
        }
    }
}