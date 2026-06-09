using Microsoft.EntityFrameworkCore;
using WorkflowApp.Api.Domain.Entities;


namespace WorkflowApp.Api.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public AppDbContext()
        {
        }

        // ユーザーのDbSetを追加
        public DbSet<User> Users => Set<User>();

        // ワークフロー申請のDbSetを追加   
        public DbSet<Application> Applications => Set<Application>();

        // 承認ステップのDbSetを追加
        public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();

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
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

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

            // ApplicationエンティティのStatusプロパティに対して、WorkflowStatus列挙型を文字列として保存するように変換を設定
            modelBuilder.Entity<Application>(entity =>
            {
                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .IsRequired();
            });

            // 申請と承認ステップの関連の設定
            modelBuilder.Entity<Application>()
                .HasMany(a => a.ApprovalSteps)          // Applicationは複数のApprovalStepを持ち、ApprovalStepは1つのApplicationに属する
                .WithOne(s => s.Application)
                .HasForeignKey(s => s.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);      // 申請を削除したら、紐づく承認ステップも削除する

            // 承認ステップと承認者ユーザーの関連の設定
            modelBuilder.Entity<ApprovalStep>()
                .HasOne(s => s.ApproverUser)            // ApprovalStepは承認者Userを1人持つ
                .WithMany()
                .HasForeignKey(s => s.ApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);     // Userが削除されても承認ステップを削除しない

            // ApprovalStepエンティティのStatusプロパティに対して、ApprovalStepStatus列挙型を文字列として保存するように変換を設定
            modelBuilder.Entity<ApprovalStep>(entity =>
            {
                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .IsRequired();
            });

            // ApplicationIdとStepOrderの組み合わせに対して一意制約を設定
            modelBuilder.Entity<ApprovalStep>()
                .HasIndex(s => new { s.ApplicationId, s.StepOrder })
                .IsUnique();
        }
    }
}
