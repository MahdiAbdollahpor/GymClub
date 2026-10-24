using FluentValidation;
using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using GymClub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Description,
    Guid CategoryId,
    decimal Price,
    decimal? DiscountPrice,
    int Stock,
    bool IsActive,
    List<NewProductImage> Images,
    List<NewProductAttribute> Attributes
) : IRequest<Result<Guid>>;

public class NewProductImage
{
    public Stream Stream { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public bool IsMain { get; set; }
}

public class NewProductAttribute
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPrice)
            .LessThan(x => x.Price).When(x => x.DiscountPrice.HasValue)
            .WithMessage("قیمت با تخفیف باید کمتر از قیمت اصلی باشد");
        RuleForEach(x => x.Attributes)
            .ChildRules(a =>
            {
                a.RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
                a.RuleFor(x => x.Value).NotEmpty().MaximumLength(500);
            });
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorageService _images;
    private readonly IDateTimeProvider _clock;

    public CreateProductCommandHandler(IApplicationDbContext db, IImageStorageService images, IDateTimeProvider clock)
    {
        _db = db; _images = images; _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand req, CancellationToken ct)
    {
        if (!await _db.ProductCategories.AnyAsync(c => c.Id == req.CategoryId && !c.IsDeleted, ct))
            return Result<Guid>.Fail("دسته‌بندی یافت نشد");

        var slugBase = new string(req.Name.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) && c < 128).Take(40).ToArray());
        if (string.IsNullOrWhiteSpace(slugBase)) slugBase = "product";
        var slug = $"{slugBase}-{Guid.NewGuid():N}".Substring(0, Math.Min(60, slugBase.Length + 1 + 8));
        while (await _db.Products.AnyAsync(p => p.Slug == slug, ct))
            slug = $"{slugBase}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        var product = new Product
        {
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description.Trim(),
            CategoryId = req.CategoryId,
            Price = req.Price,
            DiscountPrice = req.DiscountPrice,
            Stock = req.Stock,
            IsActive = req.IsActive
        };

        try
        {
            var order = 0;
            foreach (var img in req.Images)
            {
                var url = await _images.UploadAsync(img.Stream, img.FileName, img.ContentType, ct);
                product.Images.Add(new ProductImage
                {
                    Url = url,
                    SortOrder = order++,
                    IsMain = img.IsMain || order == 1
                });
            }
        }
        catch (Exception ex)
        {
            return Result<Guid>.Fail($"خطا در آپلود عکس: {ex.Message}");
        }

        var attrOrder = 0;
        foreach (var attr in req.Attributes)
            product.Attributes.Add(new ProductAttribute
            {
                Title = attr.Title.Trim(),
                Value = attr.Value.Trim(),
                SortOrder = attrOrder++
            });

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(product.Id);
    }
}