public interface IExperienceReceiver
{
    int CurrentExperience { get; }

    int ExperienceToNextLevel { get; }

    bool IsMaxLevel { get; }

    void AddExperience(int amount);
}