using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = 0.9f;
    public float speed = 15f;

    public Graphic targetGraphic;
    public Color pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private Vector3 originalScale;
    private Color originalColor;

    private Vector3 targetScale;
    private Color targetColor;

    private void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetGraphic != null)
        {
            originalColor = targetGraphic.color;
            targetColor = originalColor;
        }
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);

        if (targetGraphic != null)
        {
            targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, Time.deltaTime * speed);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * pressedScale;

        if (targetGraphic != null)
            targetColor = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetButton();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetButton();
    }

    private void ResetButton()
    {
        targetScale = originalScale;

        if (targetGraphic != null)
            targetColor = originalColor;
    }
}