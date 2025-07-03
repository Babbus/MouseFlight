using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Weapons
{
    /// <summary>
    /// Blind-Fire Missile - Simple projectile that flies straight
    /// No tracking, no radar required - pure ballistic flight
    /// Used when no radar system is available
    /// </summary>
    public class BlindFireMissile : MonoBehaviour
    {
        [Header("Blind-Fire Missile Properties")]
        [SerializeField] private float speed = 80f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifeTime = 8f; // Longer life for range
        
        private Rigidbody rb;
        private Vector3 flightDirection;
        private PrototypeShip owner;
        private float spawnTime;
        private bool hasExploded = false;

        public void Initialize(Vector3 startPos, Vector3 direction, float dmg, float spd, PrototypeShip ship)
        {
            transform.position = startPos;
            flightDirection = direction.normalized;
            damage = dmg;
            speed = spd;
            owner = ship;
            spawnTime = Time.time;
            
            rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
                
            rb.useGravity = false;
            
            // Set initial velocity in flight direction
            rb.linearVelocity = flightDirection * speed;
            
            // Point missile in flight direction
            transform.rotation = Quaternion.LookRotation(flightDirection);
            
            Destroy(gameObject, lifeTime);
            
            Debug.Log($"Blind-fire missile initialized: Direction={flightDirection}, Speed={speed}, Damage={damage}");
        }

        void FixedUpdate()
        {
            // Simple ballistic flight - maintain direction and speed
            rb.linearVelocity = flightDirection * speed;
            
            // Keep missile pointing in flight direction
            if (flightDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(flightDirection);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasExploded) return;
            
            // Check if we hit a ship
            PrototypeShip targetShip = other.GetComponent<PrototypeShip>();
            if (targetShip != null && targetShip != owner)
            {
                ApplyDamage(targetShip);
                Explode();
            }
            else if (other.gameObject.layer != gameObject.layer) // Hit environment
            {
                Explode();
            }
        }

        private void ApplyDamage(PrototypeShip targetShip)
        {
            DamageSystem targetDamageSystem = targetShip.GetComponent<DamageSystem>();
            if (targetDamageSystem != null)
            {
                targetDamageSystem.TakeDamage(damage, DamageType.Explosive, owner);
                Debug.Log($"Blind-fire missile hit {targetShip.name} for {damage} explosive damage!");
            }
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;
            
            // Create simple explosion effect
            GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosion.transform.position = transform.position;
            explosion.transform.localScale = Vector3.one * 1.5f;
            
            Renderer renderer = explosion.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.orange;
            }
            
            Destroy(explosion.GetComponent<Collider>());
            Destroy(explosion, 0.8f);
            
            Debug.Log("Blind-fire missile exploded!");
            Destroy(gameObject);
        }

        // Debug info
        public Vector3 GetFlightDirection() => flightDirection;
        public float GetSpeed() => speed;
        public float GetDamage() => damage;
    }
} 