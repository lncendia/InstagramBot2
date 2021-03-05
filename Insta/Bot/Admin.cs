using System;
using System.Linq;
using Insta.Entities;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = Insta.Entities.User;

namespace Insta.Bot
{
    public static class Admin
    {
        public static async void Admin_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Message.From.Id != 346978522 && e.Message.From.Id != 921976182) return;
                User user = MainBot.Users.ToList().FirstOrDefault(x => x.Id == e.Message.From.Id);
                if (user == null) return;
                if (user.state == User.State.mailingAdmin && e.Message.Text != "/mailing" &&
                    e.Message.Text != "/subscribes")
                {
                    SendMessageToUsers(e.Message, user);
                    return;
                }

                if (e.Message.Type != MessageType.Text) return;
                switch (e.Message.Text)
                {
                    case "/mailing":
                        if (user.state == User.State.main || user.state == User.State.subscribesAdmin)
                        {
                            await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Добро пожаловать в панель рассылки.",
                                replyMarkup: new ReplyKeyboardRemove());
                            await MainBot.Tgbot.SendTextMessageAsync(user.Id,
                                "Введите сообщение, которое хотите разослать.",
                                replyMarkup: Keyboards.Main);
                            user.state = User.State.mailingAdmin;
                        }
                        else
                        {
                            await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Вы вышли из панели рассылки.",
                                replyMarkup: Keyboards.MainKeyboard);
                            user.state = User.State.main;
                        }

                        break;
                    case "/subscribes":
                        if (user.state == User.State.main || user.state == User.State.mailingAdmin)
                        {
                            await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Добро пожаловать в панель подписок.",
                                replyMarkup: new ReplyKeyboardRemove());
                            await MainBot.Tgbot.SendTextMessageAsync(user.Id,
                                "Введите id человека и дату окончания подписки (111111111 11.11.2011).\nДля стандартного времени действия введите \"s\" (111111111 s).",
                                replyMarkup: Keyboards.Main);
                            user.state = User.State.subscribesAdmin;
                        }
                        else
                        {
                            await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Вы вышли из панели рассылки.",
                                replyMarkup: Keyboards.MainKeyboard);
                            user.state = User.State.main;
                        }

                        break;
                    default:
                        switch (user.state)
                        {
                            case User.State.subscribesAdmin:
                            {
                                string[] data = e.Message.Text.Split(' ');
                                if (data.Length != 2)
                                {
                                    await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Неверные данные.");
                                    return;
                                }

                                if (!int.TryParse(data[0], out int x))
                                {
                                    await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Неверный id.");
                                    return;
                                }

                                DateTime date;
                                if (data[1] == "s") date = DateTime.Now.AddDays(30);
                                else
                                {
                                    if (!DateTime.TryParse(data[1], out date))
                                    {
                                        await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Неверно введена дата.");
                                        return;
                                    }

                                    if (date.CompareTo(DateTime.Now) <= 0)
                                    {
                                        await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Неверно введена дата.");
                                        return;
                                    }
                                }

                                await using Db db = new Db();
                                var user2 = MainBot.Users.ToList().FirstOrDefault(_ => _.Id == x);
                                if (user2 == null)
                                {
                                    await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Пользователь не найден.");
                                    return;
                                }

                                db.Update(user2);
                                db.Add(new Subscribe() {User = user2, EndSubscribe = date});
                                var inst = user2.Instagrams.ToList().FirstOrDefault(_ => _.IsDeactivated);
                                if (inst != null)
                                    inst.IsDeactivated = false;
                                await db.SaveChangesAsync();
                                await MainBot.Tgbot.SendTextMessageAsync(user.Id,
                                    "Успешно. Вы в главном меню.",
                                    replyMarkup: Keyboards.MainKeyboard);
                                user.state = User.State.main;
                                await MainBot.Tgbot.SendTextMessageAsync(user2.Id,
                                    $"Администратор активировал вам подписку до {date:D}");
                                break;
                            }
                        }

                        break;
                }
            }
            catch
            {
                var user = MainBot.Users.FirstOrDefault(x => x.Id == e.Message.From.Id);
                MainBot.Error(user);
            }
        }

        private static async void SendMessageToUsers(Message message, User user)
        {
            try
            {
                switch (message.Type)
                {
                    case MessageType.Text:
                        foreach (var member in MainBot.Users.ToList())
                        {
                            try
                            {
                                await MainBot.Tgbot.SendTextMessageAsync(member.Id, message.Text);
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        break;
                    case MessageType.Photo:
                        foreach (var member in MainBot.Users.ToList())
                        {
                            try
                            {
                                await MainBot.Tgbot.SendPhotoAsync(member.Id,
                                    new InputMedia(message.Photo.Last().FileId), caption: message.Caption);
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        break;
                    case MessageType.Audio:
                        foreach (var member in MainBot.Users.ToList())
                        {
                            try
                            {
                                await MainBot.Tgbot.SendAudioAsync(member.Id, new InputMedia(message.Audio.FileId));
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        break;
                    case MessageType.Video:
                        foreach (var member in MainBot.Users.ToList())
                        {
                            try
                            {
                                await MainBot.Tgbot.SendVideoAsync(member.Id, new InputMedia(message.Video.FileId),
                                    caption: message.Caption);
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        break;
                    case MessageType.Voice:
                        foreach (var member in MainBot.Users.ToList())
                        {
                            try
                            {
                                await MainBot.Tgbot.SendVoiceAsync(member.Id, new InputMedia(message.Voice.FileId));
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        break;
                    case MessageType.Document:
                        foreach (var member in MainBot.Users.ToList())
                        {
                            try
                            {
                                await MainBot.Tgbot.SendPhotoAsync(member.Id,
                                    new InputMedia(message.Document.FileId));
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        break;
                    case MessageType.Sticker:
                        foreach (var member in MainBot.Users.ToList())
                        {
                            try
                            {
                                await MainBot.Tgbot.SendPhotoAsync(member.Id, new InputMedia(message.Sticker.FileId));
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        break;
                }

                await MainBot.Tgbot.SendTextMessageAsync(user.Id,
                    "Сообщение было успешно отправлено. Вы в главном меню.",
                    replyMarkup: Keyboards.MainKeyboard);
                user.state = User.State.main;
            }
            catch
            {
                MainBot.Error(user);
            }
        }
    }
}