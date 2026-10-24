using System.ComponentModel.DataAnnotations;
using GymClub.Application.Products.Dtos;
using Microsoft.AspNetCore.Http;

namespace GymClub.Web.Areas.Admin.Models;

public class ProductFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "توضیحات الزامی است")]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "دسته‌بندی الزامی است")]
    public Guid CategoryId { get; set; }

    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "قیمت باید بزرگ‌تر از صفر باشد")]
    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;

    // عکس‌ها — ایجاد
    public List<IFormFile>? Images { get; set; }

    // عکس‌ها — ویرایش
    public List<IFormFile>? NewImages { get; set; }
    public List<ProductImageDto>? ExistingImages { get; set; }
    public List<Guid>? RemoveImageIds { get; set; }
    public Guid? SetMainImageId { get; set; }

    // ویژگی‌ها
    public List<ProductAttributeInput>? Attributes { get; set; } = new();
}

public class ProductAttributeInput
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}