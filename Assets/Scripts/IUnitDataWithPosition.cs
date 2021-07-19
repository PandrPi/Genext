using Unity.Mathematics;

/// <summary>
/// Represents an interface that has a GetPosition method. This interface is used only for Food and Creature Components.
/// A unit word in the name of this interface represents an entity which is placed in the game world (food, creature)
/// </summary>
public interface IUnitDataWithPosition
{
    float2 GetPosition();
}