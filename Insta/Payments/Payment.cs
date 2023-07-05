namespace Insta.Payments;

public class Payment
{
    public Payment(string billId, string payUrl)
    {
        BillId = billId;
        PayUrl = payUrl;
    }

    public string BillId { get; }
    public string PayUrl { get; }
}