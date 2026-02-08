using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using UnityEngine.AI;
using UnityEngine.Events;

public class StudentScript : MonoBehaviour
{
    public enum CorpseState
    {
        Alive,
        Ragdoll,
        PickupAnimating,
        Carried
    }

    public CorpseState corpseState = CorpseState.Alive;

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    public SpawnStudentData StudentData;

    [Space]
    [Header("Components")]
    public Animation StudentAnimation;
    public NavMeshAgent StudentAgent;
    public StudentVision StudentVision;
    public PromptScript StudentPrompt;
    public RagdollController StudentRagdoll;
    [Header("Runtime values")]
    public bool isFollowing;

    private Collider[] IgnoreColliders;
    private Transform Head;

    private UnityEvent StudentTalkEvent;
    private UnityEvent CorpseCarryEvent;
    private UnityEvent CorpseDropEvent;
    private UnityEvent StudentAttackEvent;
    private UnityEvent StopFollowEvent;

    private DestinationSpot currentDestination;
    private DestinationData currentDestinationData;

    public void Start()
    {
        // get components
        StudentAgent = GetComponent<NavMeshAgent>();
        StudentAnimation = GetComponent<Animation>();

        // setup vision
        StudentVision = gameObject.AddComponent<StudentVision>();
        StudentVision.viewAngle = 180;
        StudentVision.viewRadius = 8;
        StudentVision.obstacleMask = GameGlobals.instance.StudentIgnore;
        StudentVision.targetMask = GameGlobals.instance.StudentTarget;

        // make sure student isnt a ragdoll on start
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        StudentRagdoll = gameObject.AddComponent<RagdollController>();
        SetRagdollState(false);

        StudentAnimation.Play(StudentData.StudentIdleAnimation);

        // random speed
        StudentAgent.speed = Random.value < 0.5f ? 1.2f : 3f;

        // new destination
        var d = GetNewDestination();
        if (d != null)
            SetStudentDestination(d);

        // setup head & prompt
        Head = transform.Find("PelvisRoot/Hips/Spine/Spine1/Spine2/Spine3/Neck/Head");
        StudentPrompt = Head.gameObject.AddComponent<PromptScript>();
        StudentPrompt.Distance = 2;
        StudentPrompt.OffsetY = 0.4f;
        SetStudentPrompt("Talk", StudentTalkEvent, 0);

        //setup events
        //kill / attack event:
        StudentAttackEvent = new UnityEvent();
        StudentAttackEvent.AddListener(Attack);
        //corpse carry event
        CorpseCarryEvent = new UnityEvent();
        CorpseCarryEvent.AddListener(CarryCorpse);
        //corpse drop event
        CorpseDropEvent = new UnityEvent();
        CorpseDropEvent.AddListener(DropCorpse);
        //talk event
        StudentTalkEvent = new UnityEvent();
        StudentTalkEvent.AddListener(Talk);
    }

    public void Update()
    {
        HandlePathfinding();
        UpdateStudentPrompt();
    }

    #region Conversation-Related
    public void Talk()
    {
        NotificationScript.instance.ShowNotification("Can't do that yet!");
        return;
    }
    #endregion

    #region Attacking & Eliminations & Screams
    public void Attack()
    {
        if (ProtagonistScript.instance.isKilling)
        {
            NotificationScript.instance.ShowNotification("Can't do that right now!");
            return;
        }
        StopAllCoroutines();
        isFollowing = false;

        Vector3 playerDirection = (ProtagonistScript.instance.transform.position - transform.position).normalized;
        if (InventoryScript.instance.CurrentItem != null)
        {
            InventoryScript.instance.CurrentItem.ItemData.isBloody = true;
        }
        Vector3 characterForward = transform.forward;
        float dotProduct = Vector3.Dot(playerDirection, characterForward);
        float threshold = 0.0f;

        if (dotProduct < threshold)
        {
            StartCoroutine(DirectKill());
        }
        else
        {
            StartCoroutine(StealthKill());
        }

    }

    public string GetAttackAnimation(ItemScript weapon)
        {
            int san = SanityScript.instance.CurrentSanity / 20;

            if (weapon.gameObject.name == "Knife")
            {
                if (san >= 4) return StudentData.StudentGender == Gender.Female ? "f02_knifeHighSanityB_00" : "knifeHighSanityB_00";
                if (san >= 2) return StudentData.StudentGender == Gender.Female ? "f02_knifeMedSanityB_00" : "knifeMedSanityB_00";
                return StudentData.StudentGender == Gender.Female ? "f02_knifeLowSanityB_00" : "knifeLowSanityB_00";
            }
            return "";
        }

