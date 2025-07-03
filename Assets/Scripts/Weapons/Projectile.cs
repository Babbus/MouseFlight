using UnityEngine;
using DomeClash.Core;

namespace DomeClash.Weapons
{
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] protected float lifetime = 10f;
        [SerializeField] protected GameObject impactEffect;
        [SerializeField] protected LayerMask hitLayers = -1;
        
        // Runtime variables
        protected Vector3 direction;
        protected float damage;
        protected float speed;
        protected ShipClass owner;
        protected float spawnTime;
        protected bool hasHit = false;
        protected DamageType damageType = DamageType.Kinetic;
        protected bool isCritical = false;
        protected float knockbackForce = 0f;
        protected int penetrationsLeft = 0;

        public virtual void Initialize(Vector3 startPos, Vector3 dir, float dmg, float spd, ShipClass ship)
        {
            transform.position = startPos;
            direction = dir.normalized;
            damage = dmg;
            speed = spd;
            owner = ship;
            spawnTime = Time.time;
            hasHit = false;
            
            // Set velocity
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }
        }

        protected virtual void Update()
        {
            // Check lifetime
            if (Time.time - spawnTime > lifetime)
            {
                DestroyProjectile();
                return;
            }

            // Check for hits
            CheckForHits();
        }

        private void CheckForHits()
        {
            if (hasHit) return;

            // Raycast forward to detect hits
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, speed * Time.deltaTime, hitLayers))
            {
                OnHit(hit);
            }
        }

        protected virtual void OnHit(RaycastHit hit)
        {
            hasHit = true;

            // Spawn impact effect
            if (impactEffect != null)
            {
                GameObject effect = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 2f);
            }

            // Destroy projectile
            DestroyProjectile();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // Check if we hit a ship
            ShipClass targetShip = other.GetComponent<ShipClass>();
            if (targetShip != null && targetShip != owner)
            {
                hasHit = true;
                
                // Apply damage
                DamageSystem targetDamageSystem = targetShip.GetComponent<DamageSystem>();
                if (targetDamageSystem != null)
                {
                    targetDamageSystem.TakeDamage(damage, damageType, owner);
                }
                else
                {
                    string critText = isCritical ? " CRITICAL" : "";
                    Debug.Log($"Projectile hit {targetShip.name} for {damage}{critText} {damageType} damage!");
                }
                
                // Apply knockback if any
                if (knockbackForce > 0f)
                {
                    Rigidbody targetRb = targetShip.GetComponent<Rigidbody>();
                    if (targetRb != null)
                    {
                        Vector3 knockbackDirection = direction.normalized;
                        targetRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                    }
                }
                
                // Spawn impact effect
                if (impactEffect != null)
                {
                    GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
                    Destroy(effect, 2f);
                }
                
                DestroyProjectile();
            }
        }

        protected virtual void DestroyProjectile()
        {
            Destroy(gameObject);
        }
        
        // Public methods for weapon system integration
        public virtual void SetDamageType(DamageType type)
        {
            damageType = type;
        }
        
        public virtual void SetCritical(bool critical)
        {
            isCritical = critical;
        }
        
        public virtual void SetKnockbackForce(float force)
        {
            knockbackForce = force;
        }
        
        public virtual void SetPenetration(int maxPenetrations)
        {
            penetrationsLeft = maxPenetrations;
        }
    }
} 