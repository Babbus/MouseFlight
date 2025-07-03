using UnityEngine;
using DomeClash.Core;
using DomeClash.Ships;

namespace DomeClash.Weapons
{
    /// <summary>
    /// Pulse Laser - Instant-hit hitscan energy weapon
    /// High accuracy, short cooldown. Excels at poking and finisher shots.
    /// </summary>
    public class PulseLaser : WeaponSystem
    {
        [Header("Pulse Laser Settings")]
        [SerializeField] protected LineRenderer laserLine;
        [SerializeField] protected Light muzzleLight;
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected ParticleSystem hitEffect;
        [SerializeField] protected AudioClip laserSound;
        [SerializeField] protected float laserDuration = 0.1f;
        [SerializeField] protected LayerMask hitLayers = -1;
        private float laserTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            
            weaponType = WeaponType.PulseLaser;
            
            // AudioSource is inherited from WeaponSystem base class
                
            // Setup laser line
            if (laserLine == null)
                laserLine = GetComponent<LineRenderer>();
            if (laserLine == null)
                laserLine = gameObject.AddComponent<LineRenderer>();
                
            SetupLaserLine();
        }

        private void SetupLaserLine()
        {
            if (laserLine != null)
            {
                laserLine.material = CreateLaserMaterial();
                laserLine.startWidth = 0.1f;
                laserLine.endWidth = 0.05f;
                laserLine.positionCount = 2;
                laserLine.enabled = false;
                laserLine.useWorldSpace = true;
            }
        }

        private Material CreateLaserMaterial()
        {
            Material laserMat = new Material(Shader.Find("Sprites/Default"));
            laserMat.color = Color.cyan;
            return laserMat;
        }

        protected override void Update()
        {
            base.Update();
            UpdateLaserVisuals();
        }

        private void UpdateLaserVisuals()
        {
            if (laserTimer > 0f)
            {
                laserTimer -= Time.deltaTime;
                
                if (laserTimer <= 0f)
                {
                    // Hide laser
                    if (laserLine != null)
                        laserLine.enabled = false;
                    if (muzzleLight != null)
                        muzzleLight.enabled = false;
                }
            }
        }

        protected override void Fire()
        {
            base.Fire();
            
            // Perform hitscan
            PerformHitscan();
        }

        private void PerformHitscan()
        {
            Vector3 startPos = firePoint.position;
            Vector3 direction = firePoint.forward;
            
            // Perform raycast
            RaycastHit hit;
            Vector3 endPos;
            bool hitSomething = Physics.Raycast(startPos, direction, out hit, stats.range, hitLayers);
            
            if (hitSomething)
            {
                endPos = hit.point;
                
                // Apply damage
                PrototypeShip targetShip = hit.collider.GetComponent<PrototypeShip>();
                if (targetShip != null && targetShip != ownerShip)
                {
                    // Apply damage to target
                    ApplyDamage(targetShip, stats.damage);
                }
                
                // Create hit effect
                CreateHitEffect(hit.point, hit.normal);
            }
            else
            {
                endPos = startPos + direction * stats.range;
            }
            
            // Show laser beam
            ShowLaserBeam(startPos, endPos);
            
            // Play effects
            PlayLaserEffects();
        }

        private void ApplyDamage(PrototypeShip target, float damage)
        {
            // Apply energy damage to target's damage system
            DamageSystem targetDamageSystem = target.GetComponent<DamageSystem>();
            if (targetDamageSystem != null)
            {
                targetDamageSystem.TakeDamage(damage, DamageType.Energy, ownerShip);
            }
            else
            {
                Debug.Log($"PulseLaser hit {target.name} for {damage} energy damage!");
            }
        }

        protected virtual void CreateHitEffect(Vector3 position, Vector3 normal)
        {
            if (hitEffect != null)
            {
                ParticleSystem effect = Instantiate(hitEffect, position, Quaternion.LookRotation(normal));
                Destroy(effect.gameObject, 2f);
            }
            else
            {
                // Create simple hit effect
                GameObject hitSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hitSphere.transform.position = position;
                hitSphere.transform.localScale = Vector3.one * 0.3f;
                
                Renderer renderer = hitSphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.yellow;
                    renderer.material.SetFloat("_Metallic", 0.8f);
                }
                
                Destroy(hitSphere.GetComponent<Collider>());
                Destroy(hitSphere, 0.5f);
            }
        }

        protected virtual void ShowLaserBeam(Vector3 startPos, Vector3 endPos)
        {
            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.SetPosition(0, startPos);
                laserLine.SetPosition(1, endPos);
                laserTimer = laserDuration;
            }
        }

        private void PlayLaserEffects()
        {
            // Play sound
            if (laserSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(laserSound);
            }
            
            // Play muzzle flash
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            // Enable muzzle light briefly
            if (muzzleLight != null)
            {
                muzzleLight.enabled = true;
            }
        }

        protected override GameObject CreateProjectileObject()
        {
            // Pulse laser doesn't use projectiles - it's hitscan
            return null;
        }

        protected override bool CanFire()
        {
            // Pulse laser specific firing conditions
            if (Time.time - lastFireTime < 1f / stats.fireRate) return false;
            if (currentHeat >= stats.maxHeat) return false;
            
            return true;
        }

        // Public methods for weapon stats
        public void SetLaserColor(Color color)
        {
            if (laserLine != null && laserLine.material != null)
            {
                laserLine.material.color = color;
            }
        }

        public void SetLaserWidth(float startWidth, float endWidth)
        {
            if (laserLine != null)
            {
                laserLine.startWidth = startWidth;
                laserLine.endWidth = endWidth;
            }
        }
    }
} 