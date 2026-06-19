using UnityEngine;
using UnityEditor;

public class WeaponCreator : EditorWindow
{
    [MenuItem("FPS/Create All Weapons")]
    public static void CreateAllWeapons()
    {
        string path = "Assets/ScriptableObjects/Weapons/";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Weapons");
        }

        CreateWeapon(path, "手枪",       0.40f, 20,  12, 48,  false, 1, 0.044f, 55f);
        CreateWeapon(path, "冲锋枪",     0.07f, 15,  30, 120, false, 1, 0.052f, 55f);
        CreateWeapon(path, "突击步枪",   0.12f, 26,  30, 90,  false, 1, 0.061f, 45f);
        CreateWeapon(path, "战斗步枪",   0.25f, 52,  20, 60,  false, 1, 0.035f, 35f);
        CreateWeapon(path, "霰弹枪",     0.80f, 20,  6,  24,  true,  6, 0.175f, 65f);
        CreateWeapon(path, "狙击枪",     1.50f, 150, 5,  15,  false, 1, 0.052f, 20f);
        CreateWeapon(path, "轻机枪",     0.10f, 22,  100,200, false, 1, 0.087f, 50f);

        AssetDatabase.Refresh();
        Debug.Log("7 weapons created in " + path);
    }

    static void CreateWeapon(string path, string name, float fireRate, int baseDmg,
        int mag, int reserve, bool shotgun, int pellets, float spread, float adsFov)
    {
        var w = ScriptableObject.CreateInstance<WeaponData>();
        w.weaponName = name;
        w.fireRate = fireRate;
        w.baseDamage = baseDmg;
        w.magazineSize = mag;
        w.reserveAmmo = reserve;
        w.isShotgun = shotgun;
        w.pellets = pellets;
        w.spread = spread;
        w.adsFov = adsFov;

        string fileName = path + name + ".asset";
        AssetDatabase.CreateAsset(w, fileName);
    }
}
