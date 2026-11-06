using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Health))]
public class EnemyUnit : MonoBehaviour
{
    public bool isRanged;
    public Color bodyColor = Color.red;
    public int moveSteps = 3;
    public float moveSpeed = 6f;
    [Min(1)] public int attackRange = 4;
    public bool hasPlayerVision = true;

    static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    HexGridManager grid;
    Vector2Int currentCoord;
    Sprite circle;

    void Awake()
    {
        grid = FindFirstObjectByType<HexGridManager>();
        circle = ProceduralSprites.CreateCircle(grid.cellSize * 0.4f);
        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = bodyColor;
        sr.sortingOrder = 5;
        GetComponent<CircleCollider2D>().radius = grid.cellSize * 0.4f;
    }

    void Start()
    {
        currentCoord = grid.WorldToAxial(transform.position);
        transform.position = grid.AxialToWorld(currentCoord);
    }

    public IEnumerator TakeTurn(PlayerController player)
    {
        if (!CanAct(player)) yield break;
        for (int i = 0; i < moveSteps; i++)
        {
            if (!CanAct(player)) yield break;
            if (HexGridManager.Distance(currentCoord, player.CurrentCoord) <= 1) break;
            Vector2Int next = BestNeighborTowards(player.CurrentCoord);
            if (next == currentCoord) break;
            yield return MoveTo(next, player);
        }

        if (!CanAct(player)) yield break;

        if (isRanged)
        {
            // Vision controls pursuit; shot range is checked only AFTER all movement finishes.
            int distance = HexGridManager.Distance(currentCoord, player.CurrentCoord);
            if (distance < 1 || distance > attackRange) yield break;
            yield return Projectile.Travel(this, transform.position, grid.AxialToWorld(player.CurrentCoord),
                grid.cellSize * 0.18f, target =>
                {
                    if (player == null) return;
                    var hit = Physics2D.OverlapPoint(target);
                    if (hit != null && hit.TryGetComponent<PlayerHealth>(out var health)) health.TakeDamage(1);
                });
        }
        else if (HexGridManager.Distance(currentCoord, player.CurrentCoord) == 1)
        {
            if (player.TryGetComponent<PlayerHealth>(out var health)) health.TakeDamage(1);
        }
    }

    // Permanent vision for now; future perception can supply this flag without blocking turns.
    bool CanAct(PlayerController player) => this != null && isActiveAndEnabled && player != null && hasPlayerVision;

    Vector2Int BestNeighborTowards(Vector2Int target)
    {
        Vector2Int best = currentCoord;
        int bestDist = HexGridManager.Distance(currentCoord, target);
        foreach (var dir in Directions)
        {
            Vector2Int candidate = currentCoord + dir;
            if (!grid.Exists(candidate)) continue;
            int dist = HexGridManager.Distance(candidate, target);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }
        return best;
    }

    IEnumerator MoveTo(Vector2Int target, PlayerController player)
    {
        Vector3 from = transform.position;
        Vector3 to = grid.AxialToWorld(target);
        float dist = Vector3.Distance(from, to);
        float duration = Mathf.Max(0.05f, dist / moveSpeed);
        float t = 0f;
        while (t < duration)
        {
            if (!CanAct(player))
            {
                if (this != null) transform.position = from;
                yield break;
            }
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        if (!CanAct(player))
        {
            if (this != null) transform.position = from;
            yield break;
        }
        transform.position = to;
        currentCoord = target;
    }

    void OnDestroy()
    {
        if (circle == null) return;
        Destroy(circle.texture);
        Destroy(circle);
    }
}
