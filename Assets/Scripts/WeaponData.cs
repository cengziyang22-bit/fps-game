using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "武器";
    public float fireRate = 0.12f;
    public int baseDamage = 26;
    public int magazineSize = 30;
    public int reserveAmmo = 90;
    public bool isShotgun = false;
    public int pellets = 1;
    public float spread = 0.06f;
    [Range(10f, 70f)]
    public float adsFov = 45f;
}
