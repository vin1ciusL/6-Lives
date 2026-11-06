using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexGridManager : MonoBehaviour
{
    public int radius = 8;
    public float cellSize = 0.6f;
    public Tilemap grassTilemap;
    public Color defaultColor = Color.black;
    public Color highlightColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color attackHighlightColor = new Color(1f, 0.2f, 0.2f, 1f);

    readonly Dictionary<Vector2Int, SpriteRenderer> cells = new Dictionary<Vector2Int, SpriteRenderer>();
    readonly HashSet<Vector2Int> visibleCoordinates = new HashSet<Vector2Int>();
    readonly List<Vector2Int> retiredCoordinates = new List<Vector2Int>();
    readonly Stack<SpriteRenderer> spareCells = new Stack<SpriteRenderer>();
    Sprite hexSprite;
    Camera viewCamera;
    TilemapRenderer grassRenderer;
    Matrix4x4 lastViewProjection;
    Matrix4x4 lastGridTransform;
    // Slight overlap closes the transparent antialiasing seam between outlines.
    float CellSpacing => cellSize * 0.98f;

    void Awake()
    {
        viewCamera = Camera.main;
        if (grassTilemap != null) grassRenderer = grassTilemap.GetComponent<TilemapRenderer>();
        hexSprite = CreateHexOutlineSprite();
        BuildGrid();
    }

    void LateUpdate()
    {
        if (viewCamera != null &&
            (lastViewProjection != viewCamera.projectionMatrix * viewCamera.worldToCameraMatrix ||
             lastGridTransform != transform.localToWorldMatrix))
            BuildGrid();
    }

    void BuildGrid()
    {
        if (viewCamera == null) return;
        var plane = new Plane(transform.forward, transform.position);
        var bounds = new Bounds();
        for (int i = 0; i < 4; i++)
        {
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(i % 2, i / 2, 0f));
            if (!plane.Raycast(ray, out float distance)) return;
            Vector3 corner = transform.InverseTransformPoint(ray.GetPoint(distance));
            if (i == 0) bounds = new Bounds(corner, Vector3.zero);
            else bounds.Encapsulate(corner);
        }
        bounds.Expand(cellSize * 4f);
        visibleCoordinates.Clear();
        int minR = Mathf.FloorToInt(bounds.min.y / (CellSpacing * 1.5f));
        int maxR = Mathf.CeilToInt(bounds.max.y / (CellSpacing * 1.5f));
        for (int r = minR; r <= maxR; r++)
        {
            int minQ = Mathf.FloorToInt(bounds.min.x / (CellSpacing * Mathf.Sqrt(3f)) - r / 2f);
            int maxQ = Mathf.CeilToInt(bounds.max.x / (CellSpacing * Mathf.Sqrt(3f)) - r / 2f);
            for (int q = minQ; q <= maxQ; q++)
            {
                var coord = new Vector2Int(q, r);
                if (Exists(coord)) visibleCoordinates.Add(coord);
            }
        }

        // The board covers the entire Tilemap; only its visible cells need renderers.
        retiredCoordinates.Clear();
        foreach (var pair in cells)
            if (!visibleCoordinates.Contains(pair.Key)) retiredCoordinates.Add(pair.Key);
        foreach (var coord in retiredCoordinates)
        {
            var sr = cells[coord];
            sr.gameObject.SetActive(false);
            spareCells.Push(sr);
            cells.Remove(coord);
        }
        foreach (var coord in visibleCoordinates)
        {
            if (cells.ContainsKey(coord)) continue;
            SpriteRenderer sr;
            if (spareCells.Count > 0) sr = spareCells.Pop();
            else
            {
                var go = new GameObject();
                go.transform.SetParent(transform, false);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = hexSprite;
            }
            sr.name = $"Hex_{coord.x}_{coord.y}";
            sr.transform.position = AxialToWorld(coord);
            sr.color = defaultColor;
            sr.sortingLayerID = grassRenderer != null ? grassRenderer.sortingLayerID : 0;
            sr.sortingOrder = BaseSortingOrder;
            sr.gameObject.SetActive(true);
            cells[coord] = sr;
        }
        lastViewProjection = viewCamera.projectionMatrix * viewCamera.worldToCameraMatrix;
        lastGridTransform = transform.localToWorldMatrix;
    }

    public Vector3 AxialToWorld(Vector2Int coord)
    {
        float x = CellSpacing * Mathf.Sqrt(3f) * (coord.x + coord.y / 2f);
        float y = CellSpacing * 1.5f * coord.y;
        return transform.TransformPoint(new Vector3(x, y, 0f));
    }

    public Vector2Int WorldToAxial(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        float q = (Mathf.Sqrt(3f) / 3f * local.x - 1f / 3f * local.y) / CellSpacing;
        float r = (2f / 3f * local.y) / CellSpacing;
        return CubeRound(q, r);
    }

    static Vector2Int CubeRound(float q, float r)
    {
        float x = q, z = r, y = -x - z;
        float rx = Mathf.Round(x), ry = Mathf.Round(y), rz = Mathf.Round(z);
        float dx = Mathf.Abs(rx - x), dy = Mathf.Abs(ry - y), dz = Mathf.Abs(rz - z);
        if (dx > dy && dx > dz) rx = -ry - rz;
        else if (dy > dz) ry = -rx - rz;
        else rz = -rx - ry;
        return new Vector2Int((int)rx, (int)rz);
    }

    public static int Distance(Vector2Int a, Vector2Int b)
    {
        int ax = a.x, az = a.y, ay = -ax - az;
        int bx = b.x, bz = b.y, by = -bx - bz;
        return (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) + Mathf.Abs(az - bz)) / 2;
    }

    int BaseSortingOrder => grassRenderer != null ? grassRenderer.sortingOrder + 1 : 1;

    public bool Exists(Vector2Int coord)
    {
        if (grassTilemap == null) return Distance(Vector2Int.zero, coord) <= radius;
        Vector3 center = AxialToWorld(coord);
        // Require the complete sprite footprint to lie on painted grass, including edges/holes.
        float halfSize = cellSize * 64f / 62f;
        Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = transform.TransformVector(new Vector3((i % 2 == 0 ? -1 : 1) * halfSize,
                (i / 2 == 0 ? -1 : 1) * halfSize, 0));
            Vector3Int tile = grassTilemap.WorldToCell(center + offset);
            min = Vector3Int.Min(min, tile);
            max = Vector3Int.Max(max, tile);
        }
        for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++)
                if (!grassTilemap.HasTile(new Vector3Int(x, y, min.z))) return false;
        return true;
    }

    // shortcut: pure distance check, no obstacles yet. Swap for BFS flood-fill once terrain can block tiles.
    public List<Vector2Int> GetReachable(Vector2Int origin, int range)
    {
        var result = new List<Vector2Int>();
        for (int q = -range; q <= range; q++)
            for (int r = Mathf.Max(-range, -q - range); r <= Mathf.Min(range, -q + range); r++)
            {
                var coord = origin + new Vector2Int(q, r);
                if (coord != origin && Exists(coord)) result.Add(coord);
            }
        return result;
    }

    public void SetCellColor(Vector2Int coord, Color color)
    {
        if (!cells.TryGetValue(coord, out var sr)) return;
        sr.color = color;
        sr.sortingOrder = color == defaultColor ? BaseSortingOrder : BaseSortingOrder + 1;
    }

    public void ResetCellColor(Vector2Int coord) => SetCellColor(coord, defaultColor);

    Sprite CreateHexOutlineSprite()
    {
        const int texSize = 128;
        const float thickness = 3f;
        float outerR = texSize / 2f - 2f;
        float innerR = outerR - thickness;
        Vector2 center = new Vector2(texSize / 2f, texSize / 2f);

        Vector2[] outer = HexVerts(center, outerR);
        Vector2[] inner = HexVerts(center, innerR);

        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[texSize * texSize];
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                bool inOuter = PointInPolygon(p, outer);
                bool inInner = PointInPolygon(p, inner);
                pixels[y * texSize + x] = (inOuter && !inInner) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        float ppu = outerR / cellSize;
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), ppu);
    }

    void OnDestroy()
    {
        if (hexSprite == null) return;
        Destroy(hexSprite.texture);
        Destroy(hexSprite);
    }

    static Vector2[] HexVerts(Vector2 center, float r)
    {
        var verts = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float deg = 60f * i - 30f;
            float rad = Mathf.Deg2Rad * deg;
            verts[i] = center + new Vector2(r * Mathf.Cos(rad), r * Mathf.Sin(rad));
        }
        return verts;
    }

    static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }
}
