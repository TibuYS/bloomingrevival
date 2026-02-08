using UnityEngine;

public enum RagdollState
{
    Animated,
    Ragdoll,
    Carried,
    Dragged
}


public class RagdollController : MonoBehaviour
{
    public Animation LegacyAnimation;
    public Rigidbody RootRigidbody;

    public RagdollState CurrentState = RagdollState.Animated;

    public float DropForwardOffset = 0.6f;
    public float DropUpOffset = 0.2f;
    public float DropImpulse = 1.5f;
    public float IgnorePlayerCollisionTime = 0.4f;

    public float DragSpring = 900f;
    public float DragDamper = 120f;

    public ConfigurableJoint dragJoint;
    Rigidbody draggedBody;
    public bool isDragging;

    Rigidbody[] bodies;
    Collider[] colliders;

    bool initialized;

    void Awake()
    {
        bodies = GetComponentsInChildren<Rigidbody>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        if (!LegacyAnimation)
            LegacyAnimation = GetComponent<Animation>();

        if (!RootRigidbody)
            RootRigidbody = GetComponent<Rigidbody>();

        initialized = true;

        ForceAnimatedImmediate();

        var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var r in renderers)
        {
            r.updateWhenOffscreen = true;
            var bounds = r.localBounds;
            bounds.Expand(5f);
            r.localBounds = bounds;
        }

        dragJoint = transform.Find("PelvisRoot/Hips/Spine/Spine1/Spine2/Spine3/RightShoulder/RightArm/RightArmRoll/RightForeArm/RightForeArmRoll/RightHand").gameObject.GetComponent<ConfigurableJoint>();
    }

    public void StartDragging(Rigidbody footBody, Transform dragPoint)
    {
        if (!initialized) return;
        if (CurrentState != RagdollState.Ragdoll) return;

        CurrentState = RagdollState.Dragged;
        isDragging = true;

        draggedBody = footBody;

        dragJoint = footBody.gameObject.AddComponent<ConfigurableJoint>();

        Rigidbody playerRB = dragPoint.GetComponentInParent<Rigidbody>();

        dragJoint.connectedBody = playerRB;

        dragJoint.autoConfigureConnectedAnchor = false;
        dragJoint.connectedAnchor = playerRB.transform.InverseTransformPoint(dragPoint.position);

        dragJoint.angularXMotion = ConfigurableJointMotion.Locked;
        dragJoint.angularYMotion = ConfigurableJointMotion.Locked;
        dragJoint.angularZMotion = ConfigurableJointMotion.Locked;

        dragJoint.xMotion = ConfigurableJointMotion.Limited;
        dragJoint.yMotion = ConfigurableJointMotion.Limited;
        dragJoint.zMotion = ConfigurableJointMotion.Limited;

        SoftJointLimitSpring spring = new SoftJointLimitSpring();
        spring.spring = DragSpring;
        spring.damper = DragDamper;

        dragJoint.linearLimitSpring = spring;

        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = 0.05f;

        dragJoint.linearLimit = limit;
    }

    public void StopDragging()
    {
        if (dragJoint)
            Destroy(dragJoint);

        draggedBody = null;
        isDragging = false;

        if (CurrentState == RagdollState.Dragged)
            CurrentState = RagdollState.Ragdoll;
    }


    public void DropFromCarrier(Transform playerRoot, Collider playerCollider)
    {
        if (!initialized) return;

        Vector3 dropPos =
            playerRoot.position +
            playerRoot.forward * DropForwardOffset +
            Vector3.up * DropUpOffset;

        transform.position = dropPos;
        transform.rotation = playerRoot.rotation;

        ForceRagdollImmediate();

        foreach (var rb in bodies)
        {
            rb.AddForce(playerRoot.forward * DropImpulse, ForceMode.Impulse);
        }

        if (playerCollider)
            StartCoroutine(TemporaryIgnorePlayer(playerCollider));
    }

    System.Collections.IEnumerator TemporaryIgnorePlayer(Collider playerCol)
    {
        var myCols = GetComponentsInChildren<Collider>();

        foreach (var c in myCols)
            Physics.IgnoreCollision(c, playerCol, true);

        yield return new WaitForSeconds(IgnorePlayerCollisionTime);

        foreach (var c in myCols)
            Physics.IgnoreCollision(c, playerCol, false);
    }


    public void ToAnimated()
    {
        if (!initialized) return;
        if (CurrentState == RagdollState.Animated) return;

        ForceAnimatedImmediate();
    }

    public void ToRagdoll()
    {
        if (!initialized) return;
        if (CurrentState == RagdollState.Ragdoll) return;

        ForceRagdollImmediate();
    }

    public void ToCarried(Transform parent)
    {
        if (!initialized) return;
        if (CurrentState == RagdollState.Carried) return;

        ForceCarriedImmediate(parent);
    }

    void ForceAnimatedImmediate()
    {
        CurrentState = RagdollState.Animated;

        transform.SetParent(null);

        if (LegacyAnimation)
            LegacyAnimation.enabled = true;

        foreach (var rb in bodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var c in colliders)
            c.enabled = false;

        if (RootRigidbody)
        {
            RootRigidbody.isKinematic = false;
            RootRigidbody.useGravity = true;
        }
    }

    void ForceRagdollImmediate()
    {
        CurrentState = RagdollState.Ragdoll;

        transform.SetParent(null);

        if (LegacyAnimation)
            LegacyAnimation.enabled = false;

        foreach (var rb in bodies)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
        }

        foreach (var c in colliders)
            c.enabled = true;

        if (RootRigidbody)
            RootRigidbody.isKinematic = true;
    }

    void ForceCarriedImmediate(Transform parent)
    {
        CurrentState = RagdollState.Carried;

        if (LegacyAnimation)
            LegacyAnimation.enabled = false;

        foreach (var rb in bodies)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

        foreach (var c in colliders)
            c.enabled = false;

        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}