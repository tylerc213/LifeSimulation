using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    public void HandleInteractions(Creature source, Creature target)
    {
        if (source is Predator predator && target is Grazer grazer)
        {
            predator.Attack(grazer);
        }

        if (source is Grazer g && target is Plant plant)
        {
            g.EatPlant(plant);
        }
    }
}