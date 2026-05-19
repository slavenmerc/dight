using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 1. ОБЯЗАТЕЛЬНО ДОБАВЬ ЭТУ СТРОКУ в самый верх скрипта!

public class PlayerBalanceHealth : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private Slider balanceSlider;

    [Header("Настройки баланса")]
    [SerializeField] private float maxThreshold = 100f;
    
    private float currentBalance = 0f; 
    private bool isDead = false;

    void Start()
    {
        if (balanceSlider != null)
        {
            balanceSlider.minValue = -maxThreshold;
            balanceSlider.maxValue = maxThreshold;
            balanceSlider.value = currentBalance;
        }
    }

    public void TakeDamage(float amount, string damageType)
    {
        if (isDead) return;

        if (damageType == "Light")
        {
            currentBalance += amount;
        }
        else if (damageType == "Dark")
        {
            currentBalance -= amount;
        }

        currentBalance = Mathf.Clamp(currentBalance, -maxThreshold, maxThreshold);

        if (balanceSlider != null)
        {
            balanceSlider.value = currentBalance;
        }

        CheckDeath();
    }

    private void CheckDeath()
    {
        if (currentBalance >= maxThreshold)
        {
            Die("Свет поглотил тебя!");
        }
        else if (currentBalance <= -maxThreshold)
        {
            Die("Тьма поглотила тебя!");
        }
    }

    private void Die(string message)
    {
        isDead = true;
        Debug.LogWarning(message);

        // 2. ВЫЗЫВАЕМ ПЕРЕЗАПУСК СЦЕНЫ
        RestartCurrentScene();
    }

    private void RestartCurrentScene()
    {
        // Получаем индекс текущей активной сцены
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        // Загружаем её заново
        SceneManager.LoadScene(currentSceneIndex);
    }
}