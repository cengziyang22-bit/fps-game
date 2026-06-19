using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Score")]
    public Text scoreText;

    [Header("Health")]
    public Image healthFill;
    public Text healthNumber;
    public Color healthHigh = Color.green;
    public Color healthMid = new Color(0.95f, 0.6f, 0.07f);
    public Color healthLow = Color.red;

    [Header("Ammo")]
    public Text ammoCurrent;
    public Text ammoReserve;
    public GameObject ammoDisplay;

    [Header("Hit Marker")]
    public Image hitMarker;

    [Header("Damage Flash")]
    public Image damageFlash;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public Text gameOverScore;

    [Header("Crosshair")]
    public GameObject crosshair;

    [Header("Weapon Info")]
    public Text weaponInfo;

    private WeaponManager weaponManager;
    private PlayerHealth playerHealth;
    private float redFlash;
    private float shakeAmount;
    private float fpsTimer;
    private int fpsFrameCount;
    private int displayFPS;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        weaponManager = FindObjectOfType<WeaponManager>();
        playerHealth = FindObjectOfType<PlayerHealth>();

        if (weaponManager != null)
            weaponManager.OnAmmoChanged += UpdateAmmo;

        UpdateAmmo();
        UpdateHealth(100, 100);
        UpdateScore(0);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hitMarker != null) hitMarker.gameObject.SetActive(false);
        if (crosshair != null) crosshair.SetActive(true);
    }

    void Update()
    {
        // Damage flash decay
        if (redFlash > 0.01f)
        {
            redFlash *= Mathf.Exp(-6f * Time.deltaTime);
            if (damageFlash != null)
                damageFlash.color = new Color(1, 0, 0, Mathf.Min(redFlash, 0.7f));
        }
        else if (redFlash > 0)
        {
            redFlash = 0;
            if (damageFlash != null) damageFlash.color = new Color(1, 0, 0, 0);
        }

        // Shake decay
        if (shakeAmount > 0.001f)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.localPosition += Random.insideUnitSphere * shakeAmount;
                shakeAmount *= Mathf.Exp(-12f * Time.deltaTime);
            }
        }

        // FPS counter
        fpsTimer += Time.unscaledDeltaTime;
        fpsFrameCount++;
        if (fpsTimer >= 0.5f)
        {
            displayFPS = Mathf.RoundToInt(fpsFrameCount / fpsTimer);
            fpsTimer = 0;
            fpsFrameCount = 0;
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = new Color(0.3f, 1f, 0.3f);
        GUI.Label(new Rect(10, 10, 120, 30), $"FPS: {displayFPS}", style);
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"击杀: {score}";
    }

    public void UpdateHealth(int current, int max)
    {
        if (healthFill != null)
        {
            healthFill.fillAmount = (float)current / max;
            healthFill.color = current > 60 ? healthHigh : current > 30 ? healthMid : healthLow;
        }
        if (healthNumber != null)
            healthNumber.text = current.ToString();
    }

    public void UpdateAmmo()
    {
        if (weaponManager == null) return;

        bool reloading = weaponManager.isReloading;
        if (ammoCurrent != null)
            ammoCurrent.text = reloading ? "..." : weaponManager.currentAmmo.ToString();
        if (ammoReserve != null)
            ammoReserve.text = weaponManager.currentReserve.ToString();

        if (ammoDisplay != null)
        {
            var texts = ammoDisplay.GetComponentsInChildren<Text>();
            if (reloading)
            {
                foreach (var t in texts) t.color = new Color(0.95f, 0.6f, 0.07f);
            }
            else if (weaponManager.currentAmmo <= 5)
            {
                foreach (var t in texts) t.color = Color.red;
            }
            else
            {
                foreach (var t in texts) t.color = Color.white;
            }
        }

        if (weaponInfo != null && weaponManager.CurrentWeapon != null)
        {
            weaponInfo.text = $"[{weaponManager.currentWeaponIndex + 1}] {weaponManager.CurrentWeapon.weaponName}";
        }
    }

    public void TriggerHitMarker()
    {
        if (hitMarker == null) return;
        StopAllCoroutines();
        StartCoroutine(FlashHitMarker());
    }

    System.Collections.IEnumerator FlashHitMarker()
    {
        hitMarker.gameObject.SetActive(true);
        hitMarker.transform.localScale = Vector3.one * 1.3f;
        yield return new WaitForSeconds(0.05f);
        hitMarker.transform.localScale = Vector3.one * 0.5f;
        hitMarker.color = new Color(1, 1, 1, 0.5f);
        yield return new WaitForSeconds(0.15f);
        hitMarker.gameObject.SetActive(false);
        hitMarker.color = Color.white;
        hitMarker.transform.localScale = Vector3.one;
    }

    public void TriggerDamageFlash()
    {
        redFlash = 0.25f;
    }

    public void TriggerShake(float amount)
    {
        shakeAmount = Mathf.Max(shakeAmount, amount);
    }

    public void ShowGameOver(int score)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverScore != null)
                gameOverScore.text = $"击杀: {score}";
        }
        if (crosshair != null) crosshair.SetActive(false);
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (crosshair != null) crosshair.SetActive(true);
    }

    void OnDestroy()
    {
        if (weaponManager != null)
            weaponManager.OnAmmoChanged -= UpdateAmmo;
    }
}
