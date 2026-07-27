using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("shortlists", Schema = "Profi1")]
public partial class Shortlist
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("owner_id")]
    public int OwnerId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("OwnerId")]
    [InverseProperty("Shortlists")]
    public virtual User Owner { get; set; } = null!;

    [InverseProperty("Shortlist")]
    public virtual ICollection<ShortlistCandidate> ShortlistCandidates { get; set; } = new List<ShortlistCandidate>();
}
