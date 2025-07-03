using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Weapons
{
    /// <summary>
    /// Simple missile for testing - no complex tracking
    /// </summary>
    public class SimpleMissile : MonoBehaviour
    {
        [Header("Simple Missile")]
        [SerializeField] private float speed = 50f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifetime = 5f;
        
        private Rigidbody rb;
        private Vector3 direction;
        private PrototypeShip owner;
        private bool initialized = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
                
            rb.useGravity = false;
            rb.mass = 0.1f;
        }

        public void Initialize(Vector3 startPos, Vector3 dir, float dmg, float spd, PrototypeShip ship)
        {
            transform.position = startPos;
            direction = dir.normalized;
            damage = dmg;
            speed = spd;
            owner = ship;
            initialized = true;
            
            // Set initial velocity
            if (rb != null)
                rb.linearVelocity = direction * speed;
            
            // Auto destroy after lifetime
            Destroy(gameObject, lifetime);
            
            Debug.Log($"Simple missile initialized: Speed={speed}, Damage={damage}");
        }

        void FixedUpdate()
        {
            if (!initialized || rb == null) return;
            
            // Keep moving in direction
            rb.linearVelocity = direction * speed;
        }

        void OnTriggerEnter(Collider other)
        {
            // Check if we hit a ship
            PrototypeShip targetShip = other.GetComponent<PrototypeShip>();
            if (targetShip != null && targetShip != owner)
            {
                // Apply damage
                DamageSystem damageSystem = targetShip.GetComponent<DamageSystem>();
                if (damageSystem != null)
                {
                    damageSystem.TakeDamage(damage, DamageType.Explosive, owner);
                    Debug.Log($"Simple missile hit {targetShip.name} for {damage} damage!");
                }
                
                Explode();
            }
            else if (other.gameObject.layer != gameObject.layer) // Hit environment
            {
                Explode();
            }
        }

        private void Explode()
        {
            // Simple explosion effect
            Debug.Log("Simple missile exploded!");
            Destroy(gameObject);
        }
    }
} 