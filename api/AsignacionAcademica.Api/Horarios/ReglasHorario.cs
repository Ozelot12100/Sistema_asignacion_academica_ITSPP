using System.Globalization;

namespace AsignacionAcademica.Api.Horarios
{
    public readonly record struct Franja(TimeOnly Inicio, TimeOnly Fin)
    {
        public bool SeTraslapaCon(Franja otra) => Inicio < otra.Fin && otra.Inicio < Fin;
    }

    public static class ReglasHorario
    {
        public const string FormatoHora = "HH:mm";
        public static readonly TimeOnly HoraApertura = new(7, 0);
        public static readonly TimeOnly HoraCierre = new(20, 0);

        public static bool TryCrearFranja(string? horaInicio, string? horaFin, out Franja franja)
        {
            franja = default;
            if (!TryLeerHora(horaInicio, out var inicio) || !TryLeerHora(horaFin, out var fin) || fin <= inicio)
                return false;

            franja = new Franja(inicio, fin);
            return true;
        }

        public static bool DentroDeJornada(Franja franja) =>
            franja.Inicio >= HoraApertura && franja.Fin <= HoraCierre;

        //La disponibilidad puede venir en varios bloques contiguos (08:00–09:00, 09:00–10:00) que juntos cubren la clase
        public static bool EstaCubierta(Franja clase, IEnumerable<Franja> disponibles)
        {
            var cubiertoHasta = clase.Inicio;
            foreach (var d in disponibles.OrderBy(d => d.Inicio))
            {
                if (d.Inicio > cubiertoHasta) break;
                if (d.Fin > cubiertoHasta) cubiertoHasta = d.Fin;
                if (cubiertoHasta >= clase.Fin) return true;
            }
            return false;
        }

        private static bool TryLeerHora(string? texto, out TimeOnly hora) =>
            TimeOnly.TryParseExact(texto, FormatoHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out hora);
    }
}
