using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace Insta
{
    public static class Keyboards
    {
        public static readonly ReplyKeyboardMarkup MainKeyboard = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>
        {
            new List<KeyboardButton>() {new KeyboardButton("🌇 Мои аккаунты"), new KeyboardButton("❤ Отработка")},
            new List<KeyboardButton>() {new KeyboardButton("💰 Оплатить подписку"),new KeyboardButton("⏱ Мои подписки")},
            new List<KeyboardButton>() {new KeyboardButton("📄 Инструкция"), new KeyboardButton("🤝 Поддержка")}
        });

        public static readonly string[] Emodji =
            {"🏞", "🏔", "🏖", "🌋", "🏜", "🏕", "🌎", "🗽", "🌃", "☘", "🐲", "🌸", "🌓", "🍃", "☀", "☁"};

        public static readonly InlineKeyboardMarkup Back =
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🔙 Назад", "back"));

        public static readonly InlineKeyboardMarkup Main =
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu"));

        public static readonly InlineKeyboardMarkup Working = new InlineKeyboardMarkup(
            new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("🏃 Начать отработку", "startWorking")},
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("⚙ Активные отработки", "stopWorking")}
            });

        public static readonly InlineKeyboardMarkup SelectMode = new InlineKeyboardMarkup(
            new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("❤ Лайки", "startLike"),InlineKeyboardButton.WithCallbackData("💾 Сохранения", "startSave")},
                //new List<InlineKeyboardButton>
                   //{InlineKeyboardButton.WithCallbackData("💾 Сохранения", "stopWorking")},
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("☑ Лайки + сохранения", "startAll")},
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
            });
        public static readonly InlineKeyboardMarkup StartWork = new InlineKeyboardMarkup(
            new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("🏃 Начать сейчас", "startNow"),
                    InlineKeyboardButton.WithCallbackData("⌛ Начать позже", "startLater")
                },
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("🛑 Отмена", "mainMenu")}
            });


        public static InlineKeyboardMarkup Select(long id)
        {
            return new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Выбрать", $"select_{id}"));
        }

        public static InlineKeyboardMarkup EnterData = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("🖊 Ввести данные", "enterData"));

        public static InlineKeyboardMarkup Cancel(long id)
        {
            return new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🛑 Отменить", $"cancel_{id}"));
        }

        public static InlineKeyboardMarkup Exit(long id)
        {
            return new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🚪 Выйти", $"exit_{id}"));
        }

        public static InlineKeyboardMarkup CheckBill(string id)
        {
            return new InlineKeyboardMarkup(
                new List<List<InlineKeyboardButton>>()
                {
                    new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData("Проверить оплату",
                            $"bill_{id}")
                    },
                    new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")
                    }
                });
        }
        public static InlineKeyboardMarkup Email(string email)
        {
            return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>()
                    {InlineKeyboardButton.WithCallbackData($"✉ Эл. адресс ({email})", "challengeEmail")},
                new List<InlineKeyboardButton>()
                    {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                new List<InlineKeyboardButton> {InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
            });
        }
        public static InlineKeyboardMarkup Phone(string number)
        {
            return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>()
                    {InlineKeyboardButton.WithCallbackData($"📲 Телефон ({number})", "challengePhone")},
                new List<InlineKeyboardButton>()
                    {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                new List<InlineKeyboardButton> {InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
            });
        }
        public static InlineKeyboardMarkup PhoneAndEmail(string email, string number)
        {
            return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>()
                    {InlineKeyboardButton.WithCallbackData($"📲 Телефон ({number})", "challengePhone")},
                new List<InlineKeyboardButton>()
                    {InlineKeyboardButton.WithCallbackData($"✉ Эл. адресс ({email})", "challengeEmail")},
                new List<InlineKeyboardButton>()
                    {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                new List<InlineKeyboardButton> {InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
            });
        }
    }
    
}