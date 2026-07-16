public static class PenguinLevelIds
{
    public const int TotalLevels = 3;

    public const string LevelOneScene = "Penguin Run Level 1";
    public const string LevelTwoScene = "Level2_Friction";
    public const string LevelThreeScene = "Level3_PE";

    public static bool TryGetLevelNumber(string sceneName, out int levelNumber)
    {
        switch (sceneName)
        {
            case LevelOneScene:
            case "Level 1":
                levelNumber = 1;
                return true;
            case LevelTwoScene:
            case "Penguin Run Level 2":
                levelNumber = 2;
                return true;
            case LevelThreeScene:
            case "Penguin Run Level 3":
                levelNumber = 3;
                return true;
            default:
                levelNumber = 0;
                return false;
        }
    }

    public static bool IsValid(int levelNumber)
    {
        return levelNumber >= 1 && levelNumber <= TotalLevels;
    }
}
