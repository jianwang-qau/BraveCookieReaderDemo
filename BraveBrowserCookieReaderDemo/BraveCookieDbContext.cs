using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace BraveBrowserCookieReaderDemo
{
    public class Cookie
    {
        [Column("host_key")]
        public string HostKey { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("encrypted_value")]
        public byte[] EncryptedValue { get; set; }
    }

    public class BraveCookieDbContext : DbContext
    {
        public DbSet<Cookie> Cookies { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // var dbPath = Environment.GetFolderPath(
            //    Environment.SpecialFolder.LocalApplicationData) 
            //             + @"\Google\Chrome\User Data\Default\Cookies";

            var dbPath = Environment.GetFolderPath(
                             Environment.SpecialFolder.LocalApplicationData)
                         + @"\BraveSoftware\Brave-Browser\User Data\Default\Cookies";

            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
            string newFile = "";
            newFile = Path.GetRandomFileName();
            string tempFileName = localAppData + "\\Temp\\" + newFile;
            File.Copy(dbPath, tempFileName);
            dbPath = tempFileName;

            if (!System.IO.File.Exists(dbPath)) throw new System.IO.FileNotFoundException("Cant find cookie store", dbPath); // race condition, but i'll risk it

            var connectionString = "Data Source=" + dbPath + ";Mode=ReadOnly;";

            optionsBuilder
                .UseSqlite(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cookie>().ToTable("cookies").HasNoKey();
        }
    }
}
