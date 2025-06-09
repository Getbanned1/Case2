using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Case2
{

    public class AppDbContext : DbContext
    {
        
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<UserChat> UserChats => Set<UserChat>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Таблица users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasColumnName("id");
                entity.Property(u => u.Username).HasColumnName("username").IsRequired().HasMaxLength(100);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
                entity.Property(u => u.LastOnline).HasColumnName("last_online");
                entity.Property(u => u.IsOnline).HasColumnName("is_online").IsRequired();
                entity.Property(u => u.AvatarUrl).HasColumnName("avatar_url");
            });

            // Таблица chats
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.ToTable("chats");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).HasColumnName("id");
                entity.Property(c => c.Name).HasColumnName("name");
                entity.Property(c => c.IsGroup).HasColumnName("is_group").IsRequired();
                entity.Property(c => c.AvatarUrl).HasColumnName("avatar_url");
            });

            // Таблица messages
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("messages");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Id).HasColumnName("id");
                entity.Property(m => m.ChatId).HasColumnName("chat_id").IsRequired();
                entity.Property(m => m.SenderId).HasColumnName("sender_id").IsRequired();
                entity.Property(m => m.Text).HasColumnName("text").IsRequired();
                entity.Property(m => m.SentAt).HasColumnName("sent_at").IsRequired();
                entity.Property(m => m.IsRead).HasColumnName("is_read").IsRequired();

                entity.HasOne(m => m.Chat)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(m => m.ChatId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                      .WithMany(u => u.MessagesSent)
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Таблица user_chats (многие-ко-многим)
            modelBuilder.Entity<UserChat>(entity =>
            {

                entity.ToTable("user_chats");
                
                entity.HasKey(uc => new { uc.UserId, uc.ChatId });

                entity.Property(uc => uc.UserId).HasColumnName("user_id");
                entity.Property(uc => uc.ChatId).HasColumnName("chat_id");

                entity.HasOne(uc => uc.User)
                      .WithMany(u => u.UserChats)
                      .HasForeignKey(uc => uc.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uc => uc.Chat)
                      .WithMany(c => c.UserChats)
                      .HasForeignKey(uc => uc.ChatId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
