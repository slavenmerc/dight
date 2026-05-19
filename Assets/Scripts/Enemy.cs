using UnityEngine;

public enum EnemyType { Day, Night }

public abstract class Enemy : MonoBehaviour
{
    [Header("Base Enemy Settings")]
    public float speed = 5f;
    public int damage = 10;
    public EnemyType enemyType;

    // Ссылка на игрока (автоматически найдем по тегу "Player")
    protected Transform player;

    protected virtual void Awake()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("В сцене не найден объект с тегом 'Player'!");
        }
    }
}