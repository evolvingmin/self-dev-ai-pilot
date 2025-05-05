using ToyProject.Data;
using UnityEngine;

public class Card : MonoBehaviour
{
    public string cardName;
    public int attack;
    public int defense;

    public void Initialize(int id, DataManager dataManager)
    {
        // Assuming dataManager has methods to get card details
        // For example: dataManager.GetCardDetails(name);
        // Here we just set the values directly for simplicity
        var cardSpec = dataManager.GetData<CardSpec>(id); 
        cardName = cardSpec.cardName;
        defense = cardSpec.defense;
        attack = cardSpec.attack;
        Debug.Log($"Initialized card: {cardName} with Attack: {attack} and Defense: {defense}");
    }
}