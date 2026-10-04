namespace Asistencias.Api.Laboratorios;

public enum EstadoEquipo { Libre, EnUso, FueraDeServicio }

public static class ReglasEquipo
{
    // Fuera de servicio manda: un equipo dado de baja se muestra así aunque alguien no haya registrado su salida
    public static EstadoEquipo Estado(bool fueraDeServicio, bool ocupado) =>
        fueraDeServicio ? EstadoEquipo.FueraDeServicio
        : ocupado ? EstadoEquipo.EnUso
        : EstadoEquipo.Libre;

    // Orden natural de etiquetas: PC-2 antes que PC-10
    public static IOrderedEnumerable<T> OrdenarPorEtiqueta<T>(this IEnumerable<T> equipos, Func<T, string> etiqueta) =>
        equipos.OrderBy(e => etiqueta(e).Length).ThenBy(etiqueta, StringComparer.OrdinalIgnoreCase);
}
