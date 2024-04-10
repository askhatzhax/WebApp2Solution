using Microsoft.EntityFrameworkCore;
using WebApplication1;
public class ApplicationContext : DbContext
{

    public DbSet<User> Users { get; set; } = null!;
    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
        Database.EnsureCreated();   // создаем базу данных при первом обращении
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Tom", Age = 37, Email = "Tom@gmail.com", Password = "123" },
                new User { Id = 2, Name = "Bob", Age = 41, Email = "Bob@gmail.com", Password = "123" },
                new User { Id = 3, Name = "Askhat", Age = 24, Email = "Askhat@gmail.com", Password = "123" }
        );
    }
}