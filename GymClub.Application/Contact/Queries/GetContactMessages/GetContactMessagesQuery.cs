using GymClub.Application.Common.Interfaces;
using GymClub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Contact.Queries.GetContactMessages;

public class ContactMessageDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AdminReply { get; set; }
}

public record GetContactMessagesQuery(bool? IsRead = null, string? Search = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ContactMessageDto>>;

public class GetContactMessagesHandler
    : IRequestHandler<GetContactMessagesQuery, PagedResult<ContactMessageDto>>
{
    private readonly IApplicationDbContext _db;
    public GetContactMessagesHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ContactMessageDto>> Handle(GetContactMessagesQuery req, CancellationToken ct)
    {
        var q = _db.ContactMessages.AsNoTracking().AsQueryable();

        if (req.IsRead.HasValue)
            q = q.Where(m => m.IsRead == req.IsRead);

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(m => m.FullName.Contains(req.Search)
                          || m.PhoneNumber.Contains(req.Search)
                          || m.Subject.Contains(req.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(m => m.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(m => new ContactMessageDto
            {
                Id = m.Id,
                FullName = m.FullName,
                PhoneNumber = m.PhoneNumber,
                Email = m.Email,
                Subject = m.Subject,
                Message = m.Message,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt,
                AdminReply = m.AdminReply
            })
            .ToListAsync(ct);

        return new PagedResult<ContactMessageDto>
        {
            Items = items,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}