using GymClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymClub.Infrastructure.Persistence.Configurations;

public class WorkoutQuestionnaireConfiguration : IEntityTypeConfiguration<WorkoutQuestionnaire>
{
    public void Configure(EntityTypeBuilder<WorkoutQuestionnaire> b)
    {
        b.ToTable("WorkoutQuestionnaires");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Price).HasPrecision(18, 0);
    }
}

public class WorkoutQuestionConfiguration : IEntityTypeConfiguration<WorkoutQuestion>
{
    public void Configure(EntityTypeBuilder<WorkoutQuestion> b)
    {
        b.ToTable("WorkoutQuestions");
        b.Property(x => x.QuestionText).HasMaxLength(500).IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.OptionsJson).HasMaxLength(2000);
        b.Property(x => x.GroupName).HasMaxLength(100);
        b.HasOne(x => x.Questionnaire).WithMany(x => x.Questions)
            .HasForeignKey(x => x.QuestionnaireId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkoutRequestConfiguration : IEntityTypeConfiguration<WorkoutRequest>
{
    public void Configure(EntityTypeBuilder<WorkoutRequest> b)
    {
        b.ToTable("WorkoutRequests");
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Amount).HasPrecision(18, 0);
        b.Property(x => x.AdminNote).HasMaxLength(2000);
        b.HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Questionnaire).WithMany(x => x.Requests)
            .HasForeignKey(x => x.QuestionnaireId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkoutAnswerConfiguration : IEntityTypeConfiguration<WorkoutAnswer>
{
    public void Configure(EntityTypeBuilder<WorkoutAnswer> b)
    {
        b.ToTable("WorkoutAnswers");
        b.Property(x => x.AnswerText).HasMaxLength(2000);
        b.Property(x => x.AnswerJson).HasMaxLength(2000);
        b.HasOne(x => x.Request).WithMany(x => x.Answers)
            .HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Question).WithMany(x => x.Answers)
            .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkoutProgramConfiguration : IEntityTypeConfiguration<WorkoutProgram>
{
    public void Configure(EntityTypeBuilder<WorkoutProgram> b)
    {
        b.ToTable("WorkoutPrograms");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.HasOne(x => x.Request).WithOne(x => x.Program)
            .HasForeignKey<WorkoutProgram>(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkoutProgramDayConfiguration : IEntityTypeConfiguration<WorkoutProgramDay>
{
    public void Configure(EntityTypeBuilder<WorkoutProgramDay> b)
    {
        b.ToTable("WorkoutProgramDays");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasOne(x => x.Program).WithMany(x => x.Days)
            .HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> b)
    {
        b.ToTable("WorkoutExercises");
        b.Property(x => x.ExerciseName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Reps).HasMaxLength(50);
        b.Property(x => x.Weight).HasMaxLength(50);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.VideoUrl).HasMaxLength(500);
        b.HasOne(x => x.ProgramDay).WithMany(x => x.Exercises)
            .HasForeignKey(x => x.ProgramDayId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkoutPaymentTransactionConfiguration : IEntityTypeConfiguration<WorkoutPaymentTransaction>
{
    public void Configure(EntityTypeBuilder<WorkoutPaymentTransaction> b)
    {
        b.ToTable("WorkoutPaymentTransactions");
        b.Property(x => x.Authority).HasMaxLength(100).IsRequired();
        b.Property(x => x.RefId).HasMaxLength(100);
        b.Property(x => x.Amount).HasPrecision(18, 0);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Gateway).HasMaxLength(50);
        b.HasOne(x => x.WorkoutRequest).WithMany()
            .HasForeignKey(x => x.WorkoutRequestId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.Authority);
    }
}