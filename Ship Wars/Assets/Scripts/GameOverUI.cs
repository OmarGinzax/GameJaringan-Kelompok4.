using System.Collections;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Panels & Text")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI winnerText;

    [Header("Modul 11 - Score Display")]
    [SerializeField] private TextMeshProUGUI matchScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Controls")]
    [SerializeField] private Button restartButton;

    private bool _subscribed = false;

    private void Awake()
    {
        // Hide panel immediately (Awake runs before Start, safer)
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // Attempt to subscribe immediately; if GameManager isn't ready yet, retry via coroutine
        TrySubscribeToGameManager();
    }

    private void Update()
    {
        // Debug shortcut: Tekan 'O' untuk memicu GameOver secara manual untuk mempermudah testing UI
        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("[GameOverUI] Debug shortcut 'O' ditekan: Memicu GameOver manual.");
            HandleGameOver(ulong.MaxValue);
        }
    }

    private void TrySubscribeToGameManager()
    {
        if (_subscribed) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
            _subscribed = true;
            Debug.Log("[GameOverUI] Berhasil subscribe ke GameManager.OnGameOver.");
        }
        else
        {
            // GameManager belum siap (network object belum spawn), retry via coroutine
            Debug.LogWarning("[GameOverUI] GameManager.Instance belum ada. Memulai retry coroutine...");
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        // Polling setiap frame sampai GameManager.Instance tersedia (maks 10 detik)
        float elapsed = 0f;
        while (GameManager.Instance == null && elapsed < 10f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
            _subscribed = true;
            Debug.Log($"[GameOverUI] Subscribe berhasil setelah {elapsed:F2}s.");
        }
        else
        {
            Debug.LogError("[GameOverUI] GameManager.Instance tetap null setelah 10 detik. GameOver UI tidak akan berfungsi!");
        }
    }

    private void HandleGameOver(ulong winnerClientId)
    {
        Debug.Log($"[GameOverUI] HandleGameOver dipanggil. WinnerClientId: {winnerClientId}");

        // Aktifkan panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("[GameOverUI] GameOverPanel diaktifkan.");
        }
        else
        {
            Debug.LogError("[GameOverUI] gameOverPanel referensi NULL! Pastikan sudah di-assign di Inspector.");
            return;
        }

        // Tentukan WIN / LOSE berdasarkan local client ID
        if (winnerText != null)
        {
            if (InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Connection != null)
            {
                ulong localClientId = (ulong)InstanceFinder.ClientManager.Connection.ClientId;
                if (localClientId == winnerClientId)
                {
                    winnerText.text = "YOU WIN!";
                    winnerText.color = Color.green;
                }
                else
                {
                    winnerText.text = "YOU LOSE!";
                    winnerText.color = Color.red;
                }
            }
            else
            {
                // Fallback jika dipanggil di server tanpa koneksi client aktif
                winnerText.text = "GAME OVER";
                winnerText.color = Color.white;
            }
        }

        // Tampilkan skor pertandingan langsung (tidak perlu tunggu network)
        int currentMatchScore = 0;
        if (ScoreManager.Instance != null)
        {
            currentMatchScore = ScoreManager.Instance.CurrentScore;
        }

        if (matchScoreText != null)
        {
            matchScoreText.text = $"Match Score: {currentMatchScore}";
        }

        // Set placeholder highscore dulu agar UI tidak kosong
        if (highScoreText != null)
        {
            highScoreText.text = "Personal High Score: ...";
        }

        // Simpan skor ke backend, kemudian ambil high score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveCurrentScore(currentMatchScore, () =>
            {
                ScoreManager.Instance.GetPersonalHighScore((retrievedHighScore) =>
                {
                    if (highScoreText != null)
                    {
                        highScoreText.text = $"Personal High Score: {retrievedHighScore}";
                    }
                });
            });
        }
        else
        {
            Debug.LogWarning("[GameOverUI] ScoreManager.Instance null. Skor tidak akan disimpan ke backend.");
            if (highScoreText != null) highScoreText.text = "Personal High Score: N/A";
        }
    }

    private void OnRestartClicked()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.StopConnection(true);
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.StopConnection();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (_subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
    }
}
