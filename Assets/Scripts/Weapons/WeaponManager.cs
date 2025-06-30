using UnityEngine;

namespace DomeClash.Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Default Weapons")]
        public PrimaryWeaponData defaultPrimary;
        public MissileWeaponData defaultSecondary;

        [Header("Primary Weapon Barrels")]
        public Transform[] primaryBarrels; // Inspector'dan atanacak
        private int barrelIndex = 0;

        [Header("Primary Weapon Aim")]
        [Tooltip("Cursor ile forward arasındaki deadzone açısı (derece)")]
        public float aimDeadzoneAngle = 10f;
        [Tooltip("Boresight mesafesi (uçak önünde hedef düzlemi)")]
        public float boresightDistance = 1000f;
        [Tooltip("Cursor'un hedeflediği dünya noktası için kullanılacak layer mask")]
        public LayerMask aimLayerMask = ~0; // Varsayılan: her şey

        private PrimaryWeaponData primary;
        private MissileWeaponData secondary;

        private float primaryFireTimer = 0f;
        private float lockProgress = 0f;
        private Transform lockedTarget;

        void Start()
        {
            EquipPrimary(defaultPrimary);
            EquipSecondary(defaultSecondary);
        }

        public void EquipPrimary(PrimaryWeaponData data)
        {
            primary = data;
        }

        public void EquipSecondary(MissileWeaponData data)
        {
            secondary = data;
        }

        public void FirePrimary()
        {
            if (primary == null || primaryBarrels == null || primaryBarrels.Length == 0) return;

            // Sıradaki barrel'ı seç
            Transform barrel = primaryBarrels[barrelIndex];
            barrelIndex = (barrelIndex + 1) % primaryBarrels.Length;

            // Hasarı bölüştür
            float splitDamage = primary.damage / primaryBarrels.Length;

            // Cursor'un işaret ettiği dünya yönünü bul (boresight düzlemi ile)
            Vector3 shootDirection = barrel.forward;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                // Uçağın önünde boresight düzlemi oluştur
                Vector3 planePoint = barrel.position + barrel.forward * boresightDistance;
                Vector3 planeNormal = barrel.forward;
                // Ray-plane kesişimi
                float denom = Vector3.Dot(ray.direction, planeNormal);
                if (Mathf.Abs(denom) > 1e-6f)
                {
                    float t = Vector3.Dot(planePoint - ray.origin, planeNormal) / denom;
                    if (t > 0)
                    {
                        Vector3 targetPoint = ray.origin + ray.direction * t;
                        Vector3 cursorDir = (targetPoint - barrel.position).normalized;
                        float angle = Vector3.Angle(barrel.forward, cursorDir);
                        if (angle < aimDeadzoneAngle)
                        {
                            shootDirection = cursorDir;
                        }
                        else
                        {
                            shootDirection = Vector3.RotateTowards(barrel.forward, cursorDir, aimDeadzoneAngle * Mathf.Deg2Rad, 0f);
                        }
                    }
                }
            }

            if (primary.projectilePrefab != null)
            {
                var go = Instantiate(primary.projectilePrefab, barrel.position, Quaternion.LookRotation(shootDirection));
                var proj = go.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Initialize(barrel.position, shootDirection, splitDamage, primary.projectileSpeed, null);
                }
                if (primary.fireSfx != null)
                    AudioSource.PlayClipAtPoint(primary.fireSfx, barrel.position);
            }
        }

        public void FireSecondary()
        {
            if (secondary == null) return;
            // Lock-on sistemi
            Collider[] hits = Physics.OverlapSphere(transform.position, secondary.lockRadius, secondary.targetMask);
            Transform bestTarget = null;
            float bestDot = 0.7f; // öne bakma açısı
            foreach (var hit in hits)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dir);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestTarget = hit.transform;
                }
            }
            if (bestTarget != null)
            {
                // Lock-on ilerlemesi (örnek, gerçek input ile bağlanabilir)
                lockProgress += Time.deltaTime / secondary.lockTime;
                if (lockProgress >= 1f)
                {
                    // Homing missile fırlat
                    var go = Instantiate(secondary.projectilePrefab, transform.position + transform.forward * 2f, transform.rotation);
                    var missile = go.GetComponent<HomingMissile>();
                    if (missile != null)
                    {
                        missile.Initialize(transform.position + transform.forward * 2f, bestTarget, secondary.damage, 80f, null);
                    }
                    if (secondary.fireSfx != null)
                        AudioSource.PlayClipAtPoint(secondary.fireSfx, transform.position);
                    lockProgress = 0f;
                }
            }
            else
            {
                // Hedef yoksa düz füze fırlat
                var go = Instantiate(secondary.projectilePrefab, transform.position + transform.forward * 2f, transform.rotation);
                var proj = go.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Initialize(transform.position + transform.forward * 2f, transform.forward, secondary.damage, 80f, null);
                }
                if (secondary.fireSfx != null)
                    AudioSource.PlayClipAtPoint(secondary.fireSfx, transform.position);
                lockProgress = 0f;
            }
        }
    }
} 