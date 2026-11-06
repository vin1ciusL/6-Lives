using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTurnManager : MonoBehaviour
{
    public bool IsActing { get; private set; }

    public void BeginTurn(PlayerController player)
    {
        if (IsActing || !isActiveAndEnabled || player == null) return;
        StartCoroutine(RunTurn(player));
    }

    IEnumerator RunTurn(PlayerController player)
    {
        IsActing = true;
        try
        {
            // Player impact may have queued Destroy; let those enemies leave before taking the snapshot.
            yield return null;
            var melee = new List<EnemyUnit>();
            var ranged = new List<EnemyUnit>();
            foreach (var enemy in FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None))
                (enemy.isRanged ? ranged : melee).Add(enemy);

            foreach (var enemy in melee)
            {
                if (player == null) yield break;
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                yield return enemy.TakeTurn(player);
            }
            foreach (var enemy in ranged)
            {
                if (player == null) yield break;
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                yield return enemy.TakeTurn(player);
            }
        }
        finally
        {
            IsActing = false;
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        IsActing = false;
    }
}
