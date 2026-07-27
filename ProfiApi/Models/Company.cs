using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("company", Schema = "Profi1")]
public partial class Company
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(20)]
    public string? Title { get; set; }

    [InverseProperty("Company")]
    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();
}
