using GymClub.Domain.Entities;
using CartEntity = GymClub.Domain.Entities.Cart;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace GymClub.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<Article> Articles { get; }
    DbSet<ArticleCategory> ArticleCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductAttribute> ProductAttributes { get; }
    DbSet<WorkoutQuestionnaire> WorkoutQuestionnaires { get; }
    DbSet<WorkoutQuestion> WorkoutQuestions { get; }
    DbSet<WorkoutRequest> WorkoutRequests { get; }
    DbSet<WorkoutAnswer> WorkoutAnswers { get; }
    DbSet<WorkoutProgram> WorkoutPrograms { get; }
    DbSet<WorkoutProgramDay> WorkoutProgramDays { get; }
    DbSet<WorkoutExercise> WorkoutExercises { get; }
    DbSet<WorkoutPaymentTransaction> WorkoutPaymentTransactions { get; }
    DbSet<Address> Addresses { get; }
    DbSet<CartEntity> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<DiscountCode> DiscountCodes { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}