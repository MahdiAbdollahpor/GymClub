using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Application.Products.Commands.CreateProduct;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    Guid CategoryId,
    decimal Price,
    decimal? DiscountPrice,
    int Stock,
    bool IsActive,
    List<NewProductImage>? NewImages,
    List<NewProductAttribute> Attributes,
    List<Guid>? RemoveImageIds,
    Guid? SetMainImageId
) : IRequest<Result>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPrice)
            .LessThan(x => x.Price).When(x => x.DiscountPrice.HasValue)
            .WithMessage("قیمت با تخفیف باید کمتر از قیمت اصلی باشد");
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorageService _images;
    private readonly IDateTimeProvider _clock;

    public UpdateProductCommandHandler(IApplicationDbContext db, IImageStorageService images, IDateTimeProvider clock)
    {
        _db = db; _images = images; _clock = clock;
    }

    public async Task<Result> Handle(UpdateProductCommand req, CancellationToken ct)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.Id == req.Id && !p.IsDeleted, ct);
        if (product is null) return Result.Fail("محصول یافت نشد");

        if (!await _db.ProductCategories.AnyAsync(c => c.Id == req.CategoryId && !c.IsDeleted, ct))
            return Result.Fail("دسته‌بندی یافت نشد");

        product.Name = req.Name.Trim();
        product.Description = req.Description.Trim();
        product.CategoryId = req.CategoryId;
        product.Price = req.Price;
        product.DiscountPrice = req.DiscountPrice;
        product.Stock = req.Stock;
        product.IsActive = req.IsActive;
        product.UpdatedAt = _clock.UtcNow;

        // حذف عکس‌ها
        if (req.RemoveImageIds?.Any() == true)
        {
            var toRemove = product.Images.Where(i => req.RemoveImageIds.Contains(i.Id)).ToList();
            foreach (var img in toRemove)
            {
                await _images.DeleteAsync(img.Url, ct);
                product.Images.Remove(img);
            }
        }

        // عکس‌های جدید
        if (req.NewImages?.Any() == true)
        {
            var maxOrder = product.Images.Any() ? product.Images.Max(i => i.SortOrder) : -1;
            foreach (var img in req.NewImages)
            {
                var url = await _images.UploadAsync(img.Stream, img.FileName, img.ContentType, ct);
                var newImage = new ProductImage
                {
                    ProductId = product.Id, // اطمینان حاصل کنید که کلید خارجی مقداردهی شده است
                    Url = url,
                    SortOrder = ++maxOrder,
                    IsMain = img.IsMain
                };

                _db.ProductImages.Add(newImage);
            }
        }

        // تنظیم عکس اصلی
        if (req.SetMainImageId.HasValue)
        {
            foreach (var img in product.Images)
                img.IsMain = img.Id == req.SetMainImageId.Value;
        }
        if (!product.Images.Any(i => i.IsMain) && product.Images.Any())
            product.Images.OrderBy(i => i.SortOrder).First().IsMain = true;

        // ویژگی‌ها: حذف و بازنویسی کامل (ساده‌ترین)
        _db.ProductAttributes.RemoveRange(product.Attributes);
        var order = 0;
        foreach (var a in req.Attributes)
            _db.ProductAttributes.Add(new ProductAttribute
            {
                ProductId = product.Id,
                Title = a.Title.Trim(),
                Value = a.Value.Trim(),
                SortOrder = order++
            });

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}