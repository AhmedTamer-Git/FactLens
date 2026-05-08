using Factlens.Core.Models;
using Factlens.Data.Context;
using Factlens.Data.Seed;                 // ✅ Seeder
using Factlens.Services.Interfaces;      // ✅ Interfaces
using Factlens.Services.Services;
using Factlens.Services.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;          // ✅ Swagger enums
using QuestPDF.Infrastructure;
using System.Text;

public class Program
{
    public static async Task Main(string[] args)   // ✅ async علشان Seeder
    {
        var builder = WebApplication.CreateBuilder(args);

        // ====================== DbContext + Migrations ======================
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly("Factlens.Data")
            )
        );

        //================== ✅ QuestPDF License===============
        QuestPDF.Settings.License = LicenseType.Community;

        // ====================== Identity ======================
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ====================== Authentication ======================
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
                ),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        })
        .AddCookie("External")
        .AddGoogle("Google", options =>
        {
            options.SignInScheme = "External";
            options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            options.SaveTokens = true;
        });

        // ====================== Authorization (NEW) ======================
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        //====================== Email Service ======================
        builder.Services.Configure<EmailSettings>(
            builder.Configuration.GetSection("EmailSettings"));

        builder.Services.AddScoped<IEmailService, EmailService>();

        // ====================== JwtService ======================
        builder.Services.AddScoped<JwtService>();

        // ====================== AiService ======================
        builder.Services.AddHttpClient<AiService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Ai:BaseUrl"]);
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        // ====================== NEW Service Layer ======================
        builder.Services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        builder.Services.AddScoped<IHistoryService, HistoryService>();
        builder.Services.AddScoped<IFeedbackService, FeedbackService>();
        builder.Services.AddScoped<IShareService, ShareService>();
        builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        // ====================== Controllers ======================
        builder.Services.AddControllers();

        // ====================== Swagger ======================
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Factlens API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. مثال: 'Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,   // ✅ الأفضل
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        //--------------------------- CORS ---------------------------
        // ✅ في Production الفرونت جوا نفس الـ app — CORS مش محتاجه
        // بس خليناه للـ Development لو حد شغّل الفرونت على Live Server
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500",
                                       "http://localhost:5173", "http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else
                {
                    // Production: same-origin فقط
                    policy.AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });

        var app = builder.Build();

        // ====================== Seeder (NEW) ======================
        await IdentitySeeder.SeedAsync(app.Services, app.Configuration);

        // ====================== Middleware ======================
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Factlens API v1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("Frontend");

        // ✅ Static Files — يخدم wwwroot (HTML / CSS / JS)
        app.UseStaticFiles();

        // ✅ Redirect / إلى html/home.html
        app.MapGet("/", () => Results.Redirect("/html/home.html"));

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // ✅ Fallback — أي URL مش API يرجع home.html
        app.MapFallbackToFile("html/home.html");

        app.Run();
    }
}