using UnityEngine;
using UnityEngine.UI;

public class PlayerShoot : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public WeaponManager weaponManager;
    public LayerMask hitMask = ~0;
    public Transform muzzlePoint;

    [Header("Effects")]
    public GameObject muzzleFlashPrefab;
    public GameObject bulletTrailPrefab;
    public GameObject hitParticlePrefab;
    public GameObject damageNumberPrefab;

    [Header("Enemy Layer")]
    public LayerMask enemyLayer;

    private float shootCooldown;
    private bool mouseHeld;

    void Start()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (weaponManager == null) weaponManager = GetComponentInParent<WeaponManager>();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.gameOver) return;

        if (weaponManager == null) weaponManager = GetComponentInParent<WeaponManager>();

        shootCooldown = Mathf.Max(0, shootCooldown - Time.deltaTime);

        if (Input.GetMouseButtonDown(0)) mouseHeld = true;
        if (Input.GetMouseButtonUp(0)) mouseHeld = false;

        if (mouseHeld && shootCooldown <= 0 && !weaponManager.isReloading)
        {
            WeaponData wp = weaponManager.CurrentWeapon;
            int pellets = wp.isShotgun ? wp.pellets : 1;

            if (!weaponManager.ConsumeAmmo()) return;

            shootCooldown = wp.fireRate;
            StartCoroutine(FlashMuzzle());

            for (int p = 0; p < pellets; p++)
            {
                FireBullet(wp);
            }
        }
    }

    void FireBullet(WeaponData wp)
    {
        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : playerCamera.transform.position;
        Vector3 dir = GetSpreadDirection(wp);

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(origin, dir, out hit, 200f, hitMask);

        Vector3 endPoint = hitSomething ? hit.point : origin + dir * 200f;

        if (bulletTrailPrefab != null)
            SpawnTrail(origin, endPoint);

        if (hitSomething)
        {
            EnemyController enemy = hit.collider.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                BodyPart bodyPart = hit.collider.GetComponent<BodyPart>();
                float mult = bodyPart != null ? bodyPart.damageMultiplier : 1.5f;
                int dmg = Mathf.RoundToInt(wp.baseDamage * mult);
                enemy.TakeDamage(dmg);
                SpawnDamageNumber(hit.point, dmg, mult >= 3f);
                SpawnHitParticles(hit.point, Color.red);
            }
            else
            {
                SpawnHitParticles(hit.point, Color.yellow);
            }
        }
    }

    Vector3 GetSpreadDirection(WeaponData wp)
    {
        var pc = weaponManager.GetComponent<PlayerController>();
        float spread = wp.spread;

        if (pc != null && pc.isADS) spread *= 0.15f;
        if (pc != null && pc.isCrouching) spread *= 0.7f;

        Vector3 dir = playerCamera.transform.forward;
        dir += playerCamera.transform.right * Random.Range(-1f, 1f) * spread;
        dir += playerCamera.transform.up * Random.Range(-1f, 1f) * spread;
        return dir.normalized;
    }

    System.Collections.IEnumerator FlashMuzzle()
    {
        if (muzzleFlashPrefab != null)
        {
            var flash = Instantiate(muzzleFlashPrefab, muzzlePoint);
            flash.transform.localPosition = Vector3.zero;
            flash.transform.localRotation = Quaternion.identity;
            Destroy(flash, 0.05f);
        }
        yield return null;
    }

    void SpawnTrail(Vector3 start, Vector3 end)
    {
        var trail = Instantiate(bulletTrailPrefab);
        var bt = trail.GetComponent<BulletTrail>();
        if (bt != null) bt.Init(start, end);
        else Destroy(trail, 0.2f);
    }

    void SpawnHitParticles(Vector3 pos, Color color)
    {
        if (hitParticlePrefab == null) return;
        var particles = Instantiate(hitParticlePrefab, pos, Quaternion.identity);
        var ps = particles.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }
        Destroy(particles, 1f);
    }

    void SpawnDamageNumber(Vector3 pos, int damage, bool isHeadshot)
    {
        if (damageNumberPrefab == null) return;
        var dn = Instantiate(damageNumberPrefab, pos + Vector3.up * 0.5f, Quaternion.identity);
        var tmp = dn.GetComponentInChildren<Text>();
        if (tmp != null)
        {
            tmp.text = damage.ToString();
            tmp.fontSize = 8;
            tmp.color = isHeadshot ? Color.red : Color.yellow;
        }

        if (Camera.main != null)
        {
            Vector3 toCamera = dn.transform.position - Camera.main.transform.position;
            dn.transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);

            // 根据距离缩放，保持屏幕上的视觉大小一致
            float dist = toCamera.magnitude;
            float scale = 0.0015f * (dist / 3f);
            dn.transform.localScale = Vector3.one * scale;
        }

        Destroy(dn, 1f);
    }
}
