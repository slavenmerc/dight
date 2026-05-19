using UnityEngine;
using UnityEngine.SceneManagement;

public class WinTrigger : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private GameObject winScreen; // Сюда перетащим WinScreen

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что флага коснулся именно игрок
        if (other.CompareTag("Player"))
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        // Показываем экран победы
        if (winScreen != null)
        {
            winScreen.SetActive(true);
        }

        // Ставим игру на паузу (чтобы мобы перестали бегать и бить)
        Time.timeScale = 0f;

        // Включаем курсор мыши, чтобы можно было нажать кнопки
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Этот метод привяжем к кнопке "Заново"
    public void RestartGame()
    {
        Time.timeScale = 1f; // Сбрасываем паузу перед перезапуском!
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Этот метод привяжем к кнопке "В меню"
    public void GoToMenu()
    {
        Time.timeScale = 1f; // Сбрасываем паузу!
        
        // Замени "MainMenu" на точное название твоей сцены с меню (в кавычках)
        // Если сцены меню еще нет, пока можно оставить пустую строку или закомментировать
        SceneManager.LoadScene("menu"); 
    }
}