using UnityEngine;
using UnityEngine.Events;

namespace BudeStudio
{
    /// <summary>
    /// Score keeper for starter maps. Static so scene objects can find it easily,
    /// plus an instance in the scene for inspector wiring.
    /// </summary>
    [AddComponentMenu("Bude Studio/Score Tracker")]
    public class ScoreTracker : MonoBehaviour
    {
        public static int Score { get; private set; }
        public static event System.Action<int> ScoreChanged;

        [Header("HUD (optional)")]
        [Tooltip("TextMesh that shows the score, e.g. 'SCORE 0'.")]
        public TextMesh hud;

        [Header("Win (optional)")]
        [Tooltip("Score needed to win. 0 = disabled.")]
        public int scoreToWin = 0;
        public UnityEvent onWin;

        private int _collected;

        private void Awake()
        {
            Score = 0;
            UpdateHud();
        }

        public static void Add(int amount)
        {
            Score += amount;
            ScoreChanged?.Invoke(Score);
        }

        private void OnEnable()
        {
            ScoreChanged += OnScoreChanged;
        }

        private void OnDisable()
        {
            ScoreChanged -= OnScoreChanged;
        }

        private void OnScoreChanged(int newScore)
        {
            _collected = newScore;
            UpdateHud();
            if (scoreToWin > 0 && newScore >= scoreToWin)
                onWin?.Invoke();
        }

        private void UpdateHud()
        {
            if (hud != null) hud.text = "SCORE " + Score;
        }
    }
}
