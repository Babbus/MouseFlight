using UnityEngine;

namespace DomeClash.Weapons
{
    public class HomingMissile : MonoBehaviour
    {
        public Transform target;
        public float speed = 80f;
        public float rotateSpeed = 200f;
        public float damage = 100f;
        public float lifeTime = 5f;

        private Rigidbody rb;
        private Vector3 initialDirection;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            Destroy(gameObject, lifeTime);
            initialDirection = transform.forward;
        }

        void FixedUpdate()
        {
            if (target == null)
            {
                rb.linearVelocity = initialDirection * speed;
                rb.angularVelocity = Vector3.zero;
                return;
            }

            Vector3 direction = (target.position - transform.position).normalized;
            Vector3 rotateAmount = Vector3.Cross(transform.forward, direction);
            rb.angularVelocity = rotateAmount * rotateSpeed * Mathf.Deg2Rad;
            rb.linearVelocity = transform.forward * speed;
        }

        void OnTriggerEnter(Collider other)
        {
            // Check if we hit a ship
            DomeClash.Core.ShipClass targetShip = other.GetComponent<DomeClash.Core.ShipClass>();
            if (targetShip != null)
            {
                // Apply explosive damage
                DomeClash.Core.DamageSystem targetDamageSystem = targetShip.GetComponent<DomeClash.Core.DamageSystem>();
                if (targetDamageSystem != null)
                {
                    targetDamageSystem.TakeDamage(damage, DomeClash.Core.DamageType.Explosive);
                }
                else
                {
                    Debug.Log($"Homing Missile hit {targetShip.name} for {damage} explosive damage!");
                }
            }
            
            // Create explosion effect (placeholder)
            CreateExplosionEffect();
            
            Destroy(gameObject);
        }
        
        private void CreateExplosionEffect()
        {
            // Create simple explosion effect
            GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosion.transform.position = transform.position;
            explosion.transform.localScale = Vector3.one * 2f;
            
            Renderer renderer = explosion.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.orange;
            }
            
            Destroy(explosion.GetComponent<Collider>());
            Destroy(explosion, 0.3f);
        }

        public void Initialize(Vector3 startPos, Transform tgt, float dmg, float spd, DomeClash.Core.ShipClass ship)
        {
            transform.position = startPos;
            target = tgt;
            damage = dmg;
            speed = spd;
            // ship parametresi şimdilik kullanılmıyor
        }
    }
} 