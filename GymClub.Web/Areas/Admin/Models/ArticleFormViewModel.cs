using System.ComponentModel.DataAnnotations;
using GymClub.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace GymClub.Web.Areas.Admin.Models;

public class ArticleFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "عنوان الزامی است")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "خلاصه الزامی است")]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "محتوا الزامی است")]
    public string ContentHtml { get; set; } = string.Empty;

    [Required]
    public ArticleType Type { get; set; } = ArticleType.Training;

    public Guid? CategoryId { get; set; }

    public bool Publish { get; set; }

    // فایل‌های جدید (اختیاری در ویرایش)
    public IFormFile? CoverImageFile { get; set; }
    public IFormFile? VideoFile { get; set; }

    // نمایش فایل‌های فعلی (در ویرایش)
    public string? CurrentCoverImageUrl { get; set; }
    public string? CurrentVideoUrl { get; set; }

    // حذف فایل‌های موجود
    public bool RemoveCoverImage { get; set; }
    public bool RemoveVideo { get; set; }
}