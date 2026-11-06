using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GymClub.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
        var userManager = sp.GetRequiredService<UserManager<User>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var db = sp.GetRequiredService<AppDbContext>();

        // ۱. نقش‌ها
        foreach (var role in new[] { "User", "ContentWriter", "ShopManager", "Trainer", "Admin", "SuperAdmin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        // ۲. سوپر ادمین
        await SeedSuperAdmin(userManager, logger);

        // ۳. کاربران نمونه
        await SeedSampleUsers(userManager, logger);

        // ۴. دسته‌بندی مقالات
        var articleCategories = await SeedArticleCategories(db, logger);

        // ۵. مقالات
        await SeedArticles(db, articleCategories, userManager, logger);

        // ۶. دسته‌بندی محصولات
        var productCategories = await SeedProductCategories(db, logger);

        // ۷. محصولات
        await SeedProducts(db, productCategories, logger);

        // ۸. پرسشنامه ورزشی
        await SeedWorkoutQuestionnaire(db, logger);

        await db.SaveChangesAsync();
        logger.LogInformation("✅ Seeder completed.");
    }

    // ═══════════════════════════════════════════════════════════
    // سوپر ادمین
    // ═══════════════════════════════════════════════════════════
    private static async Task SeedSuperAdmin(UserManager<User> userManager, ILogger logger)
    {
        const string phone = "09120000000";
        const string password = "SuperAdmin@123";

        var user = await userManager.FindByNameAsync(phone);
        if (user is null)
        {
            user = new User
            {
                UserName = phone,
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                IsPhoneConfirmed = true,
                FullName = "Super Admin",
                DisplayUserName = "superadmin",
                Role = UserRole.SuperAdmin,
                IsActive = true
            };
            var r = await userManager.CreateAsync(user, password);
            if (r.Succeeded)
                await userManager.AddToRoleAsync(user, "SuperAdmin");
            logger.LogInformation("SuperAdmin created: {Phone}", phone);
        }
        else
        {
            if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
                await userManager.AddToRoleAsync(user, "SuperAdmin");
            if (!user.IsActive)
            {
                user.IsActive = true;
                await userManager.UpdateAsync(user);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // کاربران نمونه
    // ═══════════════════════════════════════════════════════════
    private static async Task SeedSampleUsers(UserManager<User> userManager, ILogger logger)
    {
        var samples = new (string phone, string display, string fullName, UserRole role, string password)[]
        {
            ("09121111111", "admin",     "ادمین کل",       UserRole.Admin,         "Admin@123"),
            ("09121111112", "writer",    "نویسنده محتوا",  UserRole.ContentWriter, "Writer@123"),
            ("09121111113", "shop",      "مدیر فروشگاه",   UserRole.ShopManager,   "Shop@123"),
            ("09121111114", "trainer",   "مربی باشگاه",    UserRole.Trainer,       "Trainer@123"),
            ("09121111115", "user",      "کاربر عادی",     UserRole.User,          "User@123"),
        };

        foreach (var s in samples)
        {
            if (await userManager.FindByNameAsync(s.phone) is not null) continue;

            var u = new User
            {
                UserName = s.phone,
                PhoneNumber = s.phone,
                PhoneNumberConfirmed = true,
                IsPhoneConfirmed = true,
                DisplayUserName = s.display,
                FullName = s.fullName,
                Role = s.role,
                IsActive = true
            };
            var r = await userManager.CreateAsync(u, s.password);
            if (r.Succeeded)
            {
                var roleName = s.role switch
                {
                    UserRole.Admin => "Admin",
                    UserRole.ContentWriter => "ContentWriter",
                    UserRole.ShopManager => "ShopManager",
                    UserRole.Trainer => "Trainer",
                    _ => "User"
                };
                await userManager.AddToRoleAsync(u, roleName);
                logger.LogInformation("Sample user created: {Phone} ({Role})", s.phone, s.role);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // دسته‌بندی مقالات
    // ═══════════════════════════════════════════════════════════
    private static async Task<Dictionary<string, ArticleCategory>> SeedArticleCategories(
        AppDbContext db, ILogger logger)
    {
        if (await db.ArticleCategories.AnyAsync())
            return await db.ArticleCategories.ToDictionaryAsync(c => c.Title, c => c);

        var list = new List<ArticleCategory>
        {
            new() { Title = "تمرین", Slug = "training", SortOrder = 1 },
            new() { Title = "تغذیه", Slug = "nutrition", SortOrder = 2 },
            new() { Title = "معرفی باشگاه", Slug = "club-intro", SortOrder = 3 },
            new() { Title = "سلامت و ریکاوری", Slug = "health", SortOrder = 4 },
        };
        db.ArticleCategories.AddRange(list);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} article categories", list.Count);
        return list.ToDictionary(c => c.Title, c => c);
    }

    // ═══════════════════════════════════════════════════════════
    // مقالات نمونه
    // ═══════════════════════════════════════════════════════════
    private static async Task SeedArticles(
        AppDbContext db, Dictionary<string, ArticleCategory> cats,
        UserManager<User> userManager, ILogger logger)
    {
        if (await db.Articles.AnyAsync()) return;

        var author = await userManager.FindByNameAsync("09120000000");
        if (author is null) return;

        var articles = new List<Article>
        {
            new()
            {
                Title = "چطور با دستگاه اسمیت تمرین کنیم؟",
                Slug = "smith-machine-training",
                Summary = "راهنمای کامل استفاده از دستگاه اسمیت برای تمرین سینه، سرشانه و پا.",
                CategoryId = cats["تمرین"].Id,
                AuthorId = author.Id,
                Type = ArticleType.Training,
                Status = ArticleStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-10),
                CoverImageUrl = "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=800",
                ViewCount = 145,
                ContentHtml = @"<h2>دستگاه اسمیت چیست؟</h2>
<p>دستگاه اسمیت یک میله‌ی هالتر است که در ریل‌های عمودی حرکت می‌کند و امکان قفل کردن وزنه در هر ارتفاع را فراهم می‌کند. این دستگاه برای افراد مبتدی و حتی حرفه‌ای‌ها بسیار مفید است.</p>
<h2>مزایای تمرین با اسمیت</h2>
<ul>
<li>ایمنی بالا — می‌توانید وزنه را در هر لحظه قفل کنید</li>
<li>تمرکز روی عضله — نیاز به تعادل کمتر است</li>
<li>امکان تمرین با وزنه‌های سنگین‌تر</li>
</ul>
<h2>تمرینات پیشنهادی</h2>
<h3>پرس سینه اسمیت</h3>
<p>روی نیمکت دراز بکشید، میله را در سطح سینه بگیرید و به بالا پرس کنید. ۳ ست × ۱۰ تکرار.</p>
<h3>اسکوات اسمیت</h3>
<p>میله را روی شانه‌ها قرار دهید، پاها را به عرض شانه باز کنید و اسکوات بزنید. ۴ ست × ۸ تکرار.</p>
<blockquote>نکته: همیشه قبل از شروع، وزنه را با گیره محکم کنید.</blockquote>"
            },
            new()
            {
                Title = "پروتئین وی: چه زمانی و چقدر مصرف کنیم؟",
                Slug = "whey-protein-guide",
                Summary = "بهترین زمان مصرف پروتئین وی برای رشد عضلات و ریکاوری پس از تمرین.",
                CategoryId = cats["تغذیه"].Id,
                AuthorId = author.Id,
                Type = ArticleType.Nutrition,
                Status = ArticleStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-7),
                CoverImageUrl = "https://images.unsplash.com/photo-1593095948071-474c5cc2989d?w=800",
                ViewCount = 230,
                ContentHtml = @"<h2>پروتئین وی چیست؟</h2>
<p>پروتئین وی یک مکمل پروتئینی با جذب سریع است که از شیر گرفته می‌شود. هر وعده حاوی ۲۰-۲۵ گرم پروتئین خالص است.</p>
<h2>بهترین زمان مصرف</h2>
<ol>
<li><strong>بلافاصله پس از تمرین</strong> — پنجره‌ی طلایی برای جذب</li>
<li><strong>صبح پس از بیدار شدن</strong> — برای توقف کاتابولیسم</li>
<li><strong>قبل از خواب</strong> — در صورت نیاز به پروتئین بیشتر</li>
</ol>
<h2>مقدار مصرف</h2>
<p>معمولاً ۱-۲ پیمانه در روز کافی است. دوز کل پروتئین روزانه باید حدود ۱.۶-۲.۲ گرم به ازای هر کیلوگرم وزن بدن باشد.</p>
<blockquote>هشدار: اگر مشکل کلیوی دارید، قبل از مصرف با پزشک مشورت کنید.</blockquote>"
            },
            new()
            {
                Title = "چرا گرم کردن قبل از تمرین ضروری است؟",
                Slug = "why-warm-up-matters",
                Summary = "۵ دلیل علمی که چرا نباید گرم کردن را نادیده بگیرید.",
                CategoryId = cats["سلامت و ریکاوری"].Id,
                AuthorId = author.Id,
                Type = ArticleType.Training,
                Status = ArticleStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-3),
                CoverImageUrl = "https://images.unsplash.com/photo-1518611012118-696072aa579a?w=800",
                ViewCount = 89,
                ContentHtml = @"<h2>گرم کردن چیست؟</h2>
<p>گرم کردن مجموعه‌ای از حرکات سبک است که بدن را برای تمرین اصلی آماده می‌کند. معمولاً ۵-۱۰ دقیقه طول می‌کشد.</p>
<h2>چرا مهم است؟</h2>
<ul>
<li>افزایش جریان خون به عضلات</li>
<li>کاهش ریسک آسیب‌دیدگی</li>
<li>بهبود عملکرد ورزشی</li>
<li>آماده‌سازی سیستم عصبی</li>
<li>افزایش دامنه‌ی حرکتی</li>
</ul>
<h2>نمونه گرم کردن ۵ دقیقه‌ای</h2>
<ol>
<li>۲ دقیقه دوچرخه یا طناب</li>
<li>۱۰ دور چرخش شانه</li>
<li>۱۰ اسکوات بدون وزنه</li>
<li>۱۰ لانج</li>
<li>۱۰ حرکت کششی پویا</li>
</ol>"
            },
            new()
            {
                Title = "معرفی کامل باشگاه GymClub",
                Slug = "gymclub-introduction",
                Summary = "با امکانات، تجهیزات و مربیان باشگاه GymClub آشنا شوید.",
                CategoryId = cats["معرفی باشگاه"].Id,
                AuthorId = author.Id,
                Type = ArticleType.ClubIntro,
                Status = ArticleStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-30),
                CoverImageUrl = "https://images.unsplash.com/photo-1571902943202-507ec2618e8f?w=800",
                ViewCount = 412,
                ContentHtml = @"<h2>به GymClub خوش آمدید</h2>
<p>باشگاه GymClub با بیش از یک دهه تجربه در زمینه‌ی بدن‌سازی و فیتنس، یکی از مجهزترین باشگاه‌های تخصصی تهران است.</p>
<h2>امکانات</h2>
<ul>
<li>۸۰۰ متر مربع فضای تمرین</li>
<li>تجهیزات Hammer Strength و Technogym</li>
<li>بخش هوازی مجهز</li>
<li>سالن ورزش‌های رزمی</li>
<li>کلاس‌های گروهی یوگا و پیلاتس</li>
</ul>
<h2>ساعات کاری</h2>
<p>شنبه تا چهارشنبه: ۶:۰۰ تا ۲۳:۰۰ — پنجشنبه: ۶:۰۰ تا ۲۲:۰۰ — جمعه: ۸:۰۰ تا ۲۰:۰۰</p>"
            },
        };

        db.Articles.AddRange(articles);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} articles", articles.Count);
    }

    // ═══════════════════════════════════════════════════════════
    // دسته‌بندی محصولات
    // ═══════════════════════════════════════════════════════════
    private static async Task<Dictionary<string, ProductCategory>> SeedProductCategories(
        AppDbContext db, ILogger logger)
    {
        if (await db.ProductCategories.AnyAsync())
            return await db.ProductCategories.ToDictionaryAsync(c => c.Title, c => c);

        var list = new List<ProductCategory>
        {
            new() { Title = "مکمل‌های غذایی", Slug = "supplements", SortOrder = 1, IsActive = true },
            new() { Title = "ابزار و تجهیزات ورزشی", Slug = "equipment", SortOrder = 2, IsActive = true },
            new() { Title = "پوشاک ورزشی", Slug = "apparel", SortOrder = 3, IsActive = true },
            new() { Title = "لوازم جانبی", Slug = "accessories", SortOrder = 4, IsActive = true },
        };
        db.ProductCategories.AddRange(list);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} product categories", list.Count);
        return list.ToDictionary(c => c.Title, c => c);
    }

    // ═══════════════════════════════════════════════════════════
    // محصولات نمونه
    // ═══════════════════════════════════════════════════════════
    private static async Task SeedProducts(
        AppDbContext db, Dictionary<string, ProductCategory> cats, ILogger logger)
    {
        if (await db.Products.AnyAsync()) return;

        var products = new List<Product>
        {
            new()
            {
                Name = "پروتئین وی ایزوله ۲ کیلوگرمی",
                Slug = "whey-isolate-2kg",
                Description = "پروتئین وی ایزوله با خلوص بالا، مناسب برای رشد و ترمیم عضلات پس از تمرین. هر وعده حاوی ۲۴ گرم پروتئین خالص با جذب سریع است.\n\nروش مصرف: یک پیمانه (۳۰ گرم) در ۲۵۰ میلی‌لیتر آب یا شیر حل کرده و بلافاصله پس از تمرین مصرف کنید.",
                CategoryId = cats["مکمل‌های غذایی"].Id,
                Price = 2_850_000,
                DiscountPrice = 2_590_000,
                Stock = 40,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1593095948071-474c5cc2989d?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "وزن", Value = "۲ کیلوگرم", SortOrder = 0 },
                    new() { Title = "میزان پروتئین", Value = "۲۴ گرم در هر وعده", SortOrder = 1 },
                    new() { Title = "طعم", Value = "شکلات", SortOrder = 2 },
                    new() { Title = "تعداد سروینگ", Value = "۶۶ وعده", SortOrder = 3 },
                }
            },
            new()
            {
                Name = "کراتین مونوهیدرات میکرونایز ۳۰۰ گرمی",
                Slug = "creatine-monohydrate",
                Description = "کراتین مونوهیدرات میکرونایز با خلوص ۹۹.۹٪، افزایش‌دهنده قدرت و استقامت عضلانی.\n\nروش مصرف: ۵ گرم در روز همراه با آب یا آبمیوه.",
                CategoryId = cats["مکمل‌های غذایی"].Id,
                Price = 890_000,
                DiscountPrice = 750_000,
                Stock = 65,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1594737625785-a6cbdabd333c?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "وزن", Value = "۳۰۰ گرم", SortOrder = 0 },
                    new() { Title = "خلوص", Value = "۹۹.۹٪", SortOrder = 1 },
                    new() { Title = "نوع", Value = "مونوهیدرات میکرونایز", SortOrder = 2 },
                }
            },
            new()
            {
                Name = "دمبل قابل تنظیم ۲۰ کیلوگرمی (جفت)",
                Slug = "adjustable-dumbbell-20kg",
                Description = "جفت دمبل قابل تنظیم با صفحات فولادی روکش لاستیکی و دستگیره ارگونومیک ضدلغزش. مناسب برای تمرینات خانگی حرفه‌ای.",
                CategoryId = cats["ابزار و تجهیزات ورزشی"].Id,
                Price = 12_500_000,
                DiscountPrice = 10_900_000,
                Stock = 8,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1638536532686-d610adfc8e5c?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "وزن نهایی", Value = "۲۰ کیلوگرم (هر دمبل)", SortOrder = 0 },
                    new() { Title = "جنس", Value = "فولاد با روکش لاستیک", SortOrder = 1 },
                    new() { Title = "تعداد در بسته", Value = "۲ عدد", SortOrder = 2 },
                    new() { Title = "گارانتی", Value = "۱۲ ماه", SortOrder = 3 },
                }
            },
            new()
            {
                Name = "مت یوگا و ورزشی ضدلغزش ۶ میلی‌متری",
                Slug = "yoga-mat-6mm",
                Description = "مت ورزشی با سطح ضدلغزش دوطرفه، ضخامت ۶ میلی‌متر و جنس TPE سازگار با محیط زیست. همراه با بند حمل.",
                CategoryId = cats["ابزار و تجهیزات ورزشی"].Id,
                Price = 450_000,
                Stock = 120,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1601925260368-ae2f83cf8b7f?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "ابعاد", Value = "۱۸۳ × ۶۱ سانتی‌متر", SortOrder = 0 },
                    new() { Title = "ضخامت", Value = "۶ میلی‌متر", SortOrder = 1 },
                    new() { Title = "جنس", Value = "TPE", SortOrder = 2 },
                }
            },
            new()
            {
                Name = "ست کشش ورزشی ۵ تایی",
                Slug = "resistance-band-set",
                Description = "ست ۵ عددی کشش ورزشی لاتکس طبیعی با درجات مقاومت مختلف از ۵ تا ۲۵ کیلوگرم. همراه با کیف حمل.",
                CategoryId = cats["ابزار و تجهیزات ورزشی"].Id,
                Price = 380_000,
                DiscountPrice = 299_000,
                Stock = 200,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1598289431512-b97b0917affc?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "تعداد", Value = "۵ عدد", SortOrder = 0 },
                    new() { Title = "جنس", Value = "لاتکس طبیعی", SortOrder = 1 },
                    new() { Title = "مقاومت", Value = "۵ تا ۲۵ کیلوگرم", SortOrder = 2 },
                }
            },
            new()
            {
                Name = "تی‌شرت ورزشی مردانه نیم‌آستین",
                Slug = "sport-tshirt-men",
                Description = "تی‌شرت ورزشی از پارچه Dry-Fit با قابلیت انتقال رطوبت. مناسب برای تمرینات بدنسازی و دویدن.",
                CategoryId = cats["پوشاک ورزشی"].Id,
                Price = 520_000,
                DiscountPrice = 450_000,
                Stock = 85,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "جنس", Value = "پلی‌استر Dry-Fit", SortOrder = 0 },
                    new() { Title = "رنگ", Value = "مشکی", SortOrder = 1 },
                    new() { Title = "سایزبندی", Value = "S تا XXL", SortOrder = 2 },
                }
            },
            new()
            {
                Name = "دستکش بدنسازی چرمی با مچ‌بند",
                Slug = "leather-gym-gloves",
                Description = "دستکش بدنسازی با کف چرم طبیعی و مچ‌بند قابل تنظیم. محافظت کامل از کف دست در تمرینات.",
                CategoryId = cats["لوازم جانبی"].Id,
                Price = 340_000,
                Stock = 95,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1598289431512-b97b0917affc?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "جنس کف", Value = "چرم طبیعی", SortOrder = 0 },
                    new() { Title = "مچ‌بند", Value = "قابل تنظیم", SortOrder = 1 },
                    new() { Title = "سایزبندی", Value = "M، L، XL", SortOrder = 2 },
                }
            },
            new()
            {
                Name = "شیکر پروتئین ۷۰۰ میلی‌لیتری",
                Slug = "protein-shaker-700ml",
                Description = "شیکر پروتئین با بدنه BPA-Free، درب پیچی ضدچکه و توری مخلوط‌کن استیل.",
                CategoryId = cats["لوازم جانبی"].Id,
                Price = 150_000,
                DiscountPrice = 120_000,
                Stock = 300,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1593095948071-474c5cc2989d?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "حجم", Value = "۷۰۰ میلی‌لیتر", SortOrder = 0 },
                    new() { Title = "جنس", Value = "BPA-Free", SortOrder = 1 },
                    new() { Title = "رنگ", Value = "مشکی با درب قرمز", SortOrder = 2 },
                }
            },
            new()
            {
                Name = "روغن MCT نارگیل ۵۰۰ میلی‌لیتری",
                Slug = "mct-oil-500ml",
                Description = "روغن MCT خالص از نارگیل، منبع سریع انرژی برای تمرینات و کمک به چربی‌سوزی. مناسب برای رژیم‌های کتوژنیک.",
                CategoryId = cats["مکمل‌های غذایی"].Id,
                Price = 680_000,
                Stock = 30,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "حجم", Value = "۵۰۰ میلی‌لیتر", SortOrder = 0 },
                    new() { Title = "منبع", Value = "نارگیل", SortOrder = 1 },
                    new() { Title = "مناسب برای", Value = "رژیم کتو", SortOrder = 2 },
                }
            },
            new()
            {
                Name = "ست هالتر ۱۰۰ کیلوگرمی با میله المپیک",
                Slug = "barbell-set-100kg",
                Description = "ست کامل هالتر شامل میله المپیک ۲۲۰ سانتی‌متری و ۱۰۰ کیلوگرم صفحه با روکش لاستیکی. مناسب برای باشگاه‌های خانگی و تمرینات حرفه‌ای.",
                CategoryId = cats["ابزار و تجهیزات ورزشی"].Id,
                Price = 28_500_000,
                DiscountPrice = 24_900_000,
                Stock = 3,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=800", IsMain = true, SortOrder = 0 }
                },
                Attributes = new List<ProductAttribute>
                {
                    new() { Title = "وزن کل", Value = "۱۰۰ کیلوگرم", SortOrder = 0 },
                    new() { Title = "طول میله", Value = "۲۲۰ سانتی‌متر", SortOrder = 1 },
                    new() { Title = "نوع میله", Value = "المپیک (قطر ۵۰ میلی‌متر)", SortOrder = 2 },
                    new() { Title = "گارانتی", Value = "۲۴ ماه", SortOrder = 3 },
                }
            },
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} products", products.Count);
    }

    // ═══════════════════════════════════════════════════════════
    // پرسشنامه ورزشی
    // ═══════════════════════════════════════════════════════════
    private static async Task SeedWorkoutQuestionnaire(AppDbContext db, ILogger logger)
    {
        if (await db.WorkoutQuestionnaires.AnyAsync(x => x.IsActive)) return;

        var q = new WorkoutQuestionnaire
        {
            Title = "پرسشنامه جامع برنامه ورزشی",
            Description = "برای طراحی برنامه اختصاصی، لطفاً اطلاعات دقیق بدن و سلامت خود را وارد کنید. پاسخ‌های شما محرمانه است و فقط توسط مربیان دیده می‌شود.",
            Price = 450_000,
            IsActive = true,
            Version = 1,
        };

        var questions = new List<WorkoutQuestion>
        {
            new() { QuestionText = "سن شما (سال)", Type = QuestionType.Number, IsRequired = true, SortOrder = 0, GroupName = "مشخصات بدن" },
            new() { QuestionText = "قد شما (سانتی‌متر)", Type = QuestionType.Number, IsRequired = true, SortOrder = 1, GroupName = "مشخصات بدن" },
            new() { QuestionText = "وزن فعلی (کیلوگرم)", Type = QuestionType.Number, IsRequired = true, SortOrder = 2, GroupName = "مشخصات بدن" },
            new()
            {
                QuestionText = "جنسیت", Type = QuestionType.SingleChoice, IsRequired = true, SortOrder = 3,
                GroupName = "مشخصات بدن",
                OptionsJson = JsonSerializer.Serialize(new[] { "مرد", "زن" })
            },
            new()
            {
                QuestionText = "سطح تجربه در تمرین", Type = QuestionType.SingleChoice, IsRequired = true, SortOrder = 4,
                GroupName = "مشخصات بدن",
                OptionsJson = JsonSerializer.Serialize(new[] { "مبتدی", "متوسط", "پیشرفته" })
            },
            new()
            {
                QuestionText = "هدف اصلی شما", Type = QuestionType.SingleChoice, IsRequired = true, SortOrder = 5,
                GroupName = "هدف",
                OptionsJson = JsonSerializer.Serialize(new[] { "کاهش وزن", "افزایش حجم عضلانی", "حفظ تناسب", "افزایش قدرت", "آمادگی عمومی" })
            },
            new()
            {
                QuestionText = "چند روز در هفته می‌توانید تمرین کنید؟", Type = QuestionType.SingleChoice, IsRequired = true, SortOrder = 6,
                GroupName = "هدف",
                OptionsJson = JsonSerializer.Serialize(new[] { "۲ روز", "۳ روز", "۴ روز", "۵ روز", "۶ روز" })
            },
            new()
            {
                QuestionText = "محل تمرین", Type = QuestionType.SingleChoice, IsRequired = true, SortOrder = 7,
                GroupName = "تجهیزات",
                OptionsJson = JsonSerializer.Serialize(new[] { "باشگاه", "خانه با تجهیزات", "خانه بدون تجهیزات" })
            },
            new()
            {
                QuestionText = "تجهیزات در دسترس", Type = QuestionType.MultiChoice, IsRequired = false, SortOrder = 8,
                GroupName = "تجهیزات",
                OptionsJson = JsonSerializer.Serialize(new[] { "دمبل", "هالتر", "کشش", "میله بارفیکس", "دستگاه‌های باشگاه", "وزن بدن" })
            },
            new() { QuestionText = "سابقه آسیب‌دیدگی یا جراحی", Type = QuestionType.Text, IsRequired = false, SortOrder = 9, GroupName = "سلامت" },
            new() { QuestionText = "بیماری خاصی دارید؟", Type = QuestionType.Text, IsRequired = false, SortOrder = 10, GroupName = "سلامت" },
            new() { QuestionText = "مشکل قلبی یا فشار خون دارید؟", Type = QuestionType.Boolean, IsRequired = true, SortOrder = 11, GroupName = "سلامت" },
            new() { QuestionText = "توضیحات اضافی برای مربی", Type = QuestionType.Text, IsRequired = false, SortOrder = 12, GroupName = "توضیحات" },
        };

        q.Questions = questions;
        db.WorkoutQuestionnaires.Add(q);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded workout questionnaire with {Count} questions", questions.Count);
    }
}