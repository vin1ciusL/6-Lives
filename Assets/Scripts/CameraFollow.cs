using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 8f;

    void Awake()
    {
        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 pos = transform.position;
        Vector3 desired = new Vector3(target.position.x, target.position.y, pos.z);
        transform.position = Vector3.Lerp(pos, desired, smoothSpeed * Time.deltaTime);
    }
}
