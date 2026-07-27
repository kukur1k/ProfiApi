using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("technologies", Schema = "Profi1")]
[Index("Name", Name = "technologies_name_key", IsUnique = true)]
public partial class Technology
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [InverseProperty("Technology")]
    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
