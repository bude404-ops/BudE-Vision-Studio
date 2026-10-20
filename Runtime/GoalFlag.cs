using UnityEngine;
using UnityEngine.SceneManagement;

namespace BudeStudio
{
    /// <summary>Goal flag. On touch: shows COURSE CLEAR and restarts (optional).</summary>
    [AddComponentMenu("Bude Studio/Goal Flag")]
    public class GoalFlag : MonoBehaviour
    {
        [Tooltip("Reload the scene a moment after winning?")]
        public bool restartSceneAfterWin = true;
        public float restartDelay = 2f;

        private bool _won;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null) Win();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController3D>() != null) Win();
        }

        private void Win()
        {
            if (_won) return;
            _won = true;
            Debug.Log("COURSE CLEAR! Nice run.");
#if UNITY_EDITOR
            Debug.Log("[Bude Studio] COURSE CLEAR - " + SceneManager.GetActiveScene().name);
#endif
            if (restartSceneAfterWin)
                Invoke(nameof(Restart), restartDelay);
        }

        private void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
