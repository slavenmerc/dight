using UnityEngine;
using TMPro;

public class SensitivitySettings : MonoBehaviour
{
    public TMP_Text sensitivityText;

    private int sensitivity;

    private const int minSensitivity = 5;
    private const int maxSensitivity = 50;
    private const int step = 5;

    private void Start()
    {
        sensitivity = PlayerPrefs.GetInt("MouseSensitivity", 15);

        if (sensitivity < minSensitivity)
        {
            sensitivity = minSensitivity;
        }

        if (sensitivity > maxSensitivity)
        {
            sensitivity = maxSensitivity;
        }

        UpdateText();
        SaveSensitivity();
    }

    public void NextSensitivity()
    {
        sensitivity += step;

        if (sensitivity > maxSensitivity)
        {
            sensitivity = minSensitivity;
        }

        SaveSensitivity();
        UpdateText();
    }

    public void PreviousSensitivity()
    {
        sensitivity -= step;

        if (sensitivity < minSensitivity)
        {
            sensitivity = maxSensitivity;
        }

        SaveSensitivity();
        UpdateText();
    }

    private void SaveSensitivity()
    {
        PlayerPrefs.SetInt("MouseSensitivity", sensitivity);
        PlayerPrefs.Save();
    }

    private void UpdateText()
    {
        sensitivityText.text = sensitivity.ToString();
    }
}