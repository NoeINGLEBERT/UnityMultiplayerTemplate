using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DynamicTextHeight : MonoBehaviour
{
    private TextMeshProUGUI tmpText;
    private RectTransform rectTransform;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        AdjustHeight();
    }

    void AdjustHeight()
    {
        // Ensure the text mesh is updated
        tmpText.ForceMeshUpdate();

        // Get the preferred height of the text
        float preferredHeight = tmpText.GetPreferredValues(rectTransform.rect.width, 0).y;

        // Update the RectTransform's height
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, preferredHeight);
    }
}
