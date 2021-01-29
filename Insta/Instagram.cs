using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using InstagramApiSharp.API;

namespace Insta
{
    public class Instagram
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsDeactivated { get; set; } = false;
        public User User { get; set; }
        [NotMapped]
        public IInstaApi api { get; set; }
    }
}
