using UnityEngine;

public class PlayerConfig : MonoBehaviour
{
    [Header("Player Info")]
    public string playerName = "Player 1";

    [Header("Stats")]
    public int maxHP = 100;
    public float moveSpeed = 5f;
    public int damage = 10;
    public int bulletCount = 1;
}