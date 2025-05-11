using AsignacionAcademica.Api.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(p => p.AddDefaultPolicy(opt => opt.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();

app.MapGet("/", () => "API de Asignación Académica funcionando");

app.MapUsuarios();
app.MapProfesores();
app.MapAsignaturas();
app.MapGrupos();
app.MapAulas();
app.MapClases();
app.MapDisponibilidad();

app.Urls.Clear();
//Para permitir que cualquier dispositivo en la red pueda acceder a la API
app.Urls.Add("http://0.0.0.0:3000");
app.Run();
