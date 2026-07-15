using TMPro;
using UnityEngine;

public class CharacterStatRow : MonoBehaviour
{
    [SerializeField]
    private TMP_Text statNameText;

    [SerializeField]
    private TMP_Text statValueText;

    public void SetData(
        string statName,
        string statValue
    )
    {
        if (statNameText != null)
        {
            statNameText.text =
                statName;
        }

        if (statValueText != null)
        {
            statValueText.text =
                statValue;
        }
    }
}