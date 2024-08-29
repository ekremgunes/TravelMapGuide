using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using Portfolio.Business.Business.Helpers;
using System.Text;
using TravelMapGuide.Server.Data.Repositories.Abstract;
using TravelMapGuide.Server.Data.Repositories.Concrete;
using TravelMapGuide.Server.Services;
using TravelMapGuideWebApi.Server.Configuration;
using TravelMapGuideWebApi.Server.Data.Context;
using TravelMapGuideWebApi.Server.Data.Repositories.Abstract;
using TravelMapGuideWebApi.Server.Data.Repositories.Concrete;
using TravelMapGuideWebApi.Server.Extensions;
using TravelMapGuideWebApi.Server.Helpers;
using TravelMapGuideWebApi.Server.Services;
using TravelMapGuideWebApi.Server.Validators;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });

    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.Configure<DatabaseConfiguration>(builder.Configuration.GetSection("ConnectionStrings"));

builder.Services.AddSingleton<MongoDbService>();

// Repositories
builder.Services.AddScoped<ITravelRepository, TravelRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IBlacklistRepository, BlacklistRepository>();
builder.Services.AddSingleton<IBlacklistRepository, BlacklistRepository>();

// Services
builder.Services.AddScoped<ITravelService, TravelService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBlacklistService, BlacklistService>();

var profiles = ProfileHelper.GetProfiles();

var configuration = new MapperConfiguration(opt =>
{
    opt.AddProfiles(profiles);
});

var mapper = configuration.CreateMapper();
builder.Services.AddSingleton(mapper);

//JWT settings
builder.Services.AddJwtConfiguration(builder.Configuration);

builder.Services.AddAuthorization();

// FluentValidation
#pragma warning disable CS0618 // T�r veya �ye art�k kullan�lm�yor
builder.Services.AddControllers()
    .AddFluentValidation(x =>
    { x.RegisterValidatorsFromAssemblies(ValidatorProfileHelper.GetValidatorAssemblies()); });
#pragma warning restore CS0618 // T�r veya �ye art�k kullan�lm�yor

// NLog: Setup NLog for Dependency injection
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Host.UseNLog();

var app = builder.Build();

// JwtTokenReader initialize
JwtTokenReader.Initialize(app.Services.GetRequiredService<IHttpContextAccessor>());

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Global Exception Handling Middleware -- extension middleware ile kullan�m?
app.UseGlobalExceptionHandling(logger);

app.UseCors("AllowSpecificOrigins"); // veya "AllowAllOrigins"
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

NLog.LogManager.Shutdown();