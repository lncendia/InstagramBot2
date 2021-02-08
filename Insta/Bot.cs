using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InstagramApiSharp.Classes;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InputFiles;

namespace Insta
{
    class Bot
    {
        public static readonly TelegramBotClient Tgbot = 
            new("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");

        public static List<User> Users;
        public static readonly Random Rnd = new();
        public static void Start()
        {
            using DB db = new DB();
            Users = db.Users.Include(i => i.Instagrams).Include(i=>i.Subscribes).ToList();
            db.Dispose();
            Operation.SubscribeToEvent(Users);
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnMessage += Admin.Admin_OnMessage;
            Tgbot.OnCallbackQuery += Tgbot_OnCallbackQuery;
            Tgbot.StartReceiving();
        }
        private static async void Tgbot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            try
            {
                var cb = e.CallbackQuery.Data;
                var user = Users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user == null) return;

                if (cb.StartsWith("bill"))
                {
                    if (Payment.CheckPay(user, cb[5..]))
                    {
                        string message = e.CallbackQuery.Message.Text;
                        message = message.Replace("❌ Статус: Не оплачено", "✔ Статус: Оплачено");
                        message=message.Remove(message.IndexOf("Оплачено", StringComparison.Ordinal)+8);
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            message);
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Успешно оплачено.");
                    }

                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Не оплачено.");
                    return;
                }

