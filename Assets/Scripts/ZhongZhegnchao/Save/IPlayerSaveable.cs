public interface IPlayerSaveable
{
    PlayerSaveData CapturePlayerSaveData();

    void RestorePlayerSaveData(PlayerSaveData saveData);
}