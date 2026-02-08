using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;
using TMPro;
using System;

[System.Serializable]
public class InventorySlot
{
    public ItemScript SlotItem;
    public Transform SlotUI;
    //notice for ren - adding unityengine.ui  to make sure it doesnt overlap with the highlight system's outline script (Outline.cs)
    public UnityEngine.UI.Outline SlotOutline;
    public Image SlotBackground;
    public Image SlotSprite;
    public TMP_Text SlotLabel;
}

public class InventoryScript : MonoBehaviour
{
    [Header("Assign in inspector")]
    public Transform InventoryUI;
    public float keyHoldTime = 3; //if player holds Alpha 1/2/3 keys for X time (seconds), the item will be dropped.
    public float inventoryCloseTime = 3; //the inventory will close by itself in X (seconds) if no key (Alpha 1 2 3) is pressed or no item is dropped..
    public List<InventorySlot> InventorySlots = new List<InventorySlot>();
    public Color SelectedColor;
    public Color DefaultColor;
    [Space]
    public ItemScript CurrentItem;
    [Space]
    public int CurrentSlotNum;
    public bool InventoryOpen;
    float[] keyHoldTimers = new float[3];
    float inventoryIdleTimer;

    public static InventoryScript instance;


    void Start()
    {
        DeselectAllSlots();
        CloseInventory();
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (!ProtagonistScript.instance.BookBag.hasBookbag) { return; }

        CurrentSlotNum = Mathf.Clamp(CurrentSlotNum, 0, 2);

        if (Input.GetKeyDown(KeyCode.Alpha1)) { OpenInventory(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { OpenInventory(1); } 
            if (Input.GetKeyDown(KeyCode.Alpha3)) { OpenInventory(2); }

        if (!InventoryOpen) return;

        bool keyPressedThisFrame = false;

        KeyHold(KeyCode.Alpha1, 0, ref keyPressedThisFrame);
        KeyHold(KeyCode.Alpha2, 1, ref keyPressedThisFrame);
        KeyHold(KeyCode.Alpha3, 2, ref keyPressedThisFrame);

        if (keyPressedThisFrame)
        {
            inventoryIdleTimer = 0f;
        }
        else
        {
            inventoryIdleTimer += Time.deltaTime;
            if (inventoryIdleTimer >= inventoryCloseTime)
            {
                CloseInventory();
                inventoryIdleTimer = 0f;
            }
        }
    }

    void KeyHold(KeyCode key, int slotIndex, ref bool keyPressedThisFrame)
    { 
        if (Input.GetKey(key))
        {
            keyPressedThisFrame = true;
            keyHoldTimers[slotIndex] += Time.deltaTime;

            if (keyHoldTimers[slotIndex] >= keyHoldTime)
            {
                if(InventorySlots[slotIndex].SlotItem != null)
                {
                    InventorySlots[slotIndex].SlotItem.Drop();
                    ClearSlot(slotIndex);
                }
                keyHoldTimers[slotIndex] = 0f;
            }
        }

        if (Input.GetKeyUp(key))
        {
            keyHoldTimers[slotIndex] = 0f;
        }
    }



    public bool isInventoryFull()
    {
        foreach(InventorySlot slot in InventorySlots)
        {
            if(slot.SlotItem == null)
            {
                return false;
            }
        }
        return true;
    }

    public void OpenInventory(int startSlot)
    {
        inventoryIdleTimer = 0f;
        Array.Clear(keyHoldTimers, 0, keyHoldTimers.Length);

        InventoryOpen = true;
        InventoryUI.gameObject.SetActive(true);
        DOTween.Kill(InventoryUI);
        InventoryUI.DOLocalMove(new Vector3(850, 0, 0), 0.5f);
        SelectSlot(startSlot);
    }

    public void CloseInventory()
    {
        InventoryOpen = false;
        DOTween.Kill(InventoryUI);
        InventoryUI.DOLocalMove(new Vector3(1200,0,0), 0.5f).OnComplete(() => InventoryUI.gameObject.SetActive(false));
    }

    public void DeselectAllSlots()
    {
        foreach(InventorySlot slot in InventorySlots)
        {
            DOTween.Kill(slot);
            slot.SlotLabel.text = "";
            slot.SlotBackground.transform.DOScale(new Vector3(0.7f, 0.7f, 0.7f), 0.3f);
            slot.SlotOutline.effectColor = SelectedColor;
            slot.SlotBackground.color = DefaultColor;
            if(slot.SlotSprite.sprite != null)
            {
                slot.SlotSprite.color = SelectedColor;
            }
            slot.SlotSprite.GetComponent<RectTransform>().sizeDelta = new Vector2(150,150);
            slot.SlotSprite.transform.localPosition = new Vector2(slot.SlotSprite.transform.localPosition.x, 0);
            if (slot.SlotItem != null)
            {
                slot.SlotItem.Equip(false);
            }
        }
    }

    public void SelectSlot(int selectedSlot)
    {
        DeselectAllSlots();
        InventorySlots[selectedSlot].SlotUI.DOScale(new Vector3(0.9f,0.9f,0.9f), 0.3f);
        InventorySlots[selectedSlot].SlotOutline.effectColor = DefaultColor;
        InventorySlots[selectedSlot].SlotBackground.color = SelectedColor;
        if(InventorySlots[selectedSlot].SlotSprite.sprite != null)
        {
            InventorySlots[selectedSlot].SlotSprite.color = DefaultColor;
        }
        InventorySlots[selectedSlot].SlotSprite.GetComponent<RectTransform>().sizeDelta = new Vector2(112, 112);
        InventorySlots[selectedSlot].SlotSprite.transform.localPosition = new Vector2(InventorySlots[selectedSlot].SlotSprite.transform.localPosition.x, 12);
        if(InventorySlots[selectedSlot].SlotItem != null)
        {
            InventorySlots[selectedSlot].SlotLabel.text = InventorySlots[selectedSlot].SlotItem.gameObject.name;
            InventorySlots[selectedSlot].SlotItem.Equip();
        }
        else
        {
            InventorySlots[selectedSlot].SlotLabel.text = "Empty";
        }
    }

    public void Add(ItemScript item)
    {
        foreach(InventorySlot slot in InventorySlots)
        {
            if(slot.SlotItem == null)
            {
                slot.SlotItem = item;
                slot.SlotSprite.sprite = item.ItemData.itemSprite;
                slot.SlotSprite.color = new Color(slot.SlotSprite.color.r, slot.SlotSprite.color.g, slot.SlotSprite.color.b, 0f);
               /* if(InventorySlots[CurrentSlotNum] == slot)
                {
                    SelectSlot(CurrentSlotNum);
                }*/
                break;
            }
        }
    }

    public void Remove(ItemScript item)
    {
    foreach(InventorySlot slot in InventorySlots)
        {
            if(slot.SlotItem == item)
            {
                ClearSlot(InventorySlots.IndexOf(slot));
                break;
            }
        }    
    }

    public void ClearSlot(int slot)
    {
        InventorySlots[slot].SlotItem = null;
        InventorySlots[slot].SlotSprite.sprite = null;
        InventorySlots[slot].SlotSprite.color = new Color(InventorySlots[slot].SlotSprite.color.r, InventorySlots[slot].SlotSprite.color.g, InventorySlots[slot].SlotSprite.color.b, 0f);
        InventorySlots[slot].SlotLabel.text = "";
    }

    public int FindSlotNum(ItemScript item)
    {
        foreach(InventorySlot slot in InventorySlots)
        {
            if(item == slot.SlotItem)
            {
                return InventorySlots.IndexOf(slot);
            }
        }
        return -1;
    }
}
