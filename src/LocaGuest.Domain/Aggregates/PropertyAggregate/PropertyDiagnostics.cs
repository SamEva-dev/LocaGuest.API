namespace LocaGuest.Domain.Aggregates.PropertyAggregate;

public sealed class PropertyDiagnostics
{
    public string? DpeRating { get; private set; }
    public int? DpeValue { get; private set; }
    public string? GesRating { get; private set; }

    public DateTime? ElectricDiagnosticDate { get; private set; }
    public DateTime? ElectricDiagnosticExpiry { get; private set; }

    public DateTime? GasDiagnosticDate { get; private set; }
    public DateTime? GasDiagnosticExpiry { get; private set; }

    public bool? HasAsbestos { get; private set; }
    public DateTime? AsbestosDiagnosticDate { get; private set; }

    public string? ErpZone { get; private set; }

    internal PropertyDiagnostics() { }

    internal void Update(
        string? dpeRating = null,
        int? dpeValue = null,
        string? gesRating = null,
        DateTime? electricDiagnosticDate = null,
        DateTime? electricDiagnosticExpiry = null,
        DateTime? gasDiagnosticDate = null,
        DateTime? gasDiagnosticExpiry = null,
        bool? hasAsbestos = null,
        DateTime? asbestosDiagnosticDate = null,
        string? erpZone = null)
    {
        if (dpeRating is not null) DpeRating = dpeRating;
        if (dpeValue.HasValue) DpeValue = dpeValue;
        if (gesRating is not null) GesRating = gesRating;

        if (electricDiagnosticDate.HasValue) ElectricDiagnosticDate = electricDiagnosticDate;
        if (electricDiagnosticExpiry.HasValue) ElectricDiagnosticExpiry = electricDiagnosticExpiry;

        if (gasDiagnosticDate.HasValue) GasDiagnosticDate = gasDiagnosticDate;
        if (gasDiagnosticExpiry.HasValue) GasDiagnosticExpiry = gasDiagnosticExpiry;

        if (hasAsbestos.HasValue) HasAsbestos = hasAsbestos;
        if (asbestosDiagnosticDate.HasValue) AsbestosDiagnosticDate = asbestosDiagnosticDate;

        if (erpZone is not null) ErpZone = erpZone;
    }
}
