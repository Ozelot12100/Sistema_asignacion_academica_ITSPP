namespace Asistencias.Api.Clases;

public record AlumnoDeLista(int Id, string Matricula, string Nombre, string Apellido);

public record RenglonDeLista(
    int AlumnoId, string Matricula, string Nombre, string Apellido, bool Inscrito, bool Presente,
    long? AsistenciaId, string? Equipo, DateTime? HoraEntrada, DateTime? HoraSalida);

public record ListaDeClaseDto(int Inscritos, int Presentes, int Faltas, IReadOnlyList<RenglonDeLista> Alumnos);

public static class ListaDeAsistencia
{
    // Cruza a los inscritos con quienes registraron entrada. Quien asistió y después fue dado de baja
    // del curso sigue apareciendo, marcado como no inscrito, para que la lista no pierda una asistencia real.
    public static ListaDeClaseDto Armar(IReadOnlyCollection<AlumnoDeLista> inscritos, IReadOnlyCollection<AsistenteDto> asistentes)
    {
        var entradas = asistentes
            .GroupBy(a => a.AlumnoId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.HoraEntrada).First());
        var idsInscritos = inscritos.Select(a => a.Id).ToHashSet();

        var renglones = inscritos
            .Select(a => entradas.TryGetValue(a.Id, out var e)
                ? new RenglonDeLista(a.Id, a.Matricula, a.Nombre, a.Apellido, true, true, e.AsistenciaId, e.Equipo, e.HoraEntrada, e.HoraSalida)
                : new RenglonDeLista(a.Id, a.Matricula, a.Nombre, a.Apellido, true, false, null, null, null, null))
            .Concat(entradas.Values
                .Where(e => !idsInscritos.Contains(e.AlumnoId))
                .Select(e => new RenglonDeLista(
                    e.AlumnoId, e.Matricula, e.Nombre, e.Apellido, false, true, e.AsistenciaId, e.Equipo, e.HoraEntrada, e.HoraSalida)))
            .OrderBy(r => r.Apellido, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.AlumnoId)
            .ToList();

        return new ListaDeClaseDto(
            inscritos.Count,
            renglones.Count(r => r.Presente),
            renglones.Count(r => r.Inscrito && !r.Presente),
            renglones);
    }

    // Sin clases dadas no hay porcentaje: 0 % daría a entender que el alumno faltó a todo
    public static decimal? Porcentaje(int asistencias, int totalClases) =>
        totalClases == 0 ? null : Math.Round(100m * asistencias / totalClases, 1);
}
