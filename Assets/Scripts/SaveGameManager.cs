using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveGameManager
{
    private const string SaveFileName = "last_game.json";

    private static SaveData pendingLoadData;

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave => File.Exists(SavePath);

    public static void SaveCurrentGame()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Save failed: player with tag Player was not found.");
            return;
        }

        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerPosition = player.transform.position,
            playerRotation = player.transform.eulerAngles,
            playerBalance = CapturePlayerBalance(player),
            playerHealth = CapturePlayerMovementHealth(player),
            spiders = CaptureSpiders(),
            savedAt = DateTime.UtcNow.ToString("O")
        };

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Game saved to " + SavePath);
    }

    public static void LoadLastGame()
    {
        if (!TryReadSave(out SaveData data))
        {
            Debug.LogWarning("Load failed: no valid save file was found at " + SavePath);
            return;
        }

        pendingLoadData = data;
        SceneManager.sceneLoaded -= ApplyPendingLoad;
        SceneManager.sceneLoaded += ApplyPendingLoad;
        SceneManager.LoadScene(data.sceneName);
    }

    private static bool TryReadSave(out SaveData data)
    {
        data = null;

        if (!File.Exists(SavePath))
        {
            return false;
        }

        data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        return data != null && !string.IsNullOrWhiteSpace(data.sceneName);
    }

    private static void ApplyPendingLoad(Scene scene, LoadSceneMode mode)
    {
        if (pendingLoadData == null || scene.name != pendingLoadData.sceneName)
        {
            return;
        }

        SceneManager.sceneLoaded -= ApplyPendingLoad;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Load failed: player with tag Player was not found in scene " + scene.name);
            pendingLoadData = null;
            return;
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.position = pendingLoadData.playerPosition;
        player.transform.eulerAngles = pendingLoadData.playerRotation;

        if (characterController != null)
        {
            characterController.enabled = controllerWasEnabled;
        }

        RestorePlayerBalance(player, pendingLoadData.playerBalance);
        RestorePlayerMovementHealth(player, pendingLoadData.playerHealth);
        RestoreSpiders(pendingLoadData.spiders);
        pendingLoadData = null;
    }

    private static PlayerBalanceSaveData CapturePlayerBalance(GameObject player)
    {
        PlayerBalanceHealth balanceHealth = player.GetComponent<PlayerBalanceHealth>();
        if (balanceHealth == null)
        {
            return new PlayerBalanceSaveData { hasBalance = false };
        }

        return new PlayerBalanceSaveData
        {
            hasBalance = true,
            currentBalance = balanceHealth.CurrentBalance,
            maxThreshold = balanceHealth.MaxThreshold
        };
    }

    private static PlayerHealthSaveData CapturePlayerMovementHealth(GameObject player)
    {
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            return new PlayerHealthSaveData { hasHealth = false };
        }

        return new PlayerHealthSaveData
        {
            hasHealth = true,
            health = playerMovement.health,
            maxHealth = playerMovement.maxHealth
        };
    }

    private static void RestorePlayerBalance(GameObject player, PlayerBalanceSaveData balanceData)
    {
        if (balanceData == null || !balanceData.hasBalance)
        {
            return;
        }

        PlayerBalanceHealth balanceHealth = player.GetComponent<PlayerBalanceHealth>();
        if (balanceHealth != null)
        {
            balanceHealth.SetBalance(balanceData.currentBalance);
        }
    }

    private static void RestorePlayerMovementHealth(GameObject player, PlayerHealthSaveData healthData)
    {
        if (healthData == null || !healthData.hasHealth)
        {
            return;
        }

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.maxHealth = healthData.maxHealth;
            playerMovement.health = healthData.health;
        }
    }

    private static SpiderSaveData[] CaptureSpiders()
    {
        SpiderEnemy[] spiders = UnityEngine.Object.FindObjectsByType<SpiderEnemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        List<SpiderSaveData> savedSpiders = new List<SpiderSaveData>();
        foreach (SpiderEnemy spider in spiders)
        {
            if (spider == null || spider.currentState == SpiderEnemy.SpiderState.Dead)
            {
                continue;
            }

            savedSpiders.Add(new SpiderSaveData
            {
                templateName = GetTemplateName(spider.gameObject.name),
                position = spider.transform.position,
                rotation = spider.transform.eulerAngles,
                scale = spider.transform.localScale,
                state = (int)spider.currentState,
                speed = spider.speed,
                damage = spider.damage,
                enemyType = (int)spider.enemyType,
                wakeUpDistance = spider.wakeUpDistance,
                stopDistance = spider.stopDistance,
                attackDistance = spider.attackDistance,
                jumpForce = spider.jumpForce,
                jumpDuration = spider.jumpDuration
            });
        }

        return savedSpiders.ToArray();
    }

    private static void RestoreSpiders(SpiderSaveData[] savedSpiders)
    {
        SpiderEnemy[] sceneSpiders = UnityEngine.Object.FindObjectsByType<SpiderEnemy>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Dictionary<string, SpiderEnemy> templates = new Dictionary<string, SpiderEnemy>();
        SpiderEnemy fallbackTemplate = null;

        foreach (SpiderEnemy spider in sceneSpiders)
        {
            if (spider == null)
            {
                continue;
            }

            fallbackTemplate ??= spider;
            string key = GetTemplateName(spider.gameObject.name);
            if (!templates.ContainsKey(key))
            {
                templates.Add(key, spider);
            }
        }

        if (savedSpiders != null && fallbackTemplate != null)
        {
            foreach (SpiderSaveData savedSpider in savedSpiders)
            {
                SpiderEnemy template = templates.TryGetValue(savedSpider.templateName, out SpiderEnemy exactTemplate)
                    ? exactTemplate
                    : fallbackTemplate;

                GameObject spiderObject = UnityEngine.Object.Instantiate(template.gameObject);
                spiderObject.name = savedSpider.templateName;
                spiderObject.transform.position = savedSpider.position;
                spiderObject.transform.eulerAngles = savedSpider.rotation;
                spiderObject.transform.localScale = savedSpider.scale;

                SpiderEnemy spider = spiderObject.GetComponent<SpiderEnemy>();
                if (spider != null)
                {
                    spider.ApplyLoadedState(savedSpider);
                }
            }
        }

        foreach (SpiderEnemy spider in sceneSpiders)
        {
            if (spider != null)
            {
                UnityEngine.Object.Destroy(spider.gameObject);
            }
        }
    }

    private static string GetTemplateName(string objectName)
    {
        string templateName = objectName.Replace("(Clone)", string.Empty).Trim();
        int suffixStart = templateName.LastIndexOf(" (", StringComparison.Ordinal);
        if (suffixStart >= 0 && templateName.EndsWith(")", StringComparison.Ordinal))
        {
            string suffix = templateName.Substring(suffixStart + 2, templateName.Length - suffixStart - 3);
            if (int.TryParse(suffix, out _))
            {
                templateName = templateName.Substring(0, suffixStart);
            }
        }

        return templateName;
    }

    [Serializable]
    private class SaveData
    {
        public string sceneName;
        public Vector3 playerPosition;
        public Vector3 playerRotation;
        public PlayerBalanceSaveData playerBalance;
        public PlayerHealthSaveData playerHealth;
        public SpiderSaveData[] spiders;
        public string savedAt;
    }
}

[Serializable]
public class PlayerBalanceSaveData
{
    public bool hasBalance;
    public float currentBalance;
    public float maxThreshold;
}

[Serializable]
public class PlayerHealthSaveData
{
    public bool hasHealth;
    public float health;
    public float maxHealth;
}

[Serializable]
public class SpiderSaveData
{
    public string templateName;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public int state;
    public float speed;
    public int damage;
    public int enemyType;
    public float wakeUpDistance;
    public float stopDistance;
    public float attackDistance;
    public float jumpForce;
    public float jumpDuration;
}
