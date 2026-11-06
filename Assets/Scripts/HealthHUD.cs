using UnityEngine;
using UnityEngine.UI;

// Builds its own canvas/layout at runtime, same "self-contained" pattern as the other procedural visuals.
public class HealthHUD : MonoBehaviour
{
    public float circleSize = 36f;
    public float spacing = 10f;

    Image[] slots;
    PlayerHealth playerHealth;
    Sprite circleSprite;

    void Awake()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        circleSprite = ProceduralSprites.CreateCircle(0.5f);

        var canvasGo = new GameObject("HealthCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();

        var panelGo = new GameObject("HealthPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var rect = panelGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        var layout = panelGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        slots = new Image[playerHealth.maxHealth];
        for (int i = 0; i < slots.Length; i++)
        {
            var slotGo = new GameObject($"Life_{i}");
            slotGo.transform.SetParent(panelGo.transform, false);
            var img = slotGo.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color = Color.red;
            var le = slotGo.AddComponent<LayoutElement>();
            le.preferredWidth = circleSize;
            le.preferredHeight = circleSize;
            slots[i] = img;
        }

        playerHealth.OnChanged += Refresh;
        Refresh(playerHealth.Current, playerHealth.maxHealth);
    }

    void Refresh(int current, int max)
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].color = i < current ? Color.red : Color.white;
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnChanged -= Refresh;
        if (circleSprite != null)
        {
            Destroy(circleSprite.texture);
            Destroy(circleSprite);
        }
    }
}
