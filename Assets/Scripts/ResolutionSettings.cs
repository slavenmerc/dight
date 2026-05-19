using UnityEngine;
using TMPro;

public class ResolutionSettings : MonoBehaviour
{
    public TMP_Text resolutionText;

    private Resolution[] resolutions;
    private int currentResolutionIndex = 0;

    void Start()
    {
        resolutions = new Resolution[]
        {
            new Resolution { width = 1280, height = 720 },
            new Resolution { width = 1600, height = 900 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 2560, height = 1440 }
        };

        currentResolutionIndex = 2;

        UpdateResolutionText();
    }

    public void NextResolution()
    {
        currentResolutionIndex++;

        if (currentResolutionIndex >= resolutions.Length)
        {
            currentResolutionIndex = 0;
        }

        ApplyResolution();
    }

    public void PreviousResolution()
    {
        currentResolutionIndex--;

        if (currentResolutionIndex < 0)
        {
            currentResolutionIndex = resolutions.Length - 1;
        }

        ApplyResolution();
    }

    private void ApplyResolution()
    {
        Resolution resolution = resolutions[currentResolutionIndex];

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            Screen.fullScreen
        );

        UpdateResolutionText();
    }

    private void UpdateResolutionText()
    {
        Resolution resolution = resolutions[currentResolutionIndex];

        resolutionText.text = resolution.width + "x" + resolution.height;
    }
}