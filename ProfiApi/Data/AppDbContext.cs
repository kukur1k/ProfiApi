using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ProfiApi.Models;

namespace ProfiApi.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Confirmation> Confirmations { get; set; }

    public virtual DbSet<EduInstitution> EduInstitutions { get; set; }

    public virtual DbSet<EducaitonType> EducaitonTypes { get; set; }

    public virtual DbSet<Education> Educations { get; set; }

    public virtual DbSet<EmpType> EmpTypes { get; set; }

    public virtual DbSet<Experience> Experiences { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Shortlist> Shortlists { get; set; }

    public virtual DbSet<ShortlistCandidate> ShortlistCandidates { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<Technology> Technologies { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPin> UserPins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //===Схема в БД Profi1===
        modelBuilder.HasDefaultSchema("Profi1");
        modelBuilder.HasPostgresExtension("pg_catalog", "adminpack");

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("company_pkey");
        });

        modelBuilder.Entity<Confirmation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("confirmation_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Requester).WithMany(p => p.ConfirmationRequesters).HasConstraintName("confirmation_requester_id_fkey");

            entity.HasOne(d => d.Skill).WithMany(p => p.Confirmations).HasConstraintName("confirmation_skill_id_fkey");

            entity.HasOne(d => d.Target).WithMany(p => p.ConfirmationTargets).HasConstraintName("confirmation_target_id_fkey");
        });

        modelBuilder.Entity<EduInstitution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("edu_institution_pkey");
        });

        modelBuilder.Entity<EducaitonType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("educaiton_type_pkey");
        });

        modelBuilder.Entity<Education>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("education_pkey");

            entity.HasOne(d => d.EduInstitution).WithMany(p => p.Educations).HasConstraintName("education_edu_institution_id_fkey");

            entity.HasOne(d => d.EduType).WithMany(p => p.Educations).HasConstraintName("education_edu_type_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Educations).HasConstraintName("education_user_id_fkey");
        });

        modelBuilder.Entity<EmpType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("emp_type_pkey");
        });

        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("experience_pkey");

            entity.HasOne(d => d.Company).WithMany(p => p.Experiences).HasConstraintName("experience_company_id_fkey");

            entity.HasOne(d => d.EmpType).WithMany(p => p.Experiences).HasConstraintName("experience_emp_type_id_fkey");

            entity.HasOne(d => d.Position).WithMany(p => p.Experiences).HasConstraintName("experience_position_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Experiences).HasConstraintName("experience_users_fk");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("position_pkey");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rating_pkey");

            entity.Property(e => e.CalculateAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Ratings)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("rating_user_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_pkey");
        });

        modelBuilder.Entity<Shortlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shortlists_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Owner).WithMany(p => p.Shortlists)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("shortlists_owner_id_fkey");
        });

        modelBuilder.Entity<ShortlistCandidate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shortlist_candidates_pkey");

            entity.Property(e => e.AddedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Shortlist).WithMany(p => p.ShortlistCandidates).HasConstraintName("shortlist_candidates_shortlist_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ShortlistCandidates).HasConstraintName("shortlist_candidates_user_id_fkey");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("skills_pkey");

            entity.HasOne(d => d.Technology).WithMany(p => p.Skills).HasConstraintName("skills_technology_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Skills).HasConstraintName("skills_user_id_fkey");
        });

        modelBuilder.Entity<Technology>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("technologies_pkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.Property(e => e.IdRole).HasDefaultValue(2);
            entity.Property(e => e.RegisteredAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.IdRoleNavigation).WithMany(p => p.Users).HasConstraintName("users_id_role_fkey");
        });

        modelBuilder.Entity<UserPin>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_pins_pkey");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithOne(p => p.UserPin)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_pins_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
