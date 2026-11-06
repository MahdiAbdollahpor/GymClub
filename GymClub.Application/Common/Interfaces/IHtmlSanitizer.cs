namespace GymClub.Application.Common.Interfaces;

public interface IHtmlSanitizer
{
    string Sanitize(string html);
}