    IEnumerator KillCoroutine(string protagAnim, string studentAnim, Vector3 playerLocalMove, Vector3 playerLocalRotate)
        {
            InventoryScript.instance.CloseInventory();
            InventoryScript.instance.enabled = false;
            ProtagonistScript.instance.enabled = false;
            StudentPrompt.enabled = false;
            StudentPrompt.Nearby = false;

            ProtagonistScript.instance.transform.parent = null;
            ProtagonistScript.instance.animations.Stop();
            ProtagonistScript.instance.animations.Play(protagAnim);
            StudentAnimation.Play(studentAnim);

            Stop(true, false);

            ProtagonistScript.instance.playerTransform.DOMove(playerLocalMove, 0.2f);
            ProtagonistScript.instance.playerTransform.DORotate(playerLocalRotate, 0.2f);

            ProtagonistScript.instance.isKilling = true;
            AudioSource.PlayClipAtPoint(GameGlobals.instance.KnifeStab, transform.position);

            yield return new WaitForSeconds(ProtagonistScript.instance.animations.GetClip(protagAnim).length);

            StudentPrompt.enabled = true;

            SanityScript.instance.SetSanity(SanityScript.instance.CurrentSanity - 20, 0.3f);

            ProtagonistScript.instance.ToggleBlood(true);
            ProtagonistScript.instance.enabled = true;
            gameObject.layer = 8;
            StudentData.isDead = true;
            gameObject.tag = "Corpse";

        SetRagdollState(true);
        corpseState = CorpseState.Ragdoll;
        StudentRagdoll.CurrentState = RagdollState.Ragdoll;

        ProtagonistScript.instance.isKilling = false;
            ProtagonistScript.instance.CanMove = true;
            InventoryScript.instance.enabled = true;
        }

    IEnumerator DirectKill()
        {
            string protagAnim = "f02_knifeStealthA_00";
            string studentAnim = string.Empty;
            switch (InventoryScript.instance.CurrentItem.gameObject.name)
            {
                case "Knife":
                    studentAnim= StudentData.StudentGender == Gender.Female ? "f02_knifeStealthB_00" : "knifeStealthB_00";
                break;
            }

            Transform PlayerSpot = transform.Find("DirectKillSpot");

        yield return KillCoroutine(protagAnim, studentAnim, PlayerSpot.position, PlayerSpot.eulerAngles);
        }

    IEnumerator StealthKill()
        {
            Vector3 playerLocalMove = new Vector3(0.003f, -0.008088341f, 1.03f);
            Vector3 playerLocalRotate = new Vector3(0, -180, 0);
            Transform PlayerSpot = transform.Find("StealthKillSpot");
            yield return KillCoroutine(ProtagonistScript.instance.GetAttackAnimation(InventoryScript.instance.CurrentItem), GetAttackAnimation(InventoryScript.instance.CurrentItem), PlayerSpot.position, PlayerSpot.eulerAngles);
        }
    #endregion

    #region Pathfinding
    public void HandlePathfinding()
    {
        if (!StudentAgent.enabled|| StudentAgent.isStopped) return;

        if (StudentAgent.remainingDistance > 0.1f)
        {
            if (StudentAgent.speed == 1.2f)
                StudentAnimation.Play(StudentData.StudentWalkAnimation);
            else if (StudentAgent.speed == 3f)
                StudentAnimation.Play(StudentData.StudentSprintAnimation);
        }
        else
        {
            if (!StudentAgent.isStopped)
            {
                Stop(true, true);

                if (currentDestinationData != null)
                {
                    transform.DOMove(currentDestinationData.position, 0.1f);
                    transform.DORotate(currentDestinationData.rotation, 0.1f);
                }
            }
        }
    }

    public void Stop(bool shouldStop, bool performAction = true)
    {
        StudentAgent.isStopped = shouldStop;

        if (!shouldStop || !performAction) return;

        switch (currentDestination.destinationAction)
        {
            case DestinationAction.Stand:
                StudentAnimation.Play(StudentData.StudentIdleAnimation);
                break;

            case DestinationAction.Think:
                string thinkAnim = StudentData.StudentGender == Gender.Female
                    ? "f02_thinking_00"
                    : "thinking_00";
                StudentAnimation.Play(thinkAnim);
                break;
        }
    }

    public DestinationSpot GetNewDestination()
    {
        foreach (DestinationSpot destination in StudentData.StudentDestinations)
        {
            if (destination.spotTime == TimeManager.instance.CurrentPhase)
                return destination;
        }

        return null;
    }

    public void SetStudentDestination(DestinationSpot spot)
    {
        if (spot == null) return;

        currentDestination = spot;

        if (!string.IsNullOrEmpty(spot.destinationID))
        {
            currentDestinationData = RuntimeDestinationDatabase.GetByID(spot.destinationID);

            if (currentDestinationData != null)
                StudentAgent.SetDestination(currentDestinationData.position);
            else
                Debug.LogWarning($"Destination ID '{spot.destinationID}' not found in runtime database.");
        }
        else
        {
            currentDestinationData = null;
        }
    }
    #endregion

