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
            // ���������, ����� �� �������������� �������� ��� ��������� ������
            ValidateIssuer = true,
            // ������, �������������� ��������
            ValidIssuer = AuthOptions.ISSUER,
            // ����� �� �������������� ����������� ������
            ValidateAudience = true,
            // ��������� ����������� ������
            ValidAudience = AuthOptions.AUDIENCE,
            // ����� �� �������������� ����� �������������
            ValidateLifetime = true,
            // ��������� ����� ������������
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            // ��������� ����� ������������
            ValidateIssuerSigningKey = true,
        };
    });
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
// �������������� � ������� ����


app.UseAuthentication();   // ���������� middleware �������������� 
app.UseAuthorization();   // ���������� middleware ����������� 


app.MapGet("/", async (HttpContext context) =>
{
    // Redirect to login page
    context.Response.Redirect("/login");
});
app.MapGet("/index", async (HttpContext context) =>
{
    // �������� ���� � ����� index.html
    string indexPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");

    // ������ ���������� ����� index.html
    string content = await File.ReadAllTextAsync(indexPath);

    // ���������� ���������� ����� index.html
    return Results.Ok(content);
});
app.MapGet("/api/users", async (ApplicationContext db) => await db.Users.ToListAsync());

app.MapGet("/api/users/{id:int}", async (int id, ApplicationContext db) =>
{
    // �������� ������������ �� id
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (user == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, ���������� ���
    return Results.Json(user);
});


app.MapDelete("/api/users/{id:int}", async (int id, ApplicationContext db) =>
{
    // �������� ������������ �� id
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (user == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, ������� ���
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Json(user);
});

app.MapPost("/api/users", async (User user, ApplicationContext db) =>
{
    // ��������� ������������ � ������
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();
    return user;
});


app.MapPut("/api/users", async (User userData, ApplicationContext db) =>
{
    // �������� ������������ s�� id
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userData.Id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (user == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, �������� ��� ������ � ���������� ������� �������
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
// HTML-����� ��� ����� ������/������
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

    // �������� ����� �������� email � ������

    // ��������� ��������������
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
    // ���� ������������ �� ������, ���������� ��������� ��� 401
    if (user is null) return Results.Unauthorized();
    if (user is not null)
    {
        var claims = new List<Claim> { new Claim (ClaimTypes.Name, user.Name) };
        // ������� JWT-�����
        var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(2)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
        string username = new JwtSecurityTokenHandler().WriteToken(jwt);

        string indexPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");

        // ������ ���������� ����� index.html
        string content = await File.ReadAllTextAsync(indexPath);

        // ���������� ���������� ����� index.html
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
    public const string ISSUER = "MyAuthServer"; // �������� ������
    public const string AUDIENCE = "MyAuthClient"; // ����������� ������
    const string KEY = "mysupersecret_secretsecretsecretkey!123";   // ���� ��� ��������
    public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
}