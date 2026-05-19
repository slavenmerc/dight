using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ThreeBackgroundChanger : MonoBehaviour
{
    public Image background1;
    public Image background2;
    public Image background3;

    public float showTime = 3f;
    public float fadeTime = 1f;

    private Image[] backgrounds;
    private int currentIndex = 0;

    void Start()
    {
        backgrounds = new Image[] { background1, background2, background3 };

        SetAlpha(background1, 1f);
        SetAlpha(background2, 0f);
        SetAlpha(background3, 0f);

        StartCoroutine(ChangeBackgrounds());
    }

    IEnumerator ChangeBackgrounds()
    {
        while (true)
        {
            yield return new WaitForSeconds(showTime);

            int nextIndex = currentIndex + 1;

            if (nextIndex >= backgrounds.Length)
            {
                nextIndex = 0;
            }

            yield return StartCoroutine(Fade(
                backgrounds[currentIndex],
                backgrounds[nextIndex]
            ));

            currentIndex = nextIndex;
        }
    }

    IEnumerator Fade(Image oldBackground, Image newBackground)
    {
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeTime;

            SetAlpha(oldBackground, 1f - t);
            SetAlpha(newBackground, t);

            yield return null;
        }

        SetAlpha(oldBackground, 0f);
        SetAlpha(newBackground, 1f);
    }

    void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}