using System;
using System.ComponentModel.DataAnnotations.Schema;
using InstagramApiSharp.API;

namespace Insta.Model;

public class Instagram
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string StateData { get; set; }
    public bool IsDeactivated { get; set; }
    public User User { get; set; }
    public DateTime Block { get; set; }
    [NotMapped] public IInstaApi Api { get; set; }
    [NotMapped] public Proxy Proxy { get; set; }
}