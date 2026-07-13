public interface IGameSaveModule
{
    void CaptureGameSaveData(GameSaveData saveData);

    void RestoreGameSaveData(GameSaveData saveData);
}
