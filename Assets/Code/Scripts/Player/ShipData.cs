using UnityEngine;

/// <summary>Lưu thông tin và chỉ số tàu trong một ScriptableObject để cấu hình ở Editor.</summary>
[CreateAssetMenu(
    fileName = "ShipData",
    menuName = "Space Explorer/Ship Data"
)]
public class ShipData : ScriptableObject
{
    [Header("Ship Info")]
    public string shipName;
    public Sprite shipImage;

    [Header("Stats")]
    public int maxHP = 100;
    public float moveSpeed = 5f;
    public int damage = 10;
    public int bulletCount = 1;
}