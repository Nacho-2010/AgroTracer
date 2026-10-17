using AgroTraceCR.Application;
using AgroTraceCR.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<AgroDbContext>(o =>
    o.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer")
            ?? "Server=localhost;Database=AgroTraceCR;Integrated Security=True;TrustServerCertificate=True"
    )
);
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<IStellarService, StellarService>();
builder.Services.AddScoped<ITraceService, TraceService>();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();
var frontendOrigins = builder.Configuration["Frontend:Origin"] is { Length: > 0 } configuredOrigin
    ? new[] { configuredOrigin }
    : new[] { "http://localhost:5173", "http://127.0.0.1:5173" };
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod())
);
var app = builder.Build();
app.UseExceptionHandler(handler =>
    handler.Run(async ctx =>
    {
        var e = ctx
            .Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()
            ?.Error;
        var code =
            e is KeyNotFoundException ? 404
            : e is ArgumentException ? 400
            : e is InvalidOperationException ? 503
            : 500;
        ctx.Response.StatusCode = code;
        await ctx.Response.WriteAsJsonAsync(
            new
            {
                title = code == 503 ? e!.Message
                : code == 404 ? "Registro no encontrado"
                : code == 400 ? e!.Message
                : "No se pudo completar la operación. Revise los registros del servidor.",
                status = code,
            }
        );
    })
);
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.MapGet("/health", () => new { status = "ok", red = "Testnet" });
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AgroDbContext>().Database.MigrateAsync();
    return;
}
app.Run();
