using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SocialMediaAppBackend.Options;
using SocialMediaAppBackend.Services;
using SocialMediaAppBackend.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("JwtOptions");

var dbFileName = builder.Configuration["DatabaseOptions:FileName"];

var folder = Environment.SpecialFolder.LocalApplicationData;
var path = Environment.GetFolderPath(folder);
var dbPath = Path.Join(path, dbFileName);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPostsService, PostsService>();
builder.Services.AddScoped<IUsersService, UsersService>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});

// Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));


// Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtOptions["Issuer"],
            ValidAudience = jwtOptions["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions["Key"]))
        };
    });

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
        });
});

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// Backend/
// │
// ├── Controllers/
// │   ├── AuthController.cs
// │   ├── PostsController.cs
// │   ├── UsersController.cs
// │   └── FollowsController.cs
// │
// ├── DTOs/
// │   ├── Auth/
// │   ├── Posts/
// │   └── Users/
// │
// ├── Models/
// │   ├── User.cs
// │   ├── Post.cs
// │   ├── Follow.cs
// │   └── Like.cs
// │
// ├── Data/
// │   ├── AppDbContext.cs
// │   └── Migrations/
// │
// ├── Services/
// │   ├── Interfaces/
// │   │   ├── IAuthService.cs
// │   │   └── IPostService.cs
// │   │
// │   ├── AuthService.cs
// │   ├── PostService.cs
// │   └── UserService.cs
// │
// ├── Repositories/          (optional)
// │   ├── Interfaces/
// │   └── Implementations/
// │
// ├── Helpers/
// │   ├── JwtHelper.cs
// │   └── PaginationHelper.cs
// │
// ├── Middleware/
// │
// └── Program.cs