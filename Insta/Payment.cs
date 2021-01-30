using System;
using System.Linq;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Model;
using Qiwi.BillPayments.Model.In;
using Telegram.Bot;

namespace Insta
{
    internal static class Payment
    {
        private static readonly BillPaymentsClient Client = BillPaymentsClientFactory.Create("eyJ2ZXJzaW9uIjoiUDJQIiwiZGF0YSI6eyJwYXlpbl9tZXJjaGFudF9zaXRlX3VpZCI6Imk0ZmVmNS0wMCIsInVzZXJfaWQiOiI3OTI2NjY2Mzk0MSIsInNlY3JldCI6ImUzZjI1YWNjZDJhZTA5YzJjZjNlMDUwMGI1YjM4NDdmMTljZjA4YjU4MjNjYmQ3M2IyY2Q2Mzk4ODE4NGE1YTcifX0=");

        public static string AddTransaction(int sum, User user, ref string billId)
        {
            try
            {
                var response = Client.CreateBill(
                    info: new CreateBillInfo
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
                        }
                    });
                billId = response.BillId;
                return response.PayUrl.ToString();
            }
            catch(Exception)
            {
                return null;
            }
        }
        public static bool CheckPay(User user, string billId)
        {
            try
            {
                using var db = new DB();
                var response = Client.GetBillInfo(billId);
                if (response.Status.ValueEnum != BillStatusEnum.Paid) return false;
                db.Update(user);
                db.Add(new Transaction(){Amount = (int)response.Amount.ValueDecimal, User=user});
                db.SaveChanges();
                for (var i = (int) response.Amount.ValueDecimal / 120; i > 0; i--)
                {
                    db.Add(new Subscribe() {User = user});
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
}
