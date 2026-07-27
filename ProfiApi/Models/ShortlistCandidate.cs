using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("shortlist_candidates", Schema = "Profi1")]
[Index("ShortlistId", "UserId", Name = "shortlist_candidates_shortlist_id_user_id_key", IsUnique = true)]
public partial class ShortlistCandidate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("shortlist_id")]
    public int? ShortlistId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("added_at", TypeName = "timestamp without time zone")]
    public DateTime? AddedAt { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [ForeignKey("ShortlistId")]
    [InverseProperty("ShortlistCandidates")]
    public virtual Shortlist? Shortlist { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ShortlistCandidates")]
    public virtual User? User { get; set; }
}
