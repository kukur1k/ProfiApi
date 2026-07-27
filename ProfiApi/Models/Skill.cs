using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("skills", Schema = "Profi1")]
[Index("UserId", "TechnologyId", Name = "us_user_tech", IsUnique = true)]
public partial class Skill
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("technology_id")]
    public int? TechnologyId { get; set; }

    [Column("skilllevel")]
    public short? Skilllevel { get; set; }

    [InverseProperty("Skill")]
    public virtual ICollection<Confirmation> Confirmations { get; set; } = new List<Confirmation>();

    [ForeignKey("TechnologyId")]
    [InverseProperty("Skills")]
    public virtual Technology? Technology { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Skills")]
    public virtual User? User { get; set; }
}
