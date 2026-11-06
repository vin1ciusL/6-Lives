using UnityEngine;

public static class ProceduralSprites
{
    public static Sprite CreateCircle(float worldRadius)
    {
        const int size = 64;
        const float radius = size / 2f - 2f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius
                    ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, radius / worldRadius);
    }
}
