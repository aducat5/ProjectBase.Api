using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectBase.Data.Database;
using ProjectBase.Data.Services;
using ProjectBase.Model;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


#region Identity

//adding identity context
builder.Services.AddDbContext<BaseIdentityContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("BaseDb")));

//adding indentity
builder.Services
    .AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<BaseIdentityContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    //this is where you can configure anything about users and authorization
    options.Password.RequireNonAlphanumeric = false;
    //options.SignIn.RequireConfirmedEmail = false;
    //options.SignIn.RequireConfirmedAccount = false;
    //options.User.RequireUniqueEmail = false;
});
#endregion

#region JWT

//adding JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

#endregion


//adding db context as singleton
builder.Services.AddSingleton<DapperContext>();

//adding authservice
builder.Services.AddScoped<IAuthService, AuthService>();

//adding repository services
////adding test repo
builder.Services.AddScoped<IBaseService, BaseService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Base TEST API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,

            },
            new List<string>()
        }
    });
});

//adding CORS policy for dev client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDevClient", b =>
    {
        b.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//use authentication. this is for protecting controlllers with auth service and identity
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
