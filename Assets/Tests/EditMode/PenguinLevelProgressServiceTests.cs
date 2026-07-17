using NUnit.Framework;
using UnityEngine;

public sealed class PenguinLevelProgressServiceTests
{
    [SetUp]
    public void SetUp()
    {
        PenguinLevelProgressService.ResetAllProgress();
    }

    [TearDown]
    public void TearDown()
    {
        PenguinLevelProgressService.ResetAllProgress();

        if (PenguinLevelProgressService.Instance != null)
            Object.DestroyImmediate(PenguinLevelProgressService.Instance.gameObject);
    }

    [Test]
    public void SceneAliasesResolveToStableLevelNumbers()
    {
        Assert.That(PenguinLevelIds.TryGetLevelNumber(PenguinLevelIds.LevelOneScene, out int levelOne), Is.True);
        Assert.That(PenguinLevelIds.TryGetLevelNumber("Penguin Run Level 2", out int levelTwo), Is.True);
        Assert.That(PenguinLevelIds.TryGetLevelNumber(PenguinLevelIds.LevelThreeScene, out int levelThree), Is.True);
        Assert.That(new[] { levelOne, levelTwo, levelThree }, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void BeginLevelCountsEachAttempt()
    {
        PenguinLevelProgressService.BeginLevel(1);
        PenguinLevelProgressService.BeginLevel(1);

        Assert.That(PenguinLevelProgressService.GetAttempts(1), Is.EqualTo(2));
    }

    [Test]
    public void CompleteLevelStoresSnapshotAndSuppressesDuplicateCompletion()
    {
        PenguinLevelProgressService.BeginLevel(1);

        Assert.That(PenguinLevelProgressService.CompleteLevel(1), Is.True);
        Assert.That(PenguinLevelProgressService.CompleteLevel(1), Is.False);
        Assert.That(PenguinLevelProgressService.IsLevelComplete(1), Is.True);
        Assert.That(PenguinLevelProgressService.GetAttempts(1), Is.EqualTo(1));
        Assert.That(PenguinLevelProgressService.GetCompletedLevels(), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void CompletedLevelsReloadFromPlayerPrefs()
    {
        PenguinLevelProgressService.CompleteLevel(2);
        string savedJson = PlayerPrefs.GetString("KFI.PenguinRun.LevelProgress.v1", string.Empty);

        Assert.That(savedJson, Does.Contain("\"levelNumber\":2"));
        Object.DestroyImmediate(PenguinLevelProgressService.Instance.gameObject);
        Assert.That(PenguinLevelProgressService.Instance == null, Is.True);

        Assert.That(PenguinLevelProgressService.IsLevelComplete(2), Is.True);
        Assert.That(PenguinLevelProgressService.GetCompletedLevels(), Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void IsLevelUnlockedRequiresPreviousCompletion()
    {
        Assert.That(PenguinLevelProgressService.IsLevelUnlocked(1), Is.True);
        Assert.That(PenguinLevelProgressService.IsLevelUnlocked(2), Is.False);

        PenguinLevelProgressService.CompleteLevel(1);

        Assert.That(PenguinLevelProgressService.IsLevelUnlocked(2), Is.True);
        Assert.That(PenguinLevelProgressService.GetNextIncompleteLevel(), Is.EqualTo(2));
    }
}
