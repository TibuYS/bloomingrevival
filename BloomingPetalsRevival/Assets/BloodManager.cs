using System.Collections.Generic;
using UnityEngine;

public class BloodManager : MonoBehaviour
{
    public static BloodManager Instance;

    public BloodPool poolPrefab;
    public int poolReserve = 50;

    Queue<BloodPool> availablePools = new Queue<BloodPool>();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolReserve; i++)
        {
            var p = Instantiate(poolPrefab, transform);
            p.gameObject.SetActive(false);
            availablePools.Enqueue(p);
        }
    }

    public BloodPool GetPool()
    {
        if (availablePools.Count > 0)
            return availablePools.Dequeue();

        return Instantiate(poolPrefab);
    }

    public void ReturnPool(BloodPool pool)
    {
        pool.Deactivate();
        availablePools.Enqueue(pool);
    }
}
