using System;
using System.Linq;
using Insta.Bot;
using Insta.Model;
using Newtonsoft.Json.Linq;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Model;
using Qiwi.BillPayments.Model.In;
using RestSharp;

namespace Insta.Payments;

internal class Payment
{
    private readonly BillPaymentsClient _client = BillPaymentsClientFactory.Create(BotSettings.Cfg.QiwiToken);

    public string AddTransaction(int sum, User user, int countSubscribes, ref string billId)
    {
        try
        {
            var response = _client.CreateBill(
                new CreateBillInfo
                {
                    BillId = Guid.NewGuid().ToString(),
                    Amount = new MoneyAmount
                    {
                        ValueDecimal = sum,
                        CurrencyEnum = CurrencyEnum.Rub
                    },
                    ExpirationDateTime = DateTime.Now.AddDays(5),
                    Customer = new Customer
                    {
                        Account = user.Id.ToString()
                    },
                    Comment = $"Оплата {countSubscribes.ToString()} подписок в LikeBot."
                });
            billId = response.BillId;
            return response.PayUrl.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private readonly RestClient _httpClient = new();

    public bool CheckPay(User user, string billId)
    {
        try
        {
            var request = new RestRequest($"https://api.qiwi.com/partner/bill/v1/bills/{billId}");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {BotSettings.Cfg.QiwiToken}");
            var response = _httpClient.Execute(request);
            dynamic jObject = JObject.Parse(response.Content!);
            if (jObject.status.value != "PAID") return false;
            var amount = (int) decimal.Parse(jObject.amount.value.ToString().Replace('.', ','));
            using var db = new Db();
            int count = int.Parse(jObject.comment.ToString().Split(' ')[1]);
            db.Update(user);
            db.Add(new Transaction
            {
                Amount = amount, User = user, DateTime = DateTime.Parse(jObject.status.changedDateTime.ToString())
            });
            for (var i = count; i > 0; i--)
            {
                db.Add(new Subscribe {User = user});
                var inst = user.Instagrams.ToList().FirstOrDefault(x => x.IsDeactivated);
                if (inst == null) continue;
                inst.IsDeactivated = false;
            }

            db.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }
}