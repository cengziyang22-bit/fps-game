using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public WeaponData[] weapons = new WeaponData[7];
    public GameObject[] weaponModels = new GameObject[7];

    [Header("ViewModel Normalization")]
    public float[] weaponSizes = new float[7]
    {
        0.18f,  // 0 Pistol   (缩小)
        0.24f,  // 1 MP5      (缩小)
        0.36f,  // 2 AKM      (放大)
        0.40f,  // 3 MK14     (放大)
        0.38f,  // 4 AA-12
        0.44f,  // 5 AWM
        0.40f,  // 6 M249
    };
    public Vector3 viewModelLocalCenter = Vector3.zero;

    // 模型自身的基础旋转（枪口朝+Z为准）
    public Vector3[] weaponModelEuler = new Vector3[7]
    {
        new Vector3(90f, 180f, 0f),  // 0 Pistol
        new Vector3(0f, 0f, 0f),     // 1 MP5
        new Vector3(-90f, 180f, 0f), // 2 AKM
        new Vector3(0f, 90f, 0f),    // 3 MK14
        new Vector3(0f, 90f, 0f),    // 4 AA-12
        new Vector3(90f, 180f, 0f),  // 5 AWM
        new Vector3(-90f, 0f, 0f),   // 6 M249
    };

    // 用户调好的最终修正旋转（绕几何中心）
    public Vector3[] pivotEuler = new Vector3[7]
    {
        new Vector3(353.3f, 335.0f, 179.2f), // 0 Pistol
        new Vector3(23.3f, 190.8f, 267.5f),  // 1 MP5
        new Vector3(350.8f, 346.9f, 2.7f),   // 2 AKM
        new Vector3(356.8f, 348.3f, 98.0f),  // 3 MK14
        new Vector3(345.8f, 341.7f, 94.2f),  // 4 AA-12
        new Vector3(0.0f, 358.9f, 177.5f),   // 5 AWM
        new Vector3(0f, 0f, 0f),             // 6 M249
    };

    [HideInInspector] public int currentWeaponIndex = 2;
    [HideInInspector] public int currentAmmo;
    [HideInInspector] public int currentReserve;
    [HideInInspector] public bool isReloading = false;

    public WeaponData CurrentWeapon => weapons[currentWeaponIndex];

    private float reloadTimer;
    private const float RELOAD_TIME = 1.5f;
    private Transform weaponHolder;
    private GameObject currentWeaponPivot;

    public delegate void AmmoChangedHandler();
    public event AmmoChangedHandler OnAmmoChanged;

    void Start()
    {
        var cam = GetComponentInChildren<Camera>();
        if (cam != null)
            weaponHolder = cam.transform.Find("WeaponHolder");

        var w = weapons[currentWeaponIndex];
        currentAmmo = w.magazineSize;
        currentReserve = w.reserveAmmo;
        EquipWeaponModel(currentWeaponIndex);
        OnAmmoChanged?.Invoke();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameOver) return;
        if (isReloading) { HandleReload(); return; }

        HandleWeaponSwitch();
        HandleReloadInput();
    }

    void HandleWeaponSwitch()
    {
        for (int i = 0; i < 7; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                if (i < weapons.Length && weapons[i] != null && i != currentWeaponIndex)
                {
                    currentWeaponIndex = i;
                    var w = weapons[i];
                    currentAmmo = w.magazineSize;
                    currentReserve = w.reserveAmmo;
                    isReloading = false;
                    EquipWeaponModel(i);
                    OnAmmoChanged?.Invoke();
                }
            }
        }
    }

    void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
            StartReload();
    }

    void HandleReload()
    {
        reloadTimer -= Time.deltaTime;
        if (reloadTimer <= 0)
        {
            var w = CurrentWeapon;
            int needed = w.magazineSize - currentAmmo;
            int fill = Mathf.Min(needed, currentReserve);
            currentAmmo += fill;
            currentReserve -= fill;
            isReloading = false;
            OnAmmoChanged?.Invoke();
        }
    }

    public void StartReload()
    {
        if (isReloading) return;
        var w = CurrentWeapon;
        if (currentAmmo == w.magazineSize || currentReserve <= 0) return;
        isReloading = true;
        reloadTimer = RELOAD_TIME;
        OnAmmoChanged?.Invoke();
    }

    public bool ConsumeAmmo()
    {
        if (currentAmmo <= 0)
        {
            StartReload();
            return false;
        }
        currentAmmo--;
        OnAmmoChanged?.Invoke();
        return true;
    }

    public void AddReserveAmmo(int amount)
    {
        currentReserve += amount;
        OnAmmoChanged?.Invoke();
    }

    public void ResetAll()
    {
        isReloading = false;
        currentWeaponIndex = 2;
        var w = CurrentWeapon;
        currentAmmo = w.magazineSize;
        currentReserve = w.reserveAmmo;
        EquipWeaponModel(currentWeaponIndex);
        OnAmmoChanged?.Invoke();
    }

    void EquipWeaponModel(int index)
    {
        if (currentWeaponPivot != null)
            Destroy(currentWeaponPivot);

        if (weaponHolder != null && index < weaponModels.Length && weaponModels[index] != null)
        {
            // Pivot: 旋转支点，位于 holder 的 viewModelLocalCenter
            currentWeaponPivot = new GameObject("WeaponPivot");
            Transform pivot = currentWeaponPivot.transform;
            pivot.SetParent(weaponHolder);
            pivot.localPosition = viewModelLocalCenter;
            pivot.localRotation = Quaternion.Euler(pivotEuler[index]);

            // Model: 挂载在 pivot 下
            GameObject model = Instantiate(weaponModels[index], pivot);
            NormalizeViewModel(model.transform, index);
        }
    }

    void NormalizeViewModel(Transform model, int index)
    {
        Vector3 euler = (index >= 0 && index < weaponModelEuler.Length) ? weaponModelEuler[index] : Vector3.zero;
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.Euler(euler);
        model.localScale = Vector3.one;

        var rends = model.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (longest <= 0.0001f) return;

        float targetSize = (index >= 0 && index < weaponSizes.Length) ? weaponSizes[index] : 0.35f;
        float scale = targetSize / longest;
        model.localScale = Vector3.one * scale;

        // 将几何中心移到 pivot 原点，确保 pivot 旋转时绕枪身中心转
        rends = model.GetComponentsInChildren<Renderer>();
        b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        Vector3 worldCenter = b.center;
        Transform pivot = model.parent;
        Vector3 centerInPivot = pivot.InverseTransformPoint(worldCenter);
        model.localPosition = -centerInPivot;
    }

}
