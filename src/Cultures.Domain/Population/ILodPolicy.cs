namespace Cultures.Population;

/// <summary>
/// Optional policy for skipping individual simulation. Null means everyone is detailed.
/// </summary>
public interface ILodPolicy
{
    bool SimulateBody(CharacterState character);
    bool SimulateBehavior(CharacterState character);
}
