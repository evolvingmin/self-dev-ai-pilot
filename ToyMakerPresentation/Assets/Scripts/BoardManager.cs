namespace ToyProject
{
    using UnityEngine;

    public class BoardManager : MonoBehaviour, IGameModule
    {
        private const int boardSize = 3;
        private GameObject[,] board;
        public GameObject cardPrefab; // 카드 프리팹
        public Transform boardOrigin; // 보드 시작 위치
        public float tileSpacing = 1.5f; // 타일 간 간격
        private DataManager dataManager; // DataManager 참조 제거

        public void InitializeModule()
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                dataManager = gameManager.GetDataManager();
            }
            else
            {
                Debug.LogError("GameManager not found in the scene.");
            }

            board = new GameObject[boardSize, boardSize];
            Debug.Log("Board Initialized");
        }

        public void PlaceCard(int x, int y, int cardKey)
        {
            if (IsValidPlacement(x, y))
            {
                Vector3 position = GetTilePosition(x, y);
                GameObject cardObject = Instantiate(cardPrefab, position, Quaternion.identity, boardOrigin);
                Card card = cardObject.GetComponent<Card>();
                card.Initialize(cardKey, dataManager);
                board[x, y] = cardObject;
            }
        }

        private bool IsValidPlacement(int x, int y)
        {
            return x >= 0 && x < boardSize && y >= 0 && y < boardSize && board[x, y] == null;
        }

        private Vector3 GetTilePosition(int x, int y)
        {
            return boardOrigin.position + new Vector3(x * tileSpacing, 0, y * tileSpacing);
        }
    }
}