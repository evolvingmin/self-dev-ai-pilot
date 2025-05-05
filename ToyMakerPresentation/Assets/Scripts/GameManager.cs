using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private List<MonoBehaviour> gameModules; // IGameModule을 구현한 모듈들을 인스펙터에서 연결

    private DataManager dataManager;

    void Start()
    {
        dataManager = new DataManager();
        InitializeGame();
    }

    private void InitializeGame()
    {
        foreach (var module in gameModules)
        {
            if (module is IGameModule gameModule)
            {
                gameModule.InitializeModule();
            }
            else
            {
                Debug.LogError($"{module.name} does not implement IGameModule.");
            }
        }

        Debug.Log("Game Initialized");
    }

    public DataManager GetDataManager()
    {
        return dataManager;
    }
}