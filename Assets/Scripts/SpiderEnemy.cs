using UnityEngine;
using System.Collections;

public class SpiderEnemy : Enemy
{
    // Перечисление стадий паука
    public enum SpiderState { Cocoon, Chasing, Attacking, Dead }
    
    [Header("Spider States")]
    public SpiderState currentState = SpiderState.Cocoon;

    [Header("Visuals (Drop Prefabs Here)")]
    public GameObject cocoonVisual; // Сюда перетащи объект кокона
    public GameObject spiderVisual; // Сюда перетащи объект паука

    [Header("Distances")]
    public float wakeUpDistance = 5f;   // Дистанция, чтобы вылезти из кокона
    public float stopDistance = 15f;    // Дистанция, дальше которой паук засыпает
    public float attackDistance = 2f;    // Дистанция для прыжка-атаки

    [Header("Jump Attack Settings")]
    public float jumpForce = 7f;
    public float jumpDuration = 0.5f;

    private Animator animator;
    private bool isJumping = false;

    protected override void Awake()
    {
        base.Awake();
        
        // Берем аниматор с визуальной модельки паука
        if (spiderVisual != null)
            animator = spiderVisual.GetComponent<Animator>();

        // Инициализируем визуализацию
        UpdateVisuals();
    }

    void Update()
    {
        if (player == null || currentState == SpiderState.Dead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case SpiderState.Cocoon:
                // Если игрок подошел близко — вылезаем
                if (distanceToPlayer <= wakeUpDistance)
                {
                    currentState = SpiderState.Chasing;
                    UpdateVisuals();
                }
                break;

            case SpiderState.Chasing:
                // Если игрок убежал слишком далеко — замираем
                if (distanceToPlayer > stopDistance)
                {
                    SetMovingAnimation(false);
                    return; // Просто стоим на месте, код ниже не выполняется
                }

                // Если игрок на расстоянии атаки — прыгаем
                if (distanceToPlayer <= attackDistance && !isJumping)
                {
                    StartCoroutine(JumpAttackRoutine());
                    return;
                }

                // Обычное преследование
                SetMovingAnimation(true);
                MoveTowardsPlayer();
                break;
        }
    }

    void MoveTowardsPlayer()
    {
        // 1. Рассчитываем направление, но прижимаем его к земле (Y = 0)
        Vector3 targetPos = player.position;
        targetPos.y = transform.position.y; // Паук идет к игроку, но на своей высоте
        // Поворачиваемся к игроку (только по оси Y, чтобы паука не наклоняло вверх-вниз)
        Vector3 targetDirection = player.position - transform.position;
        targetDirection.y = 0; 
        if (targetDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(targetDirection);
        }

        // Движение вперед
        transform.position = Vector3.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
    }

    // Корутина для прыжка
    IEnumerator JumpAttackRoutine()
    {
        currentState = SpiderState.Attacking;
        isJumping = true;
        SetMovingAnimation(false); // Включаем анимацию прыжка/атаки, если есть

        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        float timer = 0;

        // Простая симуляция прыжка по дуге через Лерп
        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / jumpDuration;
            
            // Движение вперед + парабола вверх для эффекта прыжка
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpForce * 0.3f; 
            
            transform.position = currentPos;
            yield return null;
        }

        // Проверяем, попали ли по игроку в конце прыжка
        if (Vector3.Distance(transform.position, player.position) <= attackDistance)
        {
            ApplyEffectToPlayer();
        }

        // Исчезновение
        currentState = SpiderState.Dead;
        Destroy(gameObject);
    }

    void ApplyEffectToPlayer()
    {
        // Здесь должна быть твоя логика ХП. Для примера выведем в консоль:
        if (enemyType == EnemyType.Day)
        {
            Debug.Log($"Дневной паук попал! ХП игрока увеличилось на {damage}");
            // player.GetComponent<PlayerHealth>().Heal(damage);
        }
        else if (enemyType == EnemyType.Night)
        {
            Debug.Log($"Ночной паук попал! ХП игрока уменьшилось на {damage}");
            // player.GetComponent<PlayerHealth>().TakeDamage(damage);
        }
    }

    // Метод переключения моделек
    void UpdateVisuals()
    {
        if (cocoonVisual != null) 
            cocoonVisual.SetActive(currentState == SpiderState.Cocoon);
        
        if (spiderVisual != null) 
            spiderVisual.SetActive(currentState != SpiderState.Cocoon && currentState != SpiderState.Dead);
    }

    public void ApplyLoadedState(SpiderSaveData saveData)
    {
        StopAllCoroutines();
        isJumping = false;

        speed = saveData.speed;
        damage = saveData.damage;
        enemyType = (EnemyType)saveData.enemyType;
        wakeUpDistance = saveData.wakeUpDistance;
        stopDistance = saveData.stopDistance;
        attackDistance = saveData.attackDistance;
        jumpForce = saveData.jumpForce;
        jumpDuration = saveData.jumpDuration;

        currentState = (SpiderState)saveData.state;
        if (currentState == SpiderState.Attacking)
        {
            currentState = SpiderState.Chasing;
        }

        if (spiderVisual != null)
        {
            animator = spiderVisual.GetComponent<Animator>();
        }

        UpdateVisuals();
        SetMovingAnimation(false);
    }

    // Метод управления анимацией
    void SetMovingAnimation(bool isMoving)
    {
        if (animator != null)
        {
            // В твоем Аниматоре должна быть bool переменная с именем "isMoving"
            animator.SetBool("isMoving", isMoving);
        }
    }

    // Отрисовка гизмо в Unity для удобной настройки дистанций
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wakeUpDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
