using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Insta
{
    sealed class DB:DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Instagram> Instagrams { get; set; }
        public DbSet<Subscribe> Subscribes { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public DB()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=users.sqlite");
        }
    }
}
