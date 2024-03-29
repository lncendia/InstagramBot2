﻿using System;
using System.Collections.Generic;
using System.Linq;
using Insta.Enums;
using Insta.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = Insta.Model.User;

namespace Insta.Bot;

public static class Keyboards
{
    public static readonly ReplyKeyboardMarkup MainKeyboard = new(new List<List<KeyboardButton>>
    {
        new() { new KeyboardButton("🌇 Мои аккаунты"), new KeyboardButton("❤ Отработка") },
        new() { new KeyboardButton("💰 Оплатить подписку"), new KeyboardButton("🗒 Мой профиль") },
        new() { new KeyboardButton("📄 Инструкция"), new KeyboardButton("🤝 Поддержка") }
    })
    {
        ResizeKeyboard = true,
        InputFieldPlaceholder = "Нажмите на нужную кнопку"
    };

    public static readonly string[] Emodji =
        { "🏞", "🏔", "🏖", "🌋", "🏜", "🏕", "🌎", "🗽", "🌃", "☘", "🐲", "🌸", "🌓", "🍃", "☀", "☁" };

    public static InlineKeyboardMarkup Back(string query)
    {
        return new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🔙 Назад", $"back_{query}"));
    }

    public static readonly InlineKeyboardMarkup Main =
        new(InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu"));

    public static readonly InlineKeyboardMarkup Subscribes =
        new(InlineKeyboardButton.WithCallbackData("⏱ Мои подписки", "subscribes"));

    public static readonly InlineKeyboardMarkup Working = new(
        new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("🏃 Начать отработку", "startWorking") },
            new() { InlineKeyboardButton.WithCallbackData("⚙ Активные отработки", "stopWorking") }
        });

    public static readonly InlineKeyboardMarkup SelectMode = new(
        new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("❤ Лайки", $"wtype_{Mode.like}"),
                InlineKeyboardButton.WithCallbackData("💾 Сохранения", $"wtype_{Mode.save}")
            },
            new() { InlineKeyboardButton.WithCallbackData("☑ Лайки + сохранения", $"wtype_{Mode.likeAndSave}") },
            new() { InlineKeyboardButton.WithCallbackData("➕ Подписки", $"wtype_{Mode.follow}") },
            new() { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_selectHashtagMode") }
        });

    public static readonly InlineKeyboardMarkup SelectHashtagMode = new(
        new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("☑ Обычные", $"htype_{HashtagType.recent}") },
            new() { InlineKeyboardButton.WithCallbackData("➕ Рилс", $"htype_{HashtagType.reels}") },
            new() { InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu") }
        });

    public static readonly InlineKeyboardMarkup SetOffset = new(
        new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("➡ С последнего", "lastPost"),
                InlineKeyboardButton.WithCallbackData("↪ Ввести номер поста", "setOffset")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_offset")
            }
        });

    public static readonly InlineKeyboardMarkup StartWork = new(
        new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("🏃 Начать сейчас", "startNow"),
                InlineKeyboardButton.WithCallbackData("⌛ Начать позже", "startLater")
            },
            new() { InlineKeyboardButton.WithCallbackData("🛑 Отмена", "mainMenu") }
        });


    public static InlineKeyboardMarkup Select(User user)
    {
        var accounts = user.Instagrams.Select(inst => new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{Emodji[new Random().Next(0, Emodji.Length)]} {inst.Username}",
                $"select_{inst.Id}")
        }).ToList();

        accounts.Add(new List<InlineKeyboardButton>
            { InlineKeyboardButton.WithCallbackData("🗒 Выбрать все аккаунты", "selectAll") });
        accounts.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("👈 Выбрать режим", "selectHashtagType"),
            InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")
        });

        return new InlineKeyboardMarkup(accounts);
    }

    public static InlineKeyboardMarkup NewSelect(List<IEnumerable<InlineKeyboardButton>> keyboard,
        CallbackQuery query)
    {
        keyboard.Remove(keyboard.FirstOrDefault(_ => _.FirstOrDefault()?.CallbackData == query.Data));
        if (keyboard.Count == 2)
        {
            keyboard.Remove(keyboard.FirstOrDefault(_ => _.FirstOrDefault()?.CallbackData == "selectAll"));
        }

        return new InlineKeyboardMarkup(keyboard);
    }

    public static readonly InlineKeyboardMarkup EnterData = new(
        InlineKeyboardButton.WithCallbackData("🖊 Ввести данные", "enterData"));

    public static InlineKeyboardMarkup ChangeProxy(Instagram instagram)
    {
        if (instagram == null) return null;
        return new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("♻ Сменить прокси", $"changeProxy_{instagram.Id}"));
    }

    public static InlineKeyboardMarkup Cancel(long id)
    {
        return new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🛑 Отменить", $"cancel_{id}"));
    }

    public static InlineKeyboardMarkup Exit(long id, bool addExit = true)
    {
        var keyboard = new List<InlineKeyboardButton>();
        if (addExit) keyboard.Add(InlineKeyboardButton.WithCallbackData("🚪 Выйти", $"exit_{id}"));
        keyboard.Add(InlineKeyboardButton.WithCallbackData("♻ Перезайти", $"reLogIn_{id}"));
        return new InlineKeyboardMarkup(keyboard);
    }

    public static InlineKeyboardMarkup CheckBill(string id)
    {
        return new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Проверить оплату",
                $"bill_{id}"));
    }

    public static InlineKeyboardMarkup Email(string email)
    {
        return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
        {
            new()
                { InlineKeyboardButton.WithCallbackData($"✉ Эл. адресс ({email})", "challengeEmail") },
            new()
                { InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry") },
            new() { InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu") }
        });
    }

    public static InlineKeyboardMarkup Phone(string number)
    {
        return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
        {
            new()
                { InlineKeyboardButton.WithCallbackData($"📲 Телефон ({number})", "challengePhone") },
            new()
                { InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry") },
            new() { InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu") }
        });
    }

    public static InlineKeyboardMarkup PhoneAndEmail(string email, string number)
    {
        return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
        {
            new()
                { InlineKeyboardButton.WithCallbackData($"📲 Телефон ({number})", "challengePhone") },
            new()
                { InlineKeyboardButton.WithCallbackData($"✉ Эл. адресс ({email})", "challengeEmail") },
            new()
                { InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry") },
            new() { InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu") }
        });
    }
}