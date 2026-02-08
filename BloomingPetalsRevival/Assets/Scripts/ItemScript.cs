using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemData
{
    [Header("Runtime values")]
    public Vector3 defaultPosition;
    public Vector3 defaultRotation;
    [Space]
    [Header("Assign in inspector")]
    public Vector3 positionInHand;
    public Vector3 rotationInHand;
    [Space]
    public bool canConceal;
    public bool isWeapon;
    public bool isBloody;
    public bool hidePromptOnPickup = true;
    [Space]
    public AudioClip itemSound;
    [Space]
    public Sprite itemSprite;
}

public class ItemScript : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public ItemData ItemData = new ItemData();

    [Space]
    [Header("Runtime")]
    public Rigidbody ItemRigidbody;
    public PromptScript ItemPrompt;
    public Collider ItemCollider;

    private LayerMask GroundLayer;
    private bool hasLanded;

    private bool isDropping;
    private bool isGrounded;

    private void Start()
    {
        //so we don't have to do this manually and change it everytime the item's position changes in the scene..
        ItemData.defaultPosition = transform.position;
        ItemData.defaultRotation = transform.eulerAngles;

        ItemRigidbody = GetComponent<Rigidbody>();
        ItemPrompt = GetComponent<PromptScript>();
        ItemCollider = GetComponent<Collider>();

        GroundLayer = GameGlobals.instance.GroundLayer;
    }

    public void Equip(bool shouldEquip = true)
    {
        gameObject.SetActive(shouldEquip);
        if (!shouldEquip)
        {
            if(InventoryScript.instance.CurrentItem == this)
            {
                InventoryScript.instance.CurrentItem = null;
            }
            return;
        }
        InventoryScript.instance.CurrentItem = this;
        AudioSource.PlayClipAtPoint(ItemData.itemSound, ProtagonistScript.instance.transform.position);
        if (ItemData.isWeapon)
        {
            NotificationScript.instance.ShowNotification("Visibly Armed");
        }
    }

    public void PickUp()
    {
        if (InventoryScript.instance.isInventoryFull())
        {
            NotificationScript.instance.ShowNotification("Your bookbag is full!");
            return;
        }
        InventoryScript.instance.Add(this);
        ItemRigidbody.isKinematic = true;
        ItemRigidbody.detectCollisions = false;
        ItemRigidbody.interpolation = RigidbodyInterpolation.None;
        ItemCollider.enabled = false;
        isDropping = false;
        isGrounded = false;
        transform.parent = ProtagonistScript.instance.Hand;
        transform.localPosition = ItemData.positionInHand;
        transform.localEulerAngles = ItemData.rotationInHand;
        if (ItemData.hidePromptOnPickup)
        {
            ItemPrompt.enabled = false;
        }
        InventoryScript.instance.SelectSlot(InventoryScript.instance.FindSlotNum(this));
    }

    public void Drop()
    {
        if (InventoryScript.instance.CurrentItem == this)
            InventoryScript.instance.CurrentItem = null;

        transform.parent = null;

        ItemRigidbody.isKinematic = false;
        ItemRigidbody.useGravity = true;
        ItemRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        ItemRigidbody.detectCollisions = true;

        ItemCollider.enabled = true;

        if (Vector3.Distance(transform.position, ItemData.defaultPosition) < 1.5f)
        {
            transform.position = ItemData.defaultPosition;
            transform.eulerAngles = ItemData.defaultRotation;

            LandItem();
            return;
        }

        isDropping = true;
        hasLanded = false;
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (!isDropping || hasLanded) return;

        if (((1 << collision.gameObject.layer) & GroundLayer) != 0)
        {
            LandItem();
        }
    }


    private void LandItem()
    {
        hasLanded = true;
        isDropping = false;

        ItemRigidbody.velocity = Vector3.zero;
        ItemRigidbody.angularVelocity = Vector3.zero;

        ItemRigidbody.isKinematic = true;
        ItemRigidbody.useGravity = false;

        transform.eulerAngles = ItemData.defaultRotation;

        ItemPrompt.enabled = true;
    }
}
