using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds persistent ColorGrid buttons to the existing JeepneyPainter. The
/// palette list is intentionally serialized so designers can edit colors in
/// the Inspector without touching paint code.
/// </summary>
public class CustomizationColorPalette : MonoBehaviour
{
    [SerializeField] private JeepneyPainter painter;
    [SerializeField] private CustomizationUIController uiController;
    [SerializeField] private List<Color> paletteColors = new List<Color>
    {
        Color.black,
        Color.white,
        Color.red,
        new Color(1f, 0.45f, 0f, 1f),
        Color.yellow,
        new Color(0.1f, 0.7f, 0.2f, 1f),
        Color.cyan,
        new Color(0.1f, 0.35f, 1f, 1f),
        new Color(0.55f, 0.2f, 0.8f, 1f),
        new Color(1f, 0.35f, 0.65f, 1f),
        Color.gray,
        new Color(0.35f, 0.18f, 0.07f, 1f)
    };
    [SerializeField] private List<Button> swatchButtons = new List<Button>();

    private void Awake()
    {
        UpdateSwatchVisuals();
    }

    private void Start()
    {
        if (painter == null || !painter.gameObject.scene.IsValid())
            painter = uiController != null ? uiController.ActivePainter : null;

        if (painter == null || !painter.gameObject.scene.IsValid())
        {
            Debug.LogError("Customization color palette requires the active scene JeepneyPainter reference.", this);
            return;
        }

        ConfigureSwatches();
    }

    private void OnValidate()
    {
        UpdateSwatchVisuals();
    }

    private void ConfigureSwatches()
    {
        for (int index = 0; index < swatchButtons.Count; index++)
        {
            Button button = swatchButtons[index];
            if (button == null)
                continue;

            int colorIndex = index;
            button.onClick.AddListener(() => SelectColor(colorIndex));
        }

        UpdateSwatchVisuals();
    }

    private void SelectColor(int colorIndex)
    {
        if (painter == null || colorIndex < 0 || colorIndex >= paletteColors.Count)
            return;

        painter.SetBrushColor(paletteColors[colorIndex]);
    }

    private void UpdateSwatchVisuals()
    {
        for (int index = 0; index < swatchButtons.Count; index++)
        {
            Button button = swatchButtons[index];
            if (button == null)
                continue;

            bool hasColor = index < paletteColors.Count;
            if (button.gameObject.activeSelf != hasColor)
                button.gameObject.SetActive(hasColor);

            if (!hasColor)
                continue;

            Image image = button.targetGraphic as Image;
            if (image != null)
                image.color = paletteColors[index];
        }
    }
}
