using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("users", Schema = "Profi1")]
[Index("Email", Name = "users_email_key", IsUnique = true)]
[Index("Phone", Name = "users_phone_key", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("last_name")]
    [StringLength(100)]
    public string? LastName { get; set; }

    [Column("first_name")]
    [StringLength(100)]
    public string? FirstName { get; set; }

    [Column("middle_name")]
    [StringLength(100)]
    public string? MiddleName { get; set; }

    [Column("email")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(12)]
    public string? Phone { get; set; }

    [Column("id_role")]
    public int? IdRole { get; set; }

    [Column("registered_at", TypeName = "timestamp without time zone")]
    public DateTime? RegisteredAt { get; set; }

    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [InverseProperty("Requester")]
    public virtual ICollection<Confirmation> ConfirmationRequesters { get; set; } = new List<Confirmation>();

    [InverseProperty("Target")]
    public virtual ICollection<Confirmation> ConfirmationTargets { get; set; } = new List<Confirmation>();

    [InverseProperty("User")]
    public virtual ICollection<Education> Educations { get; set; } = new List<Education>();

    [InverseProperty("User")]
    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();

    [ForeignKey("IdRole")]
    [InverseProperty("Users")]
    public virtual Role? IdRoleNavigation { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("User")]
    public virtual ICollection<ShortlistCandidate> ShortlistCandidates { get; set; } = new List<ShortlistCandidate>();

    [InverseProperty("Owner")]
    public virtual ICollection<Shortlist> Shortlists { get; set; } = new List<Shortlist>();

    [InverseProperty("User")]
    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();

    [InverseProperty("User")]
    public virtual UserPin? UserPin { get; set; }
}
