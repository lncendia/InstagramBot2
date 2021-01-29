﻿using System;

namespace Insta
{
    public class Subscribe
    {
        public int Id { get; set; }
        public User User { get; set; }
        public DateTime EndSubscribe { get; set; } = DateTime.Now.AddDays(30);
    }
}