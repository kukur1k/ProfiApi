using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("confirmation", Schema = "Profi1")]
public partial class Confirmation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("requester_id")]
    public int? RequesterId { get; set; }

    [Column("target_id")]
    public int? TargetId { get; set; }

    [Column("skill_id")]
    public int? SkillId { get; set; }

    [Column("confirmed_level")]
    public short? ConfirmedLevel { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("responded_at", TypeName = "timestamp without time zone")]
    public DateTime? RespondedAt { get; set; }

    [ForeignKey("RequesterId")]
    [InverseProperty("ConfirmationRequesters")]
    public virtual User? Requester { get; set; }

    [ForeignKey("SkillId")]
    [InverseProperty("Confirmations")]
    public virtual Skill? Skill { get; set; }

    [ForeignKey("TargetId")]
    [InverseProperty("ConfirmationTargets")]
    public virtual User? Target { get; set; }
}
