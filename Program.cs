using Accounting_Register_BE.Areas.Users.Model;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

//**Database related services */
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDBContext>(options => options.UseSqlServer(connectionString));



#region authorization - Authentication services

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDBContext>();
#endregion

#region Section to add Interfaces 

//builder.Services.AddScoped<IAduanasService, AduanasService>();


#endregion

#region CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          );
});

#endregion


#region Controllers Endpoints

    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options => {
        options.SwaggerDoc("v1", new OpenApiInfo()
        {
            Title = "Auth Demo",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            In = ParameterLocation.Header,
            Description = "Please enter a Token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference{ Type = ReferenceType.SecurityScheme, Id="Bearer"}
                },[]
            }
        });
    });

#endregion

var app = builder.Build();


// Identity endpoints: /api/auth/login, /api/auth/refresh, /api/auth/manage/info ...
// Public sign-up (/api/auth/register) is off unless Auth:AllowRegistration is true
// (on in Development; in Docker it is set with ACCOUNTING_ALLOW_REGISTRATION in .env).
var allowRegistration = app.Configuration.GetValue<bool>("Auth:AllowRegistration");
var authGroup = app.MapGroup("/api/auth");
authGroup.AddEndpointFilter(async (context, next) =>
{
    var path = context.HttpContext.Request.Path.Value ?? string.Empty;
    if (!allowRegistration && path.EndsWith("/register", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound();
    }
    return await next(context);
});
authGroup.MapIdentityApi<ApplicationUser>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowSpecificOrigin");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapSwagger().RequireAuthorization();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapProgramEndpoints();

app.Run();