// Fresh Play Mode. Checks finite shot range separately from vision and samples movement/shot order.
// unity command script eval --file Tools/Validation/RangedAttackCheck.cs --timeout 60
var grid=Object.FindFirstObjectByType<HexGridManager>();
var player=Object.FindFirstObjectByType<PlayerController>();
var manager=Object.FindFirstObjectByType<EnemyTurnManager>();
var originals=Object.FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
var template=originals.First(e=>e.isRanged);
var created=new List<GameObject>();
var results=new List<string>();
bool playerEnabled=player.enabled;
void Check(bool value,string message){if(!value)throw new Exception(message);}
async Task Case(int start,int steps,int range,bool vision,int expectedEnd,bool shouldFire){
    var go=Object.Instantiate(template.gameObject);created.Add(go);
    go.name="RangeRegressionEnemy";
    go.transform.position=grid.AxialToWorld(new Vector2Int(start,0));
    var enemy=go.GetComponent<EnemyUnit>();
    enemy.moveSteps=steps;enemy.attackRange=range;enemy.hasPlayerVision=vision;enemy.moveSpeed=3;
    go.SetActive(true);await Task.Delay(80);
    var shotIds=new HashSet<int>();bool premature=false;bool movedAfterShot=false;
    var expected=grid.AxialToWorld(new Vector2Int(expectedEnd,0));
    Vector3 firstShotPosition=Vector3.zero;
    UnityEditor.EditorApplication.CallbackFunction sample=()=>{
        if(enemy==null)return;
        var shot=GameObject.Find("Shot");
        if(shot!=null){
            if(shotIds.Count==0)firstShotPosition=enemy.transform.position;
            shotIds.Add(shot.GetInstanceID());
            if(Vector3.Distance(enemy.transform.position,expected)>0.001f)premature=true;
        }
        if(shotIds.Count>0 && Vector3.Distance(enemy.transform.position,firstShotPosition)>0.001f)movedAfterShot=true;
    };
    int healthBefore=player.GetComponent<PlayerHealth>().Current;
    UnityEditor.EditorApplication.update+=sample;
    try{
        manager.BeginTurn(player);
        await Wait.Until(()=>!manager.IsActing,timeoutMs:6000);
        sample();
        Check(Vector3.Distance(enemy.transform.position,expected)<0.001f,"Wrong final position from "+start);
        Check(shotIds.Count==(shouldFire?1:0),"Wrong shot count at distance "+expectedEnd+" with range "+range);
        Check(!premature && !movedAfterShot,"Enemy fired before completing movement");
        if(!shouldFire)Check(player.GetComponent<PlayerHealth>().Current==healthBefore,"Out-of-range/no-vision damaged player");
        results.Add($"PASS start={start}, steps={steps}, shotRange={range}, vision={vision}: end={expectedEnd}, shots={shotIds.Count}, movement finished before shot");
    }finally{UnityEditor.EditorApplication.update-=sample;Object.Destroy(go);await Task.Delay(80);}
}
try{
    Check(template.attackRange==4 && template.hasPlayerVision,"Default ranged must have shot range 4 and permanent vision");
    player.enabled=false;
    foreach(var enemy in originals)enemy.gameObject.SetActive(false);
    await Case(8,3,4,true,5,false); // Sees player beyond weapon range; pursues, then skips shot.
    await Case(7,3,4,true,4,true); // Exact range boundary, only after all three steps.
    await Case(4,3,4,true,1,true); // Already in range: still spends all useful movement before firing.
    await Case(2,3,4,true,1,true); // Stops adjacent, not on player.
    await Case(1,3,4,true,1,true);
    await Case(4,0,4,true,4,true);
    await Case(5,0,4,true,5,false);
    await Case(6,3,2,true,3,false); // Independent configurable weapon range.
    await Case(5,3,2,true,2,true);
    await Case(2,3,4,false,2,false);
    return results;
}finally{
    foreach(var go in created)if(go!=null)Object.Destroy(go);
    foreach(var enemy in originals)if(enemy!=null)enemy.gameObject.SetActive(true);
    player.enabled=playerEnabled;
}
