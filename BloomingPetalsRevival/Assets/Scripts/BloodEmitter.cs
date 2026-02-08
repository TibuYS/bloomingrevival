using System.Collections.Generic;
using UnityEngine;

public class BloodEmitter : MonoBehaviour
{
    public Transform bleedPoint;
    public LayerMask floorMask;

    public int maxPools = 5;
    public float trailSpawnInterval = 0.7f;

    float trailTimer;

    public bool forceBleed = false;

    List<BloodPool> activePools = new List<BloodPool>();

    private void Awake()
    {
        forceBleed = false;
    }

    public void SpawnPool()
    {
        Vector3 spawnPos = GetFloorPosition();

        var pool = BloodManager.Instance.GetPool();

        pool.Activate(
            spawnPos,
            Quaternion.Euler(0, Random.Range(0, 360), 0)
        );

        RegisterPool(pool);
    }

    void RegisterPool(BloodPool pool)
    {
        activePools.Add(pool);

        if (activePools.Count > maxPools)
        {
            var oldest = activePools[0];
            activePools.RemoveAt(0);
            BloodManager.Instance.ReturnPool(oldest);
        }
    }

    Vector3 GetFloorPosition()
    {
        Ray ray = new Ray(bleedPoint.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f, floorMask))
            return hit.point + Vector3.up * 0.01f;

        return bleedPoint.position;
    }

    void Update()
    {
        if (!forceBleed)
            return;

        trailTimer += Time.deltaTime;

        if (trailTimer >= trailSpawnInterval)
        {
            trailTimer = 0f;
            SpawnPool();
        }
    }

    bool IsBleeding()
    {
        return true;
    }
}
