using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public enum ElementType { Light, Dark }
    
    [Header("Настройки атаки")]
    public ElementType enemyElement;
    public float damageAmount = 15f;

    // Пример атаки через коллизию (если у моба или игрока есть Collider и Rigidbody)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerBalanceHealth playerHealth = collision.gameObject.GetComponent<PlayerBalanceHealth>();
            
            if (playerHealth != null)
            {
                // Переводим enum в строку для скрипта игрока
                string typeString = enemyElement == ElementType.Light ? "Light" : "Dark";
                playerHealth.TakeDamage(damageAmount, typeString);
            }
        }
    }
}