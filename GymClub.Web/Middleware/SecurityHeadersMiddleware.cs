namespace GymClub.Web.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // جلوگیری از clickjacking
        headers["X-Frame-Options"] = "SAMEORIGIN";

        // جلوگیری از MIME sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Referrer Policy
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions Policy
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // HSTS (فقط در Production)
        if (context.Request.IsHttps && !context.Request.Host.Host.Contains("localhost"))
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // CSP — مراقب باش Tailwind CDN و Bootstrap CDN اجازه داشته باشند
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.tailwindcss.com https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com data:; " +
            "img-src 'self' data: https:; " +
            "frame-src 'self' https://www.openstreetmap.org https://sandbox.zarinpal.com https://www.zarinpal.com; " +
            "connect-src 'self' https://sandbox.zarinpal.com https://api.zarinpal.com; " +
            "object-src 'none'; " +
            "base-uri 'self';";

        await _next(context);
    }
}