using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StudentVision : MonoBehaviour
{

    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    public List<Transform> visibleTargets = new List<Transform>();

    void Start()
    {
        StartCoroutine("FOVRoutine", .2f);
    }

    IEnumerator FOVRoutine(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    public static GameObject FindMainParent(GameObject obj)
    {
        Transform currentTransform = obj.transform;
        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
        }
        return currentTransform.gameObject;
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    if(target.gameObject.layer != 7 && target.gameObject.layer != 11)
                    {
                        visibleTargets.Add(target);
                    }
                    else
                    {
                        visibleTargets.Add(FindMainParent(target.gameObject).transform);
                    }
                    
                }
            }
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

        Gizmos.color = Color.red;
        foreach (Transform visibleTarget in visibleTargets)
        {
            Gizmos.DrawLine(transform.position, visibleTarget.position);
        }
    }

    public bool canSeeCorpse()
    {
        foreach(Transform tr in visibleTargets)
        {
            if(tr.gameObject.tag == "Corpse")
            {
                return true;
            }
        }
        return false;
    }

    public bool canSeePlayerCarryingCorpse()
    {
            if (visibleTargets.Contains(ProtagonistScript.instance.transform) && ProtagonistScript.instance.corpseInHand != null)
            {
                return true;
            }
        return false;
    }

    public bool canSeeBlood()
    {
        foreach (Transform tr in visibleTargets)
        {
            if (tr.gameObject.tag == "Blood")
            {
                return true;
            }
        }
        return false;
    }
}
