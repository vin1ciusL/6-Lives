// Fresh Play Mode. Real Input System -> player shot -> enemy turn -> hover -> next move.
// unity command script eval --file Tools/Validation/HexGridInputCheck.cs --timeout 60
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
var player=Object.FindFirstObjectByType<PlayerController>();
var grid=Object.FindFirstObjectByType<HexGridManager>();
var manager=Object.FindFirstObjectByType<EnemyTurnManager>();
var originals=Object.FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
var created=new List<GameObject>();
var settings=InputSystem.settings;
var originalInput=settings.editorInputBehaviorInPlayMode;
var originalBackground=settings.backgroundBehavior;
settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
var mouse=InputSystem.AddDevice<Mouse>();
void Check(bool condition,string message){if(!condition)throw new Exception(message);}
Vector2 ScreenPoint(Vector2Int coord)=>Camera.main.WorldToScreenPoint(grid.AxialToWorld(coord));
SpriteRenderer Cell(Vector2Int coord)=>grid.GetComponentsInChildren<SpriteRenderer>().Single(s=>s.name==$"Hex_{coord.x}_{coord.y}");
async Task Hover(Vector2Int coord){InputSystem.QueueStateEvent(mouse,new MouseState{position=ScreenPoint(coord)});await Task.Delay(120);}
async Task Click(Vector2Int coord){await Hover(coord);InputSystem.QueueStateEvent(mouse,new MouseState{position=ScreenPoint(coord)}.WithButton(MouseButton.Left));await Task.Delay(80);InputSystem.QueueStateEvent(mouse,new MouseState{position=ScreenPoint(coord)});await Task.Delay(80);}
async Task<EnemyUnit> Spawn(bool ranged,Vector2Int coord){var template=originals.First(e=>e.isRanged==ranged);var go=Object.Instantiate(template.gameObject);created.Add(go);go.name="InputRegressionEnemy";go.transform.position=grid.AxialToWorld(coord);go.SetActive(true);await Task.Delay(80);return go.GetComponent<EnemyUnit>();}
try {
    Check(player.CurrentCoord==Vector2Int.zero,"Fresh Play Mode required");
    foreach(var enemy in originals)enemy.gameObject.SetActive(false);
    var ranged=await Spawn(true,new Vector2Int(14,0));
    await Spawn(false,new Vector2Int(26,0));
    for(int turn=1;turn<=2;turn++) {
        var move=new Vector2Int(turn,0);
        await Hover(move);
        Check(Cell(move).color==grid.highlightColor,"Movement hover missing at turn "+turn);
        Check(Cell(move).sortingOrder>Cell(move+new Vector2Int(0,1)).sortingOrder,"Hover must be above grid");
        await Click(move);
        await Wait.Until(()=>player.CurrentCoord==move,timeoutMs:2000);
        await Task.Delay(600);
        var target=move+new Vector2Int(1,0);
        await Hover(target);
        Check(Cell(target).color==grid.attackHighlightColor,"Attack phase missing");
        await Click(target);
        await Wait.Until(()=>manager.IsActing,timeoutMs:2000);
        Check(Cell(target).color==grid.defaultColor,"Hover must clear during enemy turn");
        // Input during enemies' actions must not be queued as a later move.
        await Click(move+new Vector2Int(0,-1));
        await Wait.Until(()=>!manager.IsActing,timeoutMs:8000);
        Check(player.CurrentCoord==move,"Enemy-turn input moved player");
        await Hover(target);
        Check(Cell(target).color==grid.highlightColor,"Movement hover did not return after enemy turn "+turn);
        Check(HexGridManager.Distance(grid.WorldToAxial(ranged.transform.position),player.CurrentCoord)>player.moveRange,"Scenario must keep ranged beyond player range");
    }
    var victim=await Spawn(true,new Vector2Int(3,-1));
    var thirdMove=new Vector2Int(2,-1);
    await Click(thirdMove);
    await Wait.Until(()=>player.CurrentCoord==thirdMove,timeoutMs:2000);
    await Task.Delay(600);
    await Click(new Vector2Int(3,-1));
    await Wait.Until(()=>victim==null,timeoutMs:2000);
    await Wait.Until(()=>!manager.IsActing,timeoutMs:8000);
    await Hover(new Vector2Int(3,-1));
    Check(Cell(new Vector2Int(3,-1)).color==grid.highlightColor,"Killed ranged must not block next turn/hover");
    Check(player.GetComponent<SpriteRenderer>().color==Color.blue,"Player must remain blue");
    return "PASS: three mouse-driven cycles; ranged beyond player range on turns 1 and 2; melee distant; input locked during enemy turn; hover priority/phase restoration; ranged killed by real player shot before enemy queue; next movement ready.";
} finally {
    InputSystem.RemoveDevice(mouse);
    settings.editorInputBehaviorInPlayMode=originalInput;
    settings.backgroundBehavior=originalBackground;
    foreach(var go in created)if(go!=null)Object.Destroy(go);
    foreach(var enemy in originals)if(enemy!=null)enemy.gameObject.SetActive(true);
}
