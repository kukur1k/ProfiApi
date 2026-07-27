using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("educaiton_type", Schema = "Profi1")]
public partial class EducaitonType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(20)]
    public string? Title { get; set; }

    [InverseProperty("EduType")]
    public virtual ICollection<Education> Educations { get; set; } = new List<Education>();
}
