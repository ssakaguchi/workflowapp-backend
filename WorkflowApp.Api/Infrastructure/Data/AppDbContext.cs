using Microsoft.EntityFrameworkCore;
using WorkflowApp.Api.Domain.Entities;


namespace WorkflowApp.Api.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ユーザーのDbSetを追加
        public DbSet<User> Users=> Set<User>();
        
        // ワークフロー申請のDbSetを追加   
        public DbSet<Application> Applications => Set<Application>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.HasKey(e => e.Id);

                entity.Property(x => x.LoginId)
                .IsRequired()
                .HasMaxLength(50);

                entity.Property(x => x.DisplayName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.PasswordHash)
                    .IsRequired();

                entity.Property(x => x.Role)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.IsActive)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.Property(x => x.UpdatedAt)
                    .IsRequired();

                // ログインIDの一意制約を設定
                entity.HasIndex(x => x.LoginId)
                    .IsUnique();
            });
        }
    }
}
