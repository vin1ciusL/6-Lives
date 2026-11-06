// Fresh Play Mode. Exercises a pooled cell being recreated under a stationary cursor.
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
var grid = Object.FindFirstObjectByType<HexGridManager>();
var player = Object.FindFirstObjectByType<PlayerController>();
var camera = Camera.main;
var follow = camera.GetComponent<CameraFollow>();
var original = camera.transform.position;
bool followEnabled = follow.enabled;
var settings = InputSystem.settings;
var originalInputBehavior = settings.editorInputBehaviorInPlayMode;
var originalBackground = settings.backgroundBehavior;
settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
var mouse = InputSystem.AddDevice<Mouse>();
var coord = player.CurrentCoord + new Vector2Int(1,0);
SpriteRenderer Cell() => grid.GetComponentsInChildren<SpriteRenderer>().Single(s => s.name == $"Hex_{coord.x}_{coord.y}");
try {
    follow.enabled = false;
    InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(grid.AxialToWorld(coord)) });
    await Task.Delay(150);
    if(Cell().color != grid.highlightColor) throw new Exception("Initial hover missing");
    player.enabled = false;
    camera.transform.position = original + new Vector3(80,0,0);
    await Task.Delay(150);
    camera.transform.position = original;
    await Task.Delay(150);
    player.enabled = true;
    await Task.Delay(150);
    if(Cell().color != grid.highlightColor) throw new Exception("Hover lost after renderer recycling under unchanged mouse coordinate");
    return "PASS: hover restored on recreated cell without moving mouse.";
} finally {
    player.enabled = true;
    camera.transform.position = original;
    follow.enabled = followEnabled;
    InputSystem.RemoveDevice(mouse);
    settings.editorInputBehaviorInPlayMode = originalInputBehavior;
    settings.backgroundBehavior = originalBackground;
}
