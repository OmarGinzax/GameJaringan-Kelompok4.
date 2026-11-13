using System;
using System.Collections;
using System.Text;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Backend REST Configuration (Modul 11)")]
    [SerializeField] private string baseURL = "http://localhost:5000/api";

    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int currentScoreDebug = 0;

    private readonly SyncVar<int> _score = new SyncVar<int>(0);
    public int CurrentScore => _score.Value;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Cari UI di Scene berdasarkan OwnerId (0 = Host/Player1, 1 = Client/Player2)
        int playerNum = OwnerId == 0 ? 1 : 2;
        GameObject scoreGo = GameObject.Find($"PlayerHUD/Player{playerNum}/Score");
        if (scoreGo == null) scoreGo = GameObject.Find($"Player{playerNum}/Score"); // Fallback

        if (scoreGo != null)
        {
            scoreText = scoreGo.GetComponent<TextMeshProUGUI>();
        }

        _score.OnChange += OnScoreChanged;

        UpdateScoreUI(_score.Value);

        // Jika local player, jadikan Instance aktif
        if (IsOwner)
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        _score.OnChange -= OnScoreChanged;
    }

    private void OnScoreChanged(int previousValue, int newValue, bool asServer)
    {
        currentScoreDebug = newValue;
        UpdateScoreUI(newValue);
    }

    private void UpdateScoreUI(int currentScore)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Simulasi input keyboard untuk pengujian
        // Tekan 'L' untuk menambah Score
        if (Input.GetKeyDown(KeyCode.L))
        {
            AddScoreServerRpc(5);
        }
    }

    [ServerRpc]
    private void AddScoreServerRpc(int scoreAmount)
    {
        _score.Value += scoreAmount;
    }

    // ==========================================
    // METODE REST API (MODUL 11)
    // ==========================================

    // Method 1: Mengirim skor pertandingan terkini ke backend
    public void SaveCurrentScore(int finalScore, Action onComplete = null)
    {
        int activeUserId = PlayerPrefs.GetInt("ACTIVE_USER_ID", -1);
        if (activeUserId == -1)
        {
            activeUserId = 1;
            PlayerPrefs.SetInt("ACTIVE_USER_ID", 1);
            Debug.Log("[ScoreManager] ACTIVE_USER_ID tidak ditemukan. Menggunakan default testing UserID: 1");
        }

        StartCoroutine(SendScoreRequest(activeUserId, finalScore, onComplete));
    }

    private IEnumerator SendScoreRequest(int userId, int score, Action onComplete)
    {
        SaveScoreRequest reqData = new SaveScoreRequest { userId = userId, score = score };
        string jsonPayload = JsonUtility.ToJson(reqData);

        using (UnityWebRequest request = new UnityWebRequest(baseURL + "/score/save", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SaveScoreResponse resp = JsonUtility.FromJson<SaveScoreResponse>(request.downloadHandler.text);
                Debug.Log($"[ScoreManager] Skor Berhasil Disimpan! Score ID: {resp.scoreId}");
            }
            else
            {
                Debug.LogError($"[ScoreManager] Gagal menyimpan skor: {request.error}");
            }

            onComplete?.Invoke();
        }
    }

    // Method 2: Meminta skor tertinggi pemain dari backend
    public void GetPersonalHighScore(Action<int> onCallback)
    {
        int activeUserId = PlayerPrefs.GetInt("ACTIVE_USER_ID", -1);
        if (activeUserId == -1)
        {
            activeUserId = 1;
            PlayerPrefs.SetInt("ACTIVE_USER_ID", 1);
        }

        StartCoroutine(FetchHighScoreRequest(activeUserId, onCallback));
    }

    private IEnumerator FetchHighScoreRequest(int userId, Action<int> onCallback)
    {
        string url = $"{baseURL}/score/highscore/{userId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                HighScoreResponse resp = JsonUtility.FromJson<HighScoreResponse>(request.downloadHandler.text);
                Debug.Log($"[ScoreManager] High Score Terambil: {resp.highScore}");
                onCallback?.Invoke(resp.highScore);
            }
            else
            {
                Debug.LogError($"[ScoreManager] Gagal mengambil high score: {request.error}");
                onCallback?.Invoke(0);
            }
        }
    }
}
