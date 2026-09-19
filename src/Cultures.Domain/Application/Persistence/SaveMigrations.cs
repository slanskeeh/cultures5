namespace Cultures.Application.Persistence;

/// <summary>
/// Forward-only envelope migrations. v2 stays header-only.
/// </summary>
public static class SaveMigrations
{
    public static SaveEnvelope ToCurrent(SaveEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.SaveVersion == 2 || envelope.SaveVersion == SaveEnvelope.CurrentVersion)
            return envelope;

        if (envelope.SaveVersion == 3)
        {
            return envelope with
            {
                SaveVersion = 4,
                Pacts = envelope.Pacts ?? [],
                Speed = envelope.Speed < 1 ? 1 : envelope.Speed,
                OnboardingComplete = envelope.OnboardingComplete
            };
        }

        throw new InvalidOperationException($"Unsupported save version {envelope.SaveVersion}.");
    }
}
