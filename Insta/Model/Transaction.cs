using System;

namespace Insta.Model;

public class Transaction
{
    public int Id { get; set; }
    public int Amount { get; set; }
    public User User { get; set; }
    public DateTime DateTime { get; set; }
}