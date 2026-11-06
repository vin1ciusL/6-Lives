// Fresh Play Mode: unity command script eval --file Tools/Validation/EnemyTurnRegressionCheck.cs --timeout 60
var player = Object.FindFirstObjectByType<PlayerController>();
var manager = Object.FindFirstObjectByType<EnemyTurnManager>();
var grid = Object.FindFirstObjectByType<HexGridManager>();
var originals = Object.FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
var meleeTemplate = originals.First(e => !e.isRanged);
var rangedTemplate = originals.First(e => e.isRanged);
var created = new List<GameObject>();
var passed = new List<string>();
var originalGrass = grid.grassTilemap;
int originalRadius = grid.radius;
bool playerEnabled = player.enabled;
void Check(bool value, string message) { if (!value) throw new Exception(message); }
async Task EndTurn(int timeout = 6000) => await Wait.Until(() => !manager.IsActing, timeoutMs: timeout);
async Task<EnemyUnit> Spawn(bool ranged, Vector2Int coord, int steps = 3) {
    var go = Object.Instantiate((ranged ? rangedTemplate : meleeTemplate).gameObject);
    created.Add(go);
    go.name = "RegressionEnemy";
    go.transform.position = grid.AxialToWorld(coord);
    var enemy = go.GetComponent<EnemyUnit>();
    enemy.moveSteps = steps;
    enemy.hasPlayerVision = true;
    go.SetActive(true);
    await Task.Delay(80);
    return enemy;
}
async Task Remove(EnemyUnit enemy) { if (enemy != null) Object.Destroy(enemy.gameObject); await Task.Delay(80); }
try {
    Check(originals.All(e=>e.hasPlayerVision), "Existing prefab instances must default to permanent vision");
    player.enabled = false;
    foreach (var enemy in originals) enemy.gameObject.SetActive(false);
    foreach (bool ranged in new[]{false,true}) {
        var enemy = await Spawn(ranged,new Vector2Int(20,0));
        for(int turn=1;turn<=2;turn++) {
            int before=HexGridManager.Distance(grid.WorldToAxial(enemy.transform.position), player.CurrentCoord);
            manager.BeginTurn(player);
            manager.BeginTurn(player); // Duplicate requests must not run concurrent turns.
            await EndTurn();
            int after=HexGridManager.Distance(grid.WorldToAxial(enemy.transform.position), player.CurrentCoord);
            Check(before-after==3,"Distant enemy must see player and move exactly 3, turn " + turn);
        }
        passed.Add((ranged?"ranged":"melee")+": distant player, two turns, duplicate request guard");
        enemy.hasPlayerVision=false;
        var position=enemy.transform.position;
        int health=player.GetComponent<PlayerHealth>().Current;
        for(int turn=0;turn<2;turn++) { manager.BeginTurn(player); await EndTurn(1000); }
        Check(enemy.transform.position==position && player.GetComponent<PlayerHealth>().Current==health && GameObject.Find("Shot")==null,"No vision must idle without damage or shot");
        passed.Add((ranged?"ranged":"melee")+": no vision idles and releases turn");
        await Remove(enemy);
        enemy=await Spawn(ranged,new Vector2Int(8,0));
        enemy.GetComponent<Health>().TakeDamage(1);
        manager.BeginTurn(player);
        await EndTurn(1500);
        Check(enemy==null,"Queued death must be removed before acting");
        passed.Add((ranged?"ranged":"melee")+": same-frame kill/turn transition");
        enemy=await Spawn(ranged,new Vector2Int(8,0));
        position=enemy.transform.position;
        manager.BeginTurn(player);
        await Wait.Until(()=>enemy.transform.position!=position,timeoutMs:1500);
        enemy.GetComponent<Health>().TakeDamage(1);
        await EndTurn(1500);
        passed.Add((ranged?"ranged":"melee")+": destroyed during movement");
        enemy=await Spawn(ranged,new Vector2Int(8,0));
        position=enemy.transform.position;
        manager.BeginTurn(player);
        await Wait.Until(()=>enemy.transform.position!=position,timeoutMs:1500);
        enemy.hasPlayerVision=false;
        await EndTurn(1500);
        Check(enemy.transform.position==position,"Vision loss must cancel unfinished step cleanly");
        passed.Add((ranged?"ranged":"melee")+": vision lost during movement");
        await Remove(enemy);
    }
    var shooter=await Spawn(true,new Vector2Int(4,0),0);
    manager.BeginTurn(player);
    await Wait.Until(()=>GameObject.Find("Shot")!=null,timeoutMs:1500);
    Object.Destroy(GameObject.Find("Shot"));
    await EndTurn(1500);
    passed.Add("interrupted ranged projectile releases turn");
    manager.BeginTurn(player);
    await Wait.Until(()=>GameObject.Find("Shot")!=null,timeoutMs:1500);
    Object.Destroy(shooter.gameObject);
    await EndTurn(1500);
    await Task.Delay(80);
    Check(GameObject.Find("Shot")==null,"Destroyed shooter must clean up projectile");
    passed.Add("destroyed ranged shooter releases turn and cleans shot");
    foreach(bool ranged in new[]{false,true}) {
        var enemy=await Spawn(ranged,new Vector2Int(8,0));
        var position=enemy.transform.position;
        grid.grassTilemap=null; grid.radius=0;
        manager.BeginTurn(player);
        await EndTurn(2500);
        Check(enemy.transform.position==position,"No valid movement cell must not move enemy");
        grid.grassTilemap=originalGrass; grid.radius=originalRadius;
        await Remove(enemy);
    }
    passed.Add("no valid neighboring cells: both types finish");
    manager.BeginTurn(null);
    Check(!manager.IsActing,"Missing player must not start turn");
    passed.Add("missing player safely skipped");
    return passed;
} finally {
    manager.StopAllCoroutines();
    grid.grassTilemap=originalGrass; grid.radius=originalRadius;
    foreach(var go in created) if(go!=null) Object.Destroy(go);
    foreach(var enemy in originals) if(enemy!=null) enemy.gameObject.SetActive(true);
    player.enabled=playerEnabled;
}
