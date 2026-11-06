using GymClub.Application.Common.Interfaces;
using Ganss.Xss;
using IHtmlSanitizer = GymClub.Application.Common.Interfaces.IHtmlSanitizer;

namespace GymClub.Infrastructure.Services.Security;

public class HtmlSanitizerService : IHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        // تگ‌های مجاز اضافی
        _sanitizer.AllowedTags.Add("iframe");
        _sanitizer.AllowedAttributes.Add("allowfullscreen");
        _sanitizer.AllowedAttributes.Add("frameborder");
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        return _sanitizer.Sanitize(html);
    }
}