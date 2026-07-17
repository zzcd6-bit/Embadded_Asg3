using UnityEngine;

[DisallowMultipleComponent]
public class SkillTreeStatAreaView : MonoBehaviour
{
    [SerializeField] private SkillTreeStatRowView[] rows;

    public void Show(
        SkillTreeSkillConfig skillConfig,
        int currentLevel,
        bool showNextLevel
    )
    {
        if (rows == null)
            return;

        if (skillConfig == null)
        {
            HideAll();
            return;
        }

        int rowIndex = 0;

        for (int i = 0; i < skillConfig.Stats.Count; i++)
        {
            SkillTreeStatConfig stat =
                skillConfig.Stats[i];

            if (stat == null || !stat.ShowInUI)
                continue;

            if (rowIndex >= rows.Length)
                break;

            SkillTreeStatRowView row =
                rows[rowIndex];

            if (row != null)
            {
                row.Show(
                    stat,
                    currentLevel,
                    showNextLevel
                );
            }

            rowIndex++;
        }

        for (int i = rowIndex; i < rows.Length; i++)
        {
            if (rows[i] != null)
                rows[i].Hide();
        }
    }

    public void HideAll()
    {
        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] != null)
                rows[i].Hide();
        }
    }
}