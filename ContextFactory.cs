using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramExchangeDB
{
    internal class ContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        public ApplicationContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            var config = builder.Build();

            
            string dbConnectionString = config.GetConnectionString("DefaultConnection");
            var dbOptionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            var dbOptions = dbOptionsBuilder.UseSqlite(dbConnectionString).Options;

            var context = new ApplicationContext(dbOptions);

            return context;
        }
    }
}
