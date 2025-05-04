using UnityEngine;

public class Card : MonoBehaviour
{
    public string cardName;
    public int attack;
    public int defense;

    public void Initialize(string name, int atk, int def)
    {
        cardName = name;
        attack = atk;
        defense = def;
    }
}