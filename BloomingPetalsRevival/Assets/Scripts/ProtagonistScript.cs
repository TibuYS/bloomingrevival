using System;
using UnityEngine;
using System.Collections;
//using DG.Tweening;
using System.Collections.Generic;

public class ProtagonistScript : MonoBehaviour
{
    public Transform playerTransform;
    public bool CanMove = true;
    public Transform pelvisRoot;

    public bool Running;

    public Transform CameraPivot;

    public Camera mainCamera;

    public Meshes[] meshes;

    [Space]
    public float WalkSpeed = 1f;

    public float RunSpeed = 5f;

    [Space]
    public string IdleAnimation = "f02_idleShort_00";
    public string WalkAnimation = "f02_newWalk_00";
    public string SprintAnimation = "f02_newSprint_00";

    [Space]

    private CharacterController controller;

    public Animation animations;

    private Vector3 targetDirection;

    private Quaternion targetRotation;

    public static ProtagonistScript instance;

    public Transform Hand;
    public Transform Hips;
    public Transform Head;
    public Transform dragPoint;

    public string CurrentOutfit;

    public Transform followSpot;

    public StudentScript corpseInHand;
    public string corpseIdle;
    public string corpseWalk;
    public string corpseSprint;

    public bool isKilling;

    public bool isBloody;
    public Projector bloodProjector;

    public BookBagScript BookBag;

    [HideInInspector]public string DefaultIdleAnimation = "f02_cuteIdle_00";
    [HideInInspector] public string DefaultWalkAnimation = "f02_ryobaWalk_00";
    [HideInInspector] public string DefaultSprintAnimation = "f02_ryobaRun_00";

    [Space]

    public bool isTripping;
    public AnimationClip trippingAnimation;

    public Transform BucketPivot;

    private void Awake()
    {
        instance = this;
    }

    public void ToggleBlood(bool bool_)
    {
        if (isBloody == bool_) return;
        isBloody = bool_;
        bloodProjector.enabled = bool_;
        if (isBloody)
        {
            NotificationScript.instance.ShowNotification("Visibly Bloody");
        }
    }

    public string GetAttackAnimation(ItemScript item)
    {
        int san = SanityScript.instance.CurrentSanity / 20;

        if (item.gameObject.name == "Knife")
        {
            if (san >= 4) return "f02_knifeHighSanityA_00";
            if (san >= 2) return "f02_knifeMedSanityA_00";
            return "f02_knifeLowSanityA_00";
        }

        return "";
    }

    public void SetCorpseAnims(Gender gender)
    {
        switch (gender)
        {
            case Gender.Female:
                corpseIdle = "f02_carryIdleB_00";
                corpseWalk = "f02_carryWalkB_00";
                corpseSprint = "f02_carryRunB_00";
                break;

            case Gender.Male:
                corpseIdle = "carryIdleB_00";
                corpseWalk = "carryWalkB_00";
                corpseSprint = "carryRunB_00";
                break;
        }
    }
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        DefaultIdleAnimation = "f02_cuteIdle_00";
        DefaultWalkAnimation = "f02_ryobaWalk_00";
        DefaultSprintAnimation = "f02_ryobaRun_00";

        controller = GetComponent<CharacterController>();
        animations = GetComponent<Animation>();

