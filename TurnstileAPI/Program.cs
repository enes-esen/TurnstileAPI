using System.Reflection;
using InoosterTurnstileAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

//API Projesinde Cors Hizmetinin Etkinleştirilmesi
//API uygulamasında, ReactJS'in alan adımızı kullanmasına izin vermeliyiz.
//Bunun için Conrs Politakasını etkinleştirmeliyiz.
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: "ReactPostDomain",
        policy => policy.WithOrigins("http://localhost:3000")
        //.WithMethods("POST")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

//Add Cors
app.UseCors("ReactPostDomain");

app.UseAuthorization();

app.MapControllers();

app.Run();

