// Run in Play Mode: unity command script eval --file Tools/Validation/HexGridCoverageCheck.cs
var grid = Object.FindFirstObjectByType<HexGridManager>();
var camera = Camera.main;
var originalPosition = camera.transform.position;
var originalSize = camera.orthographicSize;
void Check(bool value, string message) { if (!value) throw new Exception(message); }
void CheckVisibleCoverage() {
    for (int y=0; y<=8; y++) for (int x=0; x<=12; x++) {
        var ray = camera.ViewportPointToRay(new Vector3(x/12f, y/8f, 0));
        var plane = new Plane(grid.transform.forward, grid.transform.position);
        if (!plane.Raycast(ray, out float distance)) throw new Exception("Camera missed board");
        var coord = grid.WorldToAxial(ray.GetPoint(distance));
        if (grid.Exists(coord)) {
            var cell=grid.transform.Find($"Hex_{coord.x}_{coord.y}");
            Check(cell != null && cell.gameObject.activeSelf, "Missing visible cell " + coord);
        }
    }
    foreach(var sr in grid.GetComponentsInChildren<SpriteRenderer>()) {
        for(int i=0;i<6;i++) {
            float angle=(60*i-30)*Mathf.Deg2Rad;
            var vertex=sr.transform.TransformPoint(new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*grid.cellSize);
            Check(grid.grassTilemap.HasTile(grid.grassTilemap.WorldToCell(vertex)), "Hex protrudes beyond grass");
        }
    }
}
try {
    CheckVisibleCoverage();
    camera.transform.position = new Vector3(80, 50, originalPosition.z);
    await Task.Delay(200);
    CheckVisibleCoverage();
    Check(grid.GetReachable(grid.WorldToAxial(new Vector3(-80, -50, 0)),4).Count==60, "Offscreen logical range must be intact");
    camera.orthographicSize=12;
    await Task.Delay(200);
    CheckVisibleCoverage();
    camera.transform.position = new Vector3(192, 175, originalPosition.z);
    await Task.Delay(200);
    CheckVisibleCoverage();
    Check(!grid.Exists(grid.WorldToAxial(new Vector3(198,180,0))), "Outside grass must be rejected");
    return "PASS: viewport coverage; distant camera movement; zoom; grass boundary containment; offscreen reachability.";
} finally {
    camera.transform.position=originalPosition;
    camera.orthographicSize=originalSize;
}
