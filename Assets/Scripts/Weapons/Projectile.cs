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

        public void Initialize(Vector3 startPos, Vector3 dir, float dmg, float spd, ShipClass ship)
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

        private void Update()
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
    }

    public class HomingMissile : Projectile
    {
        [Header("Homing Settings")]
        [SerializeField] private float turnRate = 180f;
        [SerializeField] private float maxTurnAngle = 45f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float maxSpeed = 150f;
        
        private Transform target;
        private float currentSpeed;
        private bool isInitialized = false;

        public void Initialize(Vector3 startPos, Transform tgt, float dmg, float spd, ShipClass ship)
        {
            transform.position = startPos;
            target = tgt;
            damage = dmg;
            speed = spd;
            owner = ship;
            currentSpeed = speed;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || target == null) return;

            // Update homing behavior
            UpdateHoming();
            
            // Check lifetime
            if (Time.time - spawnTime > lifetime)
            {
                DestroyProjectile();
            }
        }

        private void UpdateHoming()
        {
            if (target == null) return;

            // Calculate direction to target
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            
            // Calculate current forward direction
            Vector3 currentDirection = transform.forward;
            
            // Calculate angle between current direction and target direction
            float angle = Vector3.Angle(currentDirection, directionToTarget);
            
            // Limit turn angle
            if (angle > maxTurnAngle)
            {
                directionToTarget = Vector3.RotateTowards(currentDirection, directionToTarget, 
                    maxTurnAngle * Mathf.Deg2Rad, 0f);
            }
            
            // Smoothly rotate towards target
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 
                turnRate * Time.deltaTime);
            
            // Accelerate
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            
            // Update velocity
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * currentSpeed;
            }
        }

        protected override void OnHit(RaycastHit hit)
        {
            hasHit = true;

            // Spawn explosion effect
            if (impactEffect != null)
            {
                GameObject effect = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 3f);
            }

            // Destroy projectile
            DestroyProjectile();
        }
    }
} 