        Hand = transform.Find("PelvisRoot/Hips/Spine/Spine1/Spine2/Spine3/RightShoulder/RightArm/RightArmRoll/RightForeArm/RightForeArmRoll/RightHand");
        dragPoint = transform.Find("PelvisRoot/Hips/Spine/Spine1/Spine2/Spine3/RightShoulder/RightArm/RightArmRoll/RightForeArm/RightForeArmRoll/RightHand/dragPoint");
        BookBag = transform.Find("PelvisRoot/Hips/Spine/Spine1/Spine2/Spine3/RightShoulder/BookBag").gameObject.GetComponent<BookBagScript>();
    }

    public void ToggleIdle(bool bl)
    {
        CanMove = !bl;
        animations.Play(IdleAnimation);
    }

    public void ChangeAnimations(string newIdle, string newWalk, string newSprint)
    {
        if(newIdle != "default")
        {
            IdleAnimation = newIdle;
        }
        else
        {
            IdleAnimation = DefaultIdleAnimation;
        }

        if (newWalk != "default")
        {
            WalkAnimation = newWalk;
        }
        else
        {
            WalkAnimation = DefaultWalkAnimation;
        }

        if (newSprint != "default")
        {
            SprintAnimation = newSprint;
        }
        else
        {
            SprintAnimation = DefaultSprintAnimation;
        }
    }

    public void Update()
    {
        Vector3 pivotPos = CameraPivot.localPosition;
        CameraPivot.localPosition = pivotPos;

        Physics.IgnoreLayerCollision(9, 10);

        if (base.transform.position.y < -5f)
        {
            base.transform.position = Vector3.zero;
        }


        if (CanMove && !isKilling)
        {

            Running = Input.GetKey(KeyCode.LeftShift);


            controller.Move(Physics.gravity * 0.1f);

            float axisRaw = Input.GetAxisRaw("Vertical");
            float axisRaw2 = Input.GetAxisRaw("Horizontal");

            if (mainCamera.orthographic)
            {
                targetDirection = new Vector3(axisRaw2, 0f, axisRaw);
            }
            else
            {
                Vector3 vector = mainCamera.transform.TransformDirection(Vector3.forward);
                vector.y = 0f;
                vector = vector.normalized;
                Vector3 vector2 = new Vector3(vector.z, 0f, 0f - vector.x);
                targetDirection = axisRaw2 * vector2 + axisRaw * vector;
            }

            if (targetDirection != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(targetDirection);
                base.transform.rotation = Quaternion.Lerp(base.transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            else
            {
                targetRotation = new Quaternion(0f, 0f, 0f, 0f);
            }

            if (axisRaw != 0f || axisRaw2 != 0f)
            {
                animations.CrossFade((!Running) ? WalkAnimation : SprintAnimation);

                if (corpseInHand != null)
                {
                   // corpseInHand.studentAnimation.CrossFade((!Running) ? corpseWalk : corpseSprint);
                }

                controller.Move(base.transform.forward * Time.deltaTime * ((!Running) ? WalkSpeed : RunSpeed));
            }
            else
            {
                animations.CrossFade(IdleAnimation);

                if (corpseInHand != null)
                {
                  //  corpseInHand.studentAnimation.CrossFade(corpseIdle);
                }
            }
        }
    }

    public void PlayAnimation(string animationClip)
    {
        animations.Play(animationClip);
    }

    public void UpdatePosition(Vector3 position, Quaternion rotation)
    {
        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;
    }
    public void ChangeClothes(string currentOutfit)
    {
        CurrentOutfit = currentOutfit;
        SkinnedMeshRenderer outfitMesh = GameObject.Find("Outfitmesh_geo1").GetComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer bodyMesh = GameObject.Find("AK_BodyMesh:geo_2").GetComponent<SkinnedMeshRenderer>();

        switch (currentOutfit)
        {

            case "Uniform":
                Meshes uniformMesh = Array.Find(meshes, m => m.outfitName == currentOutfit.ToString());
                outfitMesh.sharedMesh = uniformMesh.outfitMesh;
                bodyMesh.sharedMesh = uniformMesh.bodyMesh;
                outfitMesh.material.mainTexture = uniformMesh.Tex;
                bodyMesh.material.mainTexture = uniformMesh.Tex1;
                break;

            case "PJs":
                Meshes PJsMesh = Array.Find(meshes, m => m.outfitName == currentOutfit.ToString());
                outfitMesh.sharedMesh = PJsMesh.outfitMesh;
                bodyMesh.sharedMesh = PJsMesh.bodyMesh;
                outfitMesh.material.mainTexture = PJsMesh.Tex;
                bodyMesh.material.mainTexture = PJsMesh.Tex1;
                break;
        }
    }

}

[System.Serializable]
public class Meshes
{
    public string outfitName;
    public Mesh outfitMesh;
    public Mesh bodyMesh;
    public Texture2D Tex;
    public Texture2D Tex1;

}