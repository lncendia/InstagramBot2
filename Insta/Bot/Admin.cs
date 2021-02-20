﻿using System.Linq;
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
                if (user.state == User.State.mailingAdmin &&e.Message.Text!="/mailing")
                {
                    switch (e.Message.Type)
                    {
                        case MessageType.Text:
                            foreach (var member in MainBot.Users.ToList())
                            {
                                try
                                {
                                    await MainBot.Tgbot.SendTextMessageAsync(member.Id, e.Message.Text);
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
                                        new InputMedia(e.Message.Photo.Last().FileId), caption: e.Message.Caption);
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
                                    await MainBot.Tgbot.SendAudioAsync(member.Id, new InputMedia(e.Message.Audio.FileId));
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
                                    await MainBot.Tgbot.SendVideoAsync(member.Id, new InputMedia(e.Message.Video.FileId),
                                        caption: e.Message.Caption);
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
                                    await MainBot.Tgbot.SendVoiceAsync(member.Id, new InputMedia(e.Message.Voice.FileId));
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
                                        new InputMedia(e.Message.Document.FileId));
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
                                    await MainBot.Tgbot.SendPhotoAsync(member.Id, new InputMedia(e.Message.Sticker.FileId));
                                }
                                catch
                                {
                                    // ignored
                                }
                            }

                            break;
                    }
                    await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Сообщение было успешно отправлено.",
                        replyMarkup: Keyboards.MainKeyboard);
                    user.state = User.State.main;
                    return;
                }

                if (e.Message.Type != MessageType.Text) return;
                switch (e.Message.Text)
                {
                    case "/mailing":
                        if (user.state == User.State.main)
                        {
                            await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Добро пожаловать в панель рассылки.",
                                replyMarkup: new ReplyKeyboardRemove());
                            await MainBot.Tgbot.SendTextMessageAsync(user.Id, "Введите сообщение, которое хотите разослать.",
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
                }
            }
            catch
            {
                var user = MainBot.Users.FirstOrDefault(x => x.Id == e.Message.From.Id);
                MainBot.Error(user);
            }
        }
    }
}