using System;
using System.Collections;
using UnityEngine;

// Shared travelling shot used by the player and ranged enemies: same visual, same timing.
public static class Projectile
{
    public const float Speed = 8f;
    public const float MinDuration = 0.15f;
    static Sprite sharedSprite;

    public static void Fire(MonoBehaviour host, Vector3 from, Vector3 to, float worldRadius, Action<Vector3> onArrive)
    {
        host.StartCoroutine(Travel(host, from, to, worldRadius, onArrive));
    }

    // Yield this directly when a turn must wait for either arrival OR cancellation.
    public static IEnumerator Travel(MonoBehaviour host, Vector3 from, Vector3 to, float worldRadius, Action<Vector3> onArrive)
    {
        if (host == null || !host.isActiveAndEnabled) yield break;
        var go = new GameObject("Shot");
        go.transform.position = from;
        var sr = go.AddComponent<SpriteRenderer>();
        if (sharedSprite == null) sharedSprite = ProceduralSprites.CreateCircle(worldRadius);
        sr.sprite = sharedSprite;
        sr.color = Color.black;
        sr.sortingOrder = 10;
        try
        {
            float duration = Mathf.Max(MinDuration, Vector3.Distance(from, to) / Speed);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (go == null || host == null || !host.isActiveAndEnabled) yield break;
                go.transform.position = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (go == null || host == null || !host.isActiveAndEnabled) yield break;
            go.transform.position = to;
            onArrive?.Invoke(to);
        }
        finally
        {
            if (go != null) UnityEngine.Object.Destroy(go);
        }
    }
}
