using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CircleCollider2D))]
public class PlayerController : MonoBehaviour
{
    public int moveRange = 4;
    public float moveSpeed = 6f;

    HexGridManager grid;
    EnemyTurnManager enemyTurnManager;
    SpriteRenderer sr;
    Camera viewCamera;
    Vector2Int currentCoord;
    Vector2Int? hoveredCoord;
    bool isMoving;
    bool isShooting;
    bool hasMoved;
    int turnNumber = 1;

    public Vector2Int CurrentCoord => currentCoord;

    void Awake()
    {
        grid = FindFirstObjectByType<HexGridManager>();
        enemyTurnManager = FindFirstObjectByType<EnemyTurnManager>();

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralSprites.CreateCircle(grid.cellSize * 0.4f);
        sr.color = Color.blue;
        sr.sortingOrder = 5;

        GetComponent<CircleCollider2D>().radius = grid.cellSize * 0.4f;
        viewCamera = Camera.main;
    }

    void Start()
    {
        currentCoord = grid.WorldToAxial(transform.position);
        transform.position = grid.AxialToWorld(currentCoord);
    }

    void Update()
    {
        if (isMoving || isShooting || (enemyTurnManager != null && enemyTurnManager.IsActing))
        {
            ClearHover();
            return;
        }

        if (Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = viewCamera.ScreenPointToRay(screenPos);
        var boardPlane = new Plane(grid.transform.forward, grid.transform.position);
        if (!boardPlane.Raycast(ray, out float distance)) return;
        Vector3 mouseWorld = ray.GetPoint(distance);
        bool clicked = Mouse.current.leftButton.wasPressedThisFrame;

        bool attackMode = hasMoved;
        Vector2Int coord = grid.WorldToAxial(mouseWorld);
        int dist = HexGridManager.Distance(currentCoord, coord);
        bool valid = grid.Exists(coord) && dist > 0 && dist <= moveRange;
        Color activeHighlight = attackMode ? grid.attackHighlightColor : grid.highlightColor;

        if (coord != hoveredCoord)
        {
            if (hoveredCoord.HasValue) grid.ResetCellColor(hoveredCoord.Value);
            hoveredCoord = valid ? coord : (Vector2Int?)null;
        }
        // Camera motion can recycle a renderer even when the hovered coordinate did not change.
        if (hoveredCoord.HasValue) grid.SetCellColor(hoveredCoord.Value, activeHighlight);

        if (clicked && hoveredCoord.HasValue)
        {
            if (attackMode) FireAt(hoveredCoord.Value);
            else StartCoroutine(MoveTo(hoveredCoord.Value));
        }
    }

    void ClearHover()
    {
        if (hoveredCoord.HasValue) grid.ResetCellColor(hoveredCoord.Value);
        hoveredCoord = null;
    }

    void OnGUI()
    {
        string status = isMoving ? "Movendo..." : isShooting ? "Atirando..." :
            (enemyTurnManager != null && enemyTurnManager.IsActing) ? "Turno inimigo..." :
            hasMoved ? $"Clique numa casa vermelha (alcance {moveRange}) para atirar" :
            $"Clique numa casa azul (alcance {moveRange}) para mover";
        GUI.Box(new Rect(12, 12, Mathf.Min(430, Screen.width - 24), 52),
            $"Turno {turnNumber}\n{status}");
    }

    IEnumerator MoveTo(Vector2Int target)
    {
        isMoving = true;
        ClearHover();

        Vector3 from = transform.position;
        Vector3 to = grid.AxialToWorld(target);
        float dist = Vector3.Distance(from, to);
        float duration = Mathf.Max(0.05f, dist / moveSpeed);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        transform.position = to;
        currentCoord = target;
        isMoving = false;
        hasMoved = true;
    }

    void FireAt(Vector2Int target)
    {
        ClearHover();
        StartCoroutine(FireAndEndTurn(target));
    }

    IEnumerator FireAndEndTurn(Vector2Int target)
    {
        isShooting = true;
        yield return Projectile.Travel(this, transform.position, grid.AxialToWorld(target), grid.cellSize * 0.18f, OnShotArrive);
        isShooting = false;
        hasMoved = false;
        turnNumber++;
        enemyTurnManager?.BeginTurn(this);
    }

    void OnShotArrive(Vector3 target)
    {
        var hit = Physics2D.OverlapPoint(target);
        if (hit != null && hit.TryGetComponent<Health>(out var health)) health.TakeDamage(1);
    }

    void OnDestroy()
    {
        if (sr == null || sr.sprite == null) return;
        Destroy(sr.sprite.texture);
        Destroy(sr.sprite);
    }
}
