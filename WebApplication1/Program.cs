using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using WebApplication1;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
var builder = WebApplication.CreateBuilder();
string connection = "Server=(localdb)\\mssqllocaldb;Database=applicationdb;Trusted_Connection=True;";
builder.Services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connection));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/login");
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // указывает, будет ли валидироваться издатель при валидации токена
            ValidateIssuer = true,
            // строка, представляющая издателя
            ValidIssuer = AuthOptions.ISSUER,
            // будет ли валидироваться потребитель токена
            ValidateAudience = true,
            // установка потребителя токена
            ValidAudience = AuthOptions.AUDIENCE,
            // будет ли валидироваться время существования
            ValidateLifetime = true,
            // установка ключа безопасности
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            // валидация ключа безопасности
            ValidateIssuerSigningKey = true,
        };
    });
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
// аутентификация с помощью куки


app.UseAuthentication();   // добавление middleware аутентификации 
app.UseAuthorization();   // добавление middleware авторизации 


app.MapGet("/", async (HttpContext context) =>
{
    // Redirect to login page
    context.Response.Redirect("/login");
});
app.MapGet("/index", async (HttpContext context) =>
{
    // Получаем путь к файлу index.html
    string indexPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");

    // Читаем содержимое файла index.html
    string content = await File.ReadAllTextAsync(indexPath);

    // Возвращаем содержимое файла index.html
    return Results.Ok(content);
});
app.MapGet("/api/users", async (ApplicationContext db) => await db.Users.ToListAsync());

app.MapGet("/api/users/{id:int}", async (int id, ApplicationContext db) =>
{
    // получаем пользователя по id
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    // если не найден, отправляем статусный код и сообщение об ошибке
    if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });

    // если пользователь найден, отправляем его
    return Results.Json(user);
});


app.MapDelete("/api/users/{id:int}", async (int id, ApplicationContext db) =>
{
    // получаем пользователя по id
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    // если не найден, отправляем статусный код и сообщение об ошибке
    if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });

    // если пользователь найден, удаляем его
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Json(user);
});

app.MapPost("/api/users", async (User user, ApplicationContext db) =>
{
    // добавляем пользователя в массив
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();
    return user;
});


app.MapPut("/api/users", async (User userData, ApplicationContext db) =>
{
    // получаем пользователя sпо id
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userData.Id);

    // если не найден, отправляем статусный код и сообщение об ошибке
    if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });

    // если пользователь найден, изменяем его данные и отправляем обратно клиенту
    user.Age = userData.Age;
    user.Name = userData.Name;
    await db.SaveChangesAsync();
    return Results.Json(user);

});



app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.Map("/login", (HttpContext context) =>
{
context.Response.ContentType = "text/html; charset=utf-8";
// HTML-форма для ввода логина/пароля
string loginForm = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8' />
        <title>Login Form</title>
    </head>
    <body>
        <h2>Login</h2>
        <form method='post' action='/login'>
            <p>
                <label>Email:</label><br />
                <input type='email' name='email' required />
            </p>
            <p>
                <label>Password:</label><br />
                <input type='password' name='password' required />
            </p>
            <input type='submit' value='Login' />
        </form>
    </body>
    </html>";
return context.Response.WriteAsync(loginForm);
});

app.MapPost("/login", async (HttpContext context, ApplicationContext db) =>
{
var form = await context.Request.ReadFormAsync();
string email = form["email"];
string password = form["password"];

    // Добавьте здесь проверку email и пароля

    // Примерная аутентификация
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
    // если пользователь не найден, отправляем статусный код 401
    if (user is null) return Results.Unauthorized();
    if (user is not null)
    {
        var claims = new List<Claim> { new Claim (ClaimTypes.Name, user.Name) };
        // создаем JWT-токен
        var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(2)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
        string username = new JwtSecurityTokenHandler().WriteToken(jwt);

        string indexPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");

        // Читаем содержимое файла index.html
        string content = await File.ReadAllTextAsync(indexPath);

        // Возвращаем содержимое файла index.html
        return Results.Content(content, "text/html");
    }
    else
    {
    return Results.Unauthorized();
    }
});
app.MapGet("/api/users", async (ApplicationContext db) => await db.Users.ToListAsync());

app.Run();

record class Person(string Email, string Password);

public class AuthOptions
{
    public const string ISSUER = "MyAuthServer"; // издатель токена
    public const string AUDIENCE = "MyAuthClient"; // потребитель токена
    const string KEY = "mysupersecret_secretsecretsecretkey!123";   // ключ для шифрации
    public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
}