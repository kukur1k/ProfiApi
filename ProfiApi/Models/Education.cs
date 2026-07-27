using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("education", Schema = "Profi1")]
public partial class Education
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("edu_type_id")]
    public int? EduTypeId { get; set; }

    [Column("edu_institution_id")]
    public int? EduInstitutionId { get; set; }

    [Column("date_start")]
    public DateOnly? DateStart { get; set; }

    [Column("date_end")]
    public DateOnly? DateEnd { get; set; }

    [Column("document_url")]
    [StringLength(500)]
    public string? DocumentUrl { get; set; }

    [ForeignKey("EduInstitutionId")]
    [InverseProperty("Educations")]
    public virtual EduInstitution? EduInstitution { get; set; }

    [ForeignKey("EduTypeId")]
    [InverseProperty("Educations")]
    public virtual EducaitonType? EduType { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Educations")]
    public virtual User? User { get; set; }
}
