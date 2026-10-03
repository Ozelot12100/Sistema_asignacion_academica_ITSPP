using AsignacionAcademica.Api;
using AsignacionAcademica.Api.Controllers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConexionDb>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

//Solo hace falta si el frontend se sirve desde otro origen (p. ej. Vite en desarrollo sin proxy)
var origenes = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(p => p.AddDefaultPolicy(opt => opt.WithOrigins(origenes).AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => "API de Asignación Académica funcionando");

app.MapUsuarios();
app.MapProfesores();
app.MapAsignaturas();
app.MapGrupos();
app.MapAulas();
app.MapClases();
app.MapDisponibilidad();

app.Run();
