using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PenguinLevelProgressService : MonoBehaviour
{
    private const string SaveKey = "KFI.PenguinRun.LevelProgress.v1";
    private const int SaveVersion = 1;

    public static PenguinLevelProgressService Instance { get; private set; }
    public static event Action<int> LevelCompleted;
    public static event Action GameCompleted;

    private PenguinLevelProgressSaveData saveData;
    private int lastBegunSceneHandle = int.MinValue;
    private bool initialized;
    private bool gameCompletionPosted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        LevelCompleted = null;
        GameCompleted = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    private void OnDestroy()
    {
        if (initialized)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    public static void BeginLevel(int levelNumber)
    {
        if (!ValidateLevel(levelNumber))
            return;

        EnsureInstance().BeginInternal(levelNumber);
    }

    public static bool CompleteLevel(int levelNumber)
    {
        if (!ValidateLevel(levelNumber))
            return false;

        return EnsureInstance().CompleteInternal(levelNumber);
    }

    public static bool CompleteScene(string sceneName)
    {
        if (!PenguinLevelIds.TryGetLevelNumber(sceneName, out int levelNumber))
        {
            Debug.LogWarning($"[PenguinProgress] Scene '{sceneName}' does not have a stable level number.");
            return false;
        }

        return CompleteLevel(levelNumber);
    }

    public static bool CompleteGame()
    {
        return EnsureInstance().CompleteGameInternal();
    }

    public static bool IsLevelComplete(int levelNumber)
    {
        if (!ValidateLevel(levelNumber))
            return false;

        PenguinLevelProgressRecord record = EnsureInstance().FindRecord(levelNumber);
        return record != null && record.completed;
    }

    public static bool IsLevelUnlocked(int levelNumber)
    {
        if (!ValidateLevel(levelNumber))
            return false;

        return levelNumber == 1 || IsLevelComplete(levelNumber - 1);
    }

    public static int GetAttempts(int levelNumber)
    {
        if (!ValidateLevel(levelNumber))
            return 0;

        PenguinLevelProgressRecord record = EnsureInstance().FindRecord(levelNumber);
        return record != null ? record.attempts : 0;
    }

    public static int GetNextIncompleteLevel()
    {
        for (int levelNumber = 1; levelNumber <= PenguinLevelIds.TotalLevels; levelNumber++)
        {
            if (!IsLevelComplete(levelNumber))
                return levelNumber;
        }

        return 0;
    }

    public static int[] GetCompletedLevels()
    {
        return EnsureInstance().BuildCompletedLevels();
    }

    public static void ResetAllProgress()
    {
        PenguinLevelProgressService service = EnsureInstance();
        service.saveData = new PenguinLevelProgressSaveData();
        service.lastBegunSceneHandle = int.MinValue;
        service.gameCompletionPosted = false;
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    private static PenguinLevelProgressService EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        PenguinLevelProgressService existing = FindAnyObjectByType<PenguinLevelProgressService>();
        if (existing != null)
        {
            existing.Initialize();
            return existing;
        }

        GameObject serviceObject = new GameObject(nameof(PenguinLevelProgressService));
        PenguinLevelProgressService created = serviceObject.AddComponent<PenguinLevelProgressService>();
        created.Initialize();
        return created;
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        Instance = this;
        if (Application.isPlaying)
            DontDestroyOnLoad(gameObject);

        Load();
        SceneManager.sceneLoaded += OnSceneLoaded;
        BeginScene(SceneManager.GetActiveScene());
    }

    private static bool ValidateLevel(int levelNumber)
    {
        bool isValid = PenguinLevelIds.IsValid(levelNumber);
        if (!isValid)
            Debug.LogWarning($"[PenguinProgress] Invalid level number {levelNumber}.");

        return isValid;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BeginScene(scene);
    }

    private void BeginScene(Scene scene)
    {
        if (!scene.IsValid() || scene.handle == lastBegunSceneHandle)
            return;

        if (!PenguinLevelIds.TryGetLevelNumber(scene.name, out int levelNumber))
            return;

        lastBegunSceneHandle = scene.handle;
        BeginInternal(levelNumber);
    }

    private void BeginInternal(int levelNumber)
    {
        PenguinLevelProgressRecord record = FindOrCreateRecord(levelNumber);
        record.attempts += 1;
        record.lastStartedAt = DateTime.UtcNow.ToString("O");
        Save();
        PostProgress();
    }

    private bool CompleteInternal(int levelNumber)
    {
        PenguinLevelProgressRecord record = FindOrCreateRecord(levelNumber);
        if (record.completed)
        {
            PostProgress();
            return false;
        }

        if (record.attempts == 0)
            record.attempts = 1;

        record.completed = true;
        record.completedAt = DateTime.UtcNow.ToString("O");
        Save();

        LevelCompleted?.Invoke(levelNumber);
        PostProgress(record);
        return true;
    }

    private bool CompleteGameInternal()
    {
        if (gameCompletionPosted)
            return false;

        gameCompletionPosted = true;
        GameCompleted?.Invoke();

        var completionPayload = new PenguinGameCompletionPayload
        {
            saveVersion = SaveVersion,
            completedLevels = BuildCompletedLevels(),
            gameCompleted = true
        };

        PenguinProgressWebBridge.Post(JsonUtility.ToJson(completionPayload));
        return true;
    }

    private PenguinLevelProgressRecord FindOrCreateRecord(int levelNumber)
    {
        PenguinLevelProgressRecord record = FindRecord(levelNumber);
        if (record != null)
            return record;

        record = new PenguinLevelProgressRecord { levelNumber = levelNumber };
        saveData.levels.Add(record);
        return record;
    }

    private PenguinLevelProgressRecord FindRecord(int levelNumber)
    {
        EnsureSaveData();

        for (int i = 0; i < saveData.levels.Count; i++)
        {
            PenguinLevelProgressRecord record = saveData.levels[i];
            if (record != null && record.levelNumber == levelNumber)
                return record;
        }

        return null;
    }

    private int[] BuildCompletedLevels()
    {
        EnsureSaveData();
        var completedLevels = new List<int>();

        for (int i = 0; i < saveData.levels.Count; i++)
        {
            PenguinLevelProgressRecord record = saveData.levels[i];
            if (record != null && record.completed)
                completedLevels.Add(record.levelNumber);
        }

        completedLevels.Sort();
        return completedLevels.ToArray();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            saveData = new PenguinLevelProgressSaveData();
            return;
        }

        try
        {
            saveData = JsonUtility.FromJson<PenguinLevelProgressSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[PenguinProgress] Could not read saved progress; starting clean. " + exception.Message);
            saveData = new PenguinLevelProgressSaveData();
        }

        EnsureSaveData();
    }

    private void Save()
    {
        EnsureSaveData();
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }

    private void EnsureSaveData()
    {
        if (saveData == null)
            saveData = new PenguinLevelProgressSaveData();

        if (saveData.levels == null)
            saveData.levels = new List<PenguinLevelProgressRecord>();
    }

    private void PostProgress(PenguinLevelProgressRecord completedRecord = null)
    {
        int[] completedLevels = BuildCompletedLevels();

        if (completedRecord == null)
        {
            var snapshotPayload = new PenguinProgressSnapshotPayload
            {
                saveVersion = SaveVersion,
                completedLevels = completedLevels
            };

            PenguinProgressWebBridge.Post(JsonUtility.ToJson(snapshotPayload));
            return;
        }

        var completionPayload = new PenguinProgressCompletionPayload
        {
            saveVersion = SaveVersion,
            completedLevels = completedLevels,
            levelCompleted = completedRecord.levelNumber
        };

        PenguinProgressWebBridge.Post(JsonUtility.ToJson(completionPayload));
    }
}

[Serializable]
public sealed class PenguinLevelProgressSaveData
{
    public int saveVersion = 1;
    public List<PenguinLevelProgressRecord> levels = new List<PenguinLevelProgressRecord>();
}

[Serializable]
public sealed class PenguinLevelProgressRecord
{
    public int levelNumber;
    public bool completed;
    public int attempts;
    public string lastStartedAt;
    public string completedAt;
}

[Serializable]
public sealed class PenguinProgressSnapshotPayload
{
    public int saveVersion;
    public int[] completedLevels;
}

[Serializable]
public sealed class PenguinProgressCompletionPayload
{
    public int saveVersion;
    public int[] completedLevels;
    public int levelCompleted;
}

[Serializable]
public sealed class PenguinGameCompletionPayload
{
    public int saveVersion;
    public int[] completedLevels;
    public bool gameCompleted;
}
