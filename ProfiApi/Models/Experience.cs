using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("experience", Schema = "Profi1")]
public partial class Experience
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("emp_type_id")]
    public int? EmpTypeId { get; set; }

    [Column("company_id")]
    public int? CompanyId { get; set; }

    [Column("position_id")]
    public int? PositionId { get; set; }

    [Column("date_start")]
    public DateOnly? DateStart { get; set; }

    [Column("date_end")]
    public DateOnly? DateEnd { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("Experiences")]
    public virtual Company? Company { get; set; }

    [ForeignKey("EmpTypeId")]
    [InverseProperty("Experiences")]
    public virtual EmpType? EmpType { get; set; }

    [ForeignKey("PositionId")]
    [InverseProperty("Experiences")]
    public virtual Position? Position { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Experiences")]
    public virtual User? User { get; set; }
}