    #region Prompt Related
    public void UpdateStudentPrompt()
    {
        if (!StudentPrompt.enabled) return;

        if (InventoryScript.instance.CurrentItem != null && InventoryScript.instance.CurrentItem.ItemData.isWeapon && !StudentData.isDead)
        {
            SetStudentPrompt("Attack", StudentAttackEvent, 2);
        }
        else if (isFollowing)
        {
            SetStudentPrompt("Stop", StopFollowEvent, 1);
        }
        else if (StudentData.isDead && ProtagonistScript.instance.corpseInHand == null)
        {
            SetStudentPrompt("Carry", CorpseCarryEvent, 1);
        }
        else if(ProtagonistScript.instance.corpseInHand == this)
        {
            SetStudentPrompt("Drop", CorpseDropEvent, 3);
        }
        else
        {
            SetStudentPrompt("Talk", StudentTalkEvent, 0);
        }
    }

    public void SetStudentPrompt(string actionLabel, UnityEvent promptEvent, int keyIndex)
    {
        if (StudentPrompt.Text == actionLabel &&
            StudentPrompt.OnPressed == promptEvent &&
            StudentPrompt.ButtonIndex == keyIndex)
            return;

        StudentPrompt.Text = actionLabel;
        StudentPrompt.OnPressed = promptEvent;
        StudentPrompt.ButtonIndex = keyIndex;
    }
    #endregion

    #region Ragdoll & Corpse
    public void SetRagdollState(bool enable)
    {
        StudentAnimation.enabled = !enable;
        StudentVision.enabled = !enable;

        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = !enable;
            rb.useGravity = enable;
            rb.detectCollisions = enable;

            if (enable)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            else
            {
                rb.interpolation = RigidbodyInterpolation.None;
            }
        }

        foreach (var col in ragdollColliders)
            col.enabled = enable;
    }


    public void CarryCorpse()
    {
        if (corpseState != CorpseState.Ragdoll) return;
        if (StudentRagdoll.CurrentState != RagdollState.Ragdoll) return;

        corpseState = CorpseState.PickupAnimating;

        ProtagonistScript.instance.corpseInHand = this;
        StudentAgent.enabled = false;

        StopAllCoroutines();
        StartCoroutine(CarryRoutine());
    }


    public void DropCorpse()
    {
        if (corpseState != CorpseState.Carried) return;
        if (StudentRagdoll.CurrentState != RagdollState.Carried) return;

        StopAllCoroutines();

        corpseState = CorpseState.Ragdoll;

        ProtagonistScript.instance.corpseInHand = null;

        ProtagonistScript.instance.ChangeAnimations("default", "default", "default");

        StudentRagdoll.DropFromCarrier(
            ProtagonistScript.instance.transform,
            ProtagonistScript.instance.GetComponent<Collider>()
        );

        InventoryScript.instance.enabled = true;
    }


    IEnumerator CarryRoutine()
    {
        InventoryScript.instance.CloseInventory();
        InventoryScript.instance.enabled = false;

        StudentPrompt.enabled = false;
        StudentPrompt.Nearby = false;
        StudentAnimation.Stop();

        ProtagonistScript.instance.CanMove = false;

        SetRagdollState(false);

        ProtagonistScript.instance.SetCorpseAnims(StudentData.StudentGender);
        ProtagonistScript.instance.animations.Play("f02_carryLiftA_00");

        string studentLift =
            StudentData.StudentGender == Gender.Female
            ? "f02_carryLiftB_00"
            : "carryLiftB_00";

        StudentAnimation.Play(studentLift);

        transform.position = ProtagonistScript.instance.transform.position;
        transform.eulerAngles = ProtagonistScript.instance.transform.eulerAngles;

        float animLength =
            ProtagonistScript.instance.animations.GetClip("f02_carryLiftA_00").length;

        yield return new WaitForSeconds(animLength);

        Transform carryBone =
            ProtagonistScript.instance.transform.Find("PelvisRoot/Hips/Spine");

        StudentRagdoll.ToCarried(carryBone);
        transform.localPosition = new Vector3(-0.084f, -0.959f, 0.236f);
        transform.localEulerAngles = Vector3.zero;

        corpseState = CorpseState.Carried;


        ProtagonistScript.instance.ChangeAnimations(
            "f02_carryIdleA_00",
            "f02_carryWalkA_00",
            "f02_carryRunA_00"
        );

        ProtagonistScript.instance.CanMove = true;

        StudentPrompt.enabled = true;
    }


    #endregion
}
