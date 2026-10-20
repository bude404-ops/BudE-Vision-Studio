using UnityEngine;
using UnityEngine.Events;

namespace BudeStudio
{
    /// <summary>Collectible pickup. Adds score and optionally destroys itself.</summary>
    [AddComponentMenu("Bude Studio/Collectible")]
    public class Collectible : MonoBehaviour
    {
        [Tooltip("Points this pickup is worth.")]
        public int scoreValue = 1;
        public bool destroyOnPickup = true;

        [Tooltip("Optional: fired when collected (wire in the inspector).")]
        public UnityEvent onCollected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null) return;
            Collect();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController3D>() == null) return;
            Collect();
        }

        private void Collect()
        {
            ScoreTracker.Add(scoreValue);
            onCollected?.Invoke();
            if (destroyOnPickup) Destroy(gameObject);
        }
    }
}
