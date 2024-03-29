﻿using System;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Payments;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class EnterCountSubscribesCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        if (!int.TryParse(message.Text, out var count))
        {
            await client.SendTextMessageAsync(message.From!.Id,
                "Введите число!", replyMarkup: Keyboards.Main);
            return;
        }

        if (count > 100)
        {
            await client.SendTextMessageAsync(message.From!.Id,
                "Слишком большое количество!", replyMarkup: Keyboards.Main);
            return;
        }

        var bonus = count * BotSettings.Cfg.Bonus >= user.Bonus ? user.Bonus : count * BotSettings.Cfg.Bonus;
        var payment = await new PaymentService().AddTransaction(count * BotSettings.Cfg.Cost - bonus, user);

        await using var db = new Db();
        db.Update(user);
        user.Bonus -= bonus;
        await db.SaveChangesAsync();
        user.State = State.main;

        await client.SendTextMessageAsync(message.From!.Id,
            $"💸 Оплата подписки на сумму {count * BotSettings.Cfg.Cost}₽ из которых {bonus}₽ списанно с бонусного счета.\n📆 Дата: {DateTime.Now:dd.MMM.yyyy}\n❌ Статус: Не оплачено.\n\n💳 Оплатите счет по ссылке.\n{payment.PayUrl}",
            replyMarkup: Keyboards.CheckBill(payment.BillId));
    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && user.State == State.enterCountToBuy;
    }
}