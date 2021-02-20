using System;
using System.Collections.Generic;
using System.Text;
using Insta.Entities;
using Microsoft.EntityFrameworkCore;

namespace Insta
{
    internal sealed class Db:DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Instagram> Instagrams { get; set; }
        public DbSet<Subscribe> Subscribes { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<WorkTask> Works { get; set; }
        public DbSet<Proxy> Proxies { get; set; }

        public Db()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=users.sqlite");
        }
    }
}
