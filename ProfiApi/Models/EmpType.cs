using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("emp_type", Schema = "Profi1")]
public partial class EmpType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(20)]
    public string? Title { get; set; }

    [InverseProperty("EmpType")]
    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();
}