                if (cb.StartsWith("exit"))
                {
                    if (user.state != User.State.main) return;
                    if (!await Operation.LogOut(user, int.Parse(cb[5..])))
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм не найден.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    }
                    else
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Успешно.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    }
                    return;
                }

                IResult<InstaLoginResult> login;
                if (cb.StartsWith("select_"))
                {
                    if (user.state != User.State.selectAccounts) return;
                    user.state = User.State.block;
                    Instagram inst = user.Instagrams.Find(x => x.Id == int.Parse(cb[7..]));
                    if (inst == null)
                    {
                        user.state = User.State.selectAccounts;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм не найден.");
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            e.CallbackQuery.Message.Text,
                            replyMarkup: Keyboards.NewSelect(e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                                e.CallbackQuery));
                        return;
                    }
                    
                    if (inst.IsDeactivated)
                    {
                        user.state = User.State.selectAccounts;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Аккаунт {inst.Username} деактивирован. Купите подписку, чтобы активировать аккаунт.");
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            e.CallbackQuery.Message.Text,
                            replyMarkup: Keyboards.NewSelect(e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                                e.CallbackQuery));
                        return;
                    }

                    if (user.CurrentWorks.FirstOrDefault(x => x.GetUsername() == inst.Username) != null)
                    {
                        user.state = User.State.selectAccounts;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                            $"Аккаунт {inst.Username} уже добавлен.");
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            e.CallbackQuery.Message.Text,
                            replyMarkup: Keyboards.NewSelect(e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                                e.CallbackQuery));
                        return;
                    }
                    if (inst.api == null || !inst.api.IsUserAuthenticated)
                    {
                        user.EnterData = inst;
                        var message = await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            $"Необходима аутентификация в аккаунт {inst.Username}. Пожалуйста, подождите...");
                        user.EnterData = inst;
                        login = await Operation.CheckLoginAsync(user.EnterData);
                        try
                        {
                            await Tgbot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                        }
                        catch
                        {
                            // ignored
                        }
                        if (login?.Value != InstaLoginResult.Success)
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                $"Не удалось войти. Инстаграм {inst.Username} будет удален, попробуйте войти в него заново.", showAlert:true);
                            user.EnterData = null;
                            await using DB db = new DB();
                            db.UpdateRange(user, inst);
                            user.Instagrams.Remove(inst);
                            db.Remove(inst);
                            await db.SaveChangesAsync();
                            user.state = User.State.selectAccounts;
                            await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                e.CallbackQuery.Message.Text,
                                replyMarkup: Keyboards.NewSelect(e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                                    e.CallbackQuery));
                            return;
                        }
                    }

                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                        $"Инстаграм {inst.Username} успешно добавлен.");
                    await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                        e.CallbackQuery.Message.Text,
                        replyMarkup: Keyboards.NewSelect(e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                            e.CallbackQuery));
                    var work = new Work(user.Works.Count, inst.api, user);
                    user.CurrentWorks.Add(work);
                    user.state = User.State.selectAccounts;
                    return;
                }

                if (cb.StartsWith("cancel"))
                {
                    if (user.state != User.State.main) return;
                    user.state = User.State.block;
                    Work work = user.Works.Find(x => x.Id == int.Parse(cb[7..]));
                    if (work == null)
                    {
                        user.state = User.State.main;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Отработка не найдена.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        return;
                    }

                    if (work.IsStarted)
                    {
                        work.CancelTokenSource.Cancel();
                    }
                    else
                    {
                        work.TimerDispose();
                    }

                    user.Works.Remove(work);
                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Отработка отменена.");
                    await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                        e.CallbackQuery.Message.MessageId);
                    user.state = User.State.main;
                    return;
                }

                switch (cb)
                {
                    case "selectAll":
                        if(user.state!=User.State.selectAccounts) return;
                        try
                        {
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        }
                        catch
                        {
                            // ignored
                        }

                        user.state = User.State.block;
                        foreach (var inst in user.Instagrams)
                        {
                            if (inst.IsDeactivated)
                            {
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    $"Аккаунт {inst.Username} деактивирован. Купите подписку, чтобы активировать аккаунт.");
                                continue;
                            }
                            if (user.CurrentWorks.FirstOrDefault(x => x.GetUsername() == inst.Username) != null)
                            {
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    $"Аккаунт {inst.Username} уже добавлен.");
                                continue;
                            }
                            if (inst.api == null || !inst.api.IsUserAuthenticated)
                            {
                                var message = await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    $"Необходима аутентификация в аккаунт {inst.Username}. Пожалуйста, подождите...");
                                user.EnterData = inst;
                                login = await Operation.CheckLoginAsync(user.EnterData);
                                try
                                {
                                    await Tgbot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                                }
                                catch
                                {
                                    // ignored
                                }

                                if (login?.Value != InstaLoginResult.Success)
                                {
                                    await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                        $"Не удалось войти. Инстаграм {inst.Username} будет удален, попробуйте войти в него заново.");
                                    user.EnterData = null;
                                    await using DB db = new DB();
                                    db.UpdateRange(user, inst);
                                    user.Instagrams.Remove(inst);
                                    db.Remove(inst);
                                    await db.SaveChangesAsync();
                                    continue;
                                }
                            }
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                $"Инстаграм {inst.Username} добавлен.");
                            var work = new Work(user.Works.Count, inst.api, user);
                            user.CurrentWorks.Add(work);
                        }

                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Выберите режим.", replyMarkup: Keyboards.SelectMode);
                        user.state = User.State.selectMode;
                        break;
                    case "startLike":
                        if(user.state!=User.State.selectMode) return;
                        user.CurrentWorks.ForEach(x => x.SetMode(Work.Mode.like));
                        user.state = User.State.selectHashtag;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Введите хештег без #.", replyMarkup: Keyboards.Main);
                        break;
                    case "startSave":
                        if(user.state!=User.State.selectMode) return;
                        user.CurrentWorks.ForEach(x => x.SetMode(Work.Mode.save));
                        user.state = User.State.selectHashtag;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Введите хештег без #.", replyMarkup: Keyboards.Main);
                        break;
                    case "startFollowing":
                        if(user.state!=User.State.selectMode) return;
                        user.CurrentWorks.ForEach(x => x.SetMode(Work.Mode.follow));
                        user.state = User.State.selectHashtag;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Введите хештег без #.", replyMarkup: Keyboards.Main);
                        break;
                    case "startAll":
                        if(user.state!=User.State.selectMode) return;
                        user.CurrentWorks.ForEach(x => x.SetMode(Work.Mode.likeAndSave));
                        user.state = User.State.selectHashtag;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Введите хештег без #.", replyMarkup: Keyboards.Main);
                        break;
                    case "selectMode":
                        if (user.state != User.State.selectAccounts) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        if (user.CurrentWorks.Count == 0)
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Вы не выбрали ни одного аккаунта.");
                            return;
                        }
                        user.state = User.State.selectMode;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Выберите режим.", replyMarkup: Keyboards.SelectMode);
                        break;
                    case "startWorking":
                        if(user.state!=User.State.main) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        if (user.Instagrams.Count == 0)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "У вас нет аккаунтов.");
                            break;
                        }
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Нажмите на нужные аккаунты.",replyMarkup:Keyboards.Select(user));
                        user.state = User.State.selectAccounts;
                        break;
                    case "stopWorking":
                        if (user.state != User.State.main) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        if (user.Works.Count == 0)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "У вас нет активных отработок.");
                            break;
                        }
                        foreach (var x in user.Works.ToList())
                        {
                            var str = x.IsStarted ? "Уже началась" : "Еще не началась";
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                $"Аккаунт {x.GetUsername()}. Хештег #{x.Hashtag}. {str}.",
                                replyMarkup: Keyboards.Cancel(x.Id));
                        }
                        break;
                    case "startNow":
                        if (user.state != User.State.setTimeWork) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        user.CurrentWorks.ForEach(x => x.Start());
                        user.CurrentWorks.Clear();
                        user.state = User.State.main;
                        break;
                    case "startLater":
                        if (user.state != User.State.setTimeWork) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        user.state = User.State.setDate;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Введите время запуска по МСК в формате ЧЧ:мм. (<strong>Пример:</strong> <em>13:30</em>).",replyMarkup: Keyboards.Back,parseMode: ParseMode.Html);
                        break;
                    case "enterData":
                        if(user.state!=User.State.main) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        if (user.Instagrams.Count >=user.Subscribes.Count)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Увы... Так не работает.");
                            return;
                        }
                        user.EnterData = new Instagram(){User=user};
                        user.state = User.State.login;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Введите логин", replyMarkup: Keyboards.Main);
                        break;
                    case "acceptEntry":
                        if(user.state!=User.State.challengeRequired) return;
                        login = await Operation.CheckLoginAsync(user.EnterData);
                        if (await Login(user, login))
                        {
                            await using DB db = new DB();
                            db.Update(user);
                            db.Add(user.EnterData);
                            user.EnterData = null;
                            await db.SaveChangesAsync();
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                e.CallbackQuery.Message.MessageId);
                        }
                        break;
                    case "challengeEmail":
                        if (user.state != User.State.challengeRequired) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        var response = await user.EnterData.api.RequestVerifyCodeToEmailForChallengeRequireAsync();
                        if (!response.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Ошибка. Попробуйте войти снова.");
                            user.state = User.State.main;
                            return;
                        }
                        user.state = User.State.challengeRequiredAccept;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Код отправлен. Введите код из сообщения.", replyMarkup: Keyboards.Main);
                        break;
                    case "challengePhone":
                        if (user.state != User.State.challengeRequired) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        var response2 = await user.EnterData.api.RequestVerifyCodeToSMSForChallengeRequireAsync();
                        if (!response2.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Ошибка. Попробуйте войти снова.");
                            user.state = User.State.main;
                        }
                        user.state = User.State.challengeRequiredAccept;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Код отправлен. Введите код из сообщения.",replyMarkup: Keyboards.Main);
                        break;
                    case "back":
                        switch (user.state)
                        {
                            case User.State.password:
                                user.state = User.State.login;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Введите логин", replyMarkup: Keyboards.Main);
                                break;
                            case User.State.twoFactor:
                                user.state = User.State.password;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Теперь введите пароль", replyMarkup: Keyboards.Back);
                                break;
                            case User.State.selectMode:
                                user.state = User.State.selectMode;
                                await Tgbot.EditMessageTextAsync(e.CallbackQuery.InlineMessageId,
                                    "Выберите режим.", replyMarkup: Keyboards.SelectMode);
                                break;
                            case User.State.setDuration:
                                user.state = User.State.selectHashtag;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Введите хештег. Решётку писать не нужно.", replyMarkup: Keyboards.Back);
                                break;
                            case User.State.setDate:
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Выбирете, когда хотите начать.", replyMarkup: Keyboards.StartWork);
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                user.state = User.State.setTimeWork;
                                break;

                        }
                        break;
                    case "mainMenu":
                        if(user.state == User.State.block) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        foreach (var work in user.CurrentWorks)
                        {
                            user.Works.Remove(work);
                        }
                        user.CurrentWorks.Clear();
                        user.EnterData = null;
                        user.state = User.State.main;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Вы в главном меню.", replyMarkup: Keyboards.MainKeyboard);
                        break;

                }
            }
            catch
            {
                var user = Users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                Error(user);
            }
        }

        private static async void Tgbot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                var message = e.Message;
                if (message.Type != MessageType.Text) return;
                var user = Users.FirstOrDefault(x => x.Id == message.From.Id);
                if (user == null)
                {
                    await using DB db = new DB();
                    user = new User {Id = e.Message.From.Id, state = User.State.main};
                    Users.Add(user);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    await Tgbot.SendStickerAsync(message.From.Id,
                        new InputOnlineFile("CAACAgIAAxkBAAK_HGAQINBHw7QKWWRV4LsEU4nNBxQ3AAKZAAPZvGoabgceWN53_gIeBA"),
                        replyMarkup: Keyboards.MainKeyboard);
                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                        "Добро пожаловать.\nДля дальнейшей работы тебе необходимо оплатить подписку и ввести данные своего instagram.");
                    return;
                }

                switch (message.Text)
                {
                    case "/start":
                        if (user.state != User.State.main) break;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Вы в главном меню.", replyMarkup: Keyboards.MainKeyboard);
                        break;
                    case "🌇 Мои аккаунты":
                        if (user.state != User.State.main) break;
                        foreach (var x in user.Instagrams)
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                $"{Keyboards.Emodji[Rnd.Next(0, Keyboards.Emodji.Length)]} Аккаунт {x.Username}",
                                replyMarkup: Keyboards.Exit(x.Id));
                        }
                        if(user.Instagrams.Count<user.Subscribes.Count)
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Вы можете добавить аккаунт",
                                replyMarkup: new InlineKeyboardMarkup(
                                    InlineKeyboardButton.WithCallbackData("➕ Добавить аккаунт", "enterData")));
                        else
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Оплатите подписку, чтобы добавить аккаунт.", replyMarkup: Keyboards.MainKeyboard);
                        }
                        break;
                    case "❤ Отработка":
                        if (user.state != User.State.main) return;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Выберите, что вы хотите сделать.", replyMarkup: Keyboards.Working);
                        break;
                    case "📄 Инструкция":
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Всю инструкцию вы можете прочитать в канале @likebotgid.");
                        break;
                    case "🤝 Поддержка":
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "За поддержкой вы можете обратиться к @Per4at.");
                        break;
                    case "💰 Оплатить подписку":
                        if (user.state != User.State.main) return;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Введите количество аккаунтов, которые хотите добавить. Цена одного аккаунта - 120 рублей/30 дней.",
                            replyMarkup: Keyboards.Main);
                        user.state = User.State.enterCountToBuy;
                        break;
                    case "⏱ Мои подписки":
                        if (user.state != User.State.main) return;
                        string subscribes = $"У вас {user.Subscribes.Count} подписки(ок).\n";
                        int i = 0;
                        foreach (var sub in user.Subscribes.ToList())
                        {
                            i++;
                            subscribes += $"Подписка {i}. Истекает {sub.EndSubscribe.ToString("g")}\n";
                        }
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            subscribes);
                        break;
                    default:
                        switch (user.state)
                        {
                            case User.State.login:
                                if (user.Instagrams.Find(instagram => instagram.Username == message.Text) != null)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Вы уже добавили этот аккаунт.");
                                    user.state = User.State.main;
                                    break;
                                }

                                user.EnterData.Username = message.Text;
                                user.state = User.State.password;
                                await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                    "Теперь введите пароль.", replyMarkup: Keyboards.Back);
                                break;
                            case User.State.password:
                                if (e.Message.Text.Length < 6)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Пароль не может быть меньше 6 символов!");
                                    return;
                                }

                                user.EnterData.Password = message.Text;
                                var login = await Operation.CheckLoginAsync(user.EnterData);
                                if (await Login(user, login))
                                {
                                    await using DB db = new DB();
                                    db.Update(user);
                                    db.Add(user.EnterData);
                                    user.EnterData = null;
                                    await db.SaveChangesAsync();
                                }

                                break;
                            case User.State.twoFactor:
                                var x = await Operation.SendCodeTwoFactorAsync(user.EnterData.api, message.Text);
                                if (x == null || !x.Succeeded)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Ошибка. Попробуйте войти ещё раз.");
                                    user.state = User.State.main;
                                    return;
                                }

                                switch (x.Value)
                                {
                                    case InstaLoginTwoFactorResult.Success:
                                    {
                                        await using DB db = new DB();
                                        db.Update(user);
                                        db.Add(user.EnterData);
                                        user.EnterData = null;
                                        await db.SaveChangesAsync();
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Инстаграм успешно добавлен.");
                                        user.state = User.State.main;
                                        break;
                                    }
                                    case InstaLoginTwoFactorResult.InvalidCode:
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Неверный код, попробуйте еще раз.", replyMarkup: Keyboards.Main);
                                        break;
                                    default:
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Ошибка. Попробуйте войти ещё раз.");
                                        user.state = User.State.main;
                                        break;
                                }

                                break;
                            case User.State.challengeRequiredAccept:
                                var y = await Operation.SendCodeChallengeRequiredAsync(user.EnterData.api,
                                    message.Text);
                                if (await Login(user, y))
                                {
                                    await using DB db = new DB();
                                    db.Update(user);
                                    db.Add(user.EnterData);
                                    user.EnterData = null;
                                    await db.SaveChangesAsync();
                                }
                                break;
                            case User.State.challengeRequiredPhoneCall:
                                bool isRight = await Operation.SubmitPhoneChallengeRequiredAsync(user.EnterData.api,
                                    message.Text);
                                if (isRight)
                                {
                                    user.state = User.State.challengeRequiredAccept;
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Код отправлен. Введите код из сообщения.", replyMarkup: Keyboards.Main);
                                }
                                else
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Ошибка. Неверный номер.", replyMarkup: Keyboards.Main);
                                }
                                break;
                            case User.State.selectHashtag:
                                user.CurrentWorks.ForEach(_ => _.SetHashtag(message.Text));
                                user.state = User.State.setDuration;
                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    "Введите пределы интервала в секундах. (<strong>Пример:</strong> <em>30:120</em>).\nРекомендуемые параметры нижнего предела:\nНоввый аккаунт: <code>120 секунд.</code>\n3 - 6 месяцев: <code>90 секунд.</code>\nБольше года: <code>72 секунды.</code>\n", replyMarkup: Keyboards.Back,parseMode: ParseMode.Html);
                                break;
                            case User.State.setDuration:
                                if(!e.Message.Text.Contains(':'))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат!");
                                    return;
                                }
                                if (!int.TryParse(message.Text.Split(':')[0], out var lowerDelay))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат!");
                                    return;
                                }

                                if (!int.TryParse(message.Text.Split(':')[1], out var upperDelay))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат!");
                                }

                                if (upperDelay < lowerDelay)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Верхняя граница не может быть больше нижней!");
                                    return;
                                }
                                if (upperDelay > 300)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Интервал не может быть больше 5 минут!");
                                    return;
                                }

                                user.CurrentWorks.ForEach(_ => _.SetDuration(lowerDelay,upperDelay));
                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    "Выбирете, когда хотите начать.", replyMarkup: Keyboards.StartWork);
                                user.state = User.State.setTimeWork;
                                break;
                            case User.State.setDate:
                                if (TimeSpan.TryParse(message.Text, out var timeSpan))
                                {
                                    var timeEnter = DateTime.Today.Add(timeSpan);
                                    if (timeEnter.CompareTo(DateTime.Now) <= 0)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Введите корректное время.");
                                        return;
                                    }
                                    user.CurrentWorks.ForEach(_ => _?.StartAtTime(timeEnter.Subtract(DateTime.Now)));
                                    foreach (var work in user.CurrentWorks.Where(work => work != null))
                                    {
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            $"Отработка на аккаунте {work.GetUsername()} по хештегу #{work.Hashtag} начнётся в {timeEnter:HH:mm:ss}.",
                                            replyMarkup: Keyboards.Cancel(work.Id));
                                    }
                                    user.state = User.State.main;
                                }
                                else
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат времени.");
                                }
                                break;
                            case User.State.enterCountToBuy:
                                int count;
                                if (!int.TryParse(message.Text, out count))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Введите число!");
                                    return;
                                }

                                if (count > 100)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Слишком большое количество!");
                                    return;
                                }

                                user.state = User.State.main;
                                string billId = "";
                                var payUrl = Payment.AddTransaction(count * 120, user, ref billId);
                                if (payUrl == null)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Произошла ошибка при создании счета. Попробуйте еще раз.");
                                    return;
                                }

                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    $"💸 Оплата подписки на сумму {count * 120} р.\n📆 Дата: {DateTime.Now:dd.MMM.yyyy}\n❌ Статус: Не оплачено.\n\n💳 Оплатите счет по ссылке.\n{payUrl}",
                                    replyMarkup: Keyboards.CheckBill(billId));
                                break;
                        }
                        break;
                }
            }
            catch
            {
                var user = Users.FirstOrDefault(x => x.Id == e.Message.From.Id);
                Error(user);
            }
        }

        private static async Task<bool> Login(User user, IResult<InstaLoginResult> login)
        {
            try
            {
                if (login == null)
                {
                    await Tgbot.SendTextMessageAsync(user.Id,
                        "Ошибка. Данные введены неверно.");
                    user.state = User.State.main;
                    return false;
                }
                switch (login.Value)
                {
                    case InstaLoginResult.Success:
                    {
                        if (!login.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Ошибка. Данные введены неверно.");
                            user.state = User.State.main;
                        }
                        await Tgbot.SendTextMessageAsync(user.Id, "Инстаграм успешно добавлен.");
                        user.state = User.State.main;
                        return true;
                    }
                    case InstaLoginResult.BadPassword:
                        await Tgbot.SendTextMessageAsync(user.Id, "Неверный пароль. Попробуйте ввести снова.",
                            replyMarkup: Keyboards.Back);
                        break;
                    case InstaLoginResult.InvalidUser:
                        user.state = User.State.main;
                        await Tgbot.SendTextMessageAsync(user.Id, "Пользователь не найден. Попробуйте еще раз.",
                            replyMarkup: Keyboards.EnterData);
                        break;
                    case InstaLoginResult.LimitError:
                        user.state = User.State.main;
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Слишком много запросов. Подождите несколько минут и попробуйте снова.",
                            replyMarkup: Keyboards.EnterData);
                        break;
                    case InstaLoginResult.TwoFactorRequired:
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Необходима двухфакторная аутентификация. Введите код из сообщения.", replyMarkup: Keyboards.Main);
                        user.state = User.State.twoFactor;
                        break;
                    case InstaLoginResult.ChallengeRequired:
                    {
                        var challenge = await user.EnterData.api.GetChallengeRequireVerifyMethodAsync();
                        if (!challenge.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Ошибка. Попробуйте войти ещё раз.");
                            user.state = User.State.main;
                            return false;
                        }
                        if (challenge.Value.SubmitPhoneRequired)
                        {
                            user.state = User.State.challengeRequiredPhoneCall;
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Инстаграм просит подтверждение. Введите подключенный к аккаунту номер.", replyMarkup: Keyboards.Main);
                            break;
                        }

                        InlineKeyboardMarkup key;
                        if (string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                        {
                            key = Keyboards.Email(challenge.Value.StepData.Email);
                        }
                        else if (string.IsNullOrEmpty(challenge.Value.StepData.Email))
                        {
                            key = Keyboards.Phone(challenge.Value.StepData.PhoneNumber);
                        }
                        else
                        {
                            key = Keyboards.PhoneAndEmail(challenge.Value.StepData.Email,challenge.Value.StepData.PhoneNumber);
                        }
                        user.state = User.State.challengeRequired;
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Инстаграм просит подтверждение. Выбирете, каким образом вы хотите подтвердить код:",
                            replyMarkup: key);
                    }
                        break;
                }

                return false;
            }
            catch
            {
                Error(user);
                return false;
            }
        }

        public static async void Error(User user)
        {
            if (user == null) return;
            user.CurrentWorks.ForEach(x=>user.Works.Remove(x));
            user.EnterData = null;
            user.state = User.State.main;
            try
            {
                await Tgbot.SendTextMessageAsync(user.Id,
                    "Произошла ошибка.", replyMarkup: Keyboards.MainKeyboard);
            }
            catch
            {
                // ignored
            }
        }
    }
}
