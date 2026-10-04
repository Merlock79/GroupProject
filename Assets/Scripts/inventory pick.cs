using UnityEngine;
using UnityEngine.UI;

public class ItemPickupInventory : MonoBehaviour
{
    [Header("Inventory UI Text (shows item names)")]
    public Text inventoryText;

    [Header("Max 6 inventory slots")]
    public string[] inventory = new string[6];

    private int selectedSlot = 0;
    private Transform player;

    [Header("Pickup Settings")]
    public float pickupRange = 3f;

    void Start()
    {
        player = transform;
        UpdateInventoryUI();
    }

    void Update()
    {
        HandleSlotSelection();
        HandlePickup();
        HandleDrop();
    }

    void HandleSlotSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedSlot = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedSlot = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedSlot = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedSlot = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) selectedSlot = 4;
        if (Input.GetKeyDown(KeyCode.Alpha6)) selectedSlot = 5;
    }

    void HandlePickup()
    {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.C))
        {
            RaycastHit hit;
            if (Physics.Raycast(player.position, player.forward, out hit, pickupRange))
            {
                if (hit.collider.CompareTag("Pick"))
                {
                    AddItem(hit.collider.gameObject.name);
                    hit.collider.gameObject.SetActive(false); // hide item
                }
            }
        }
    }

    void HandleDrop()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (!string.IsNullOrEmpty(inventory[selectedSlot]))
            {
                Debug.Log("Dropped: " + inventory[selectedSlot]);
                inventory[selectedSlot] = "";
                UpdateInventoryUI();
            }
        }
    }

    void AddItem(string itemName)
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (string.IsNullOrEmpty(inventory[i]))
            {
                inventory[i] = itemName;
                UpdateInventoryUI();
                return;
            }
        }

        Debug.Log("Inventory full!");
    }

    void UpdateInventoryUI()
    {
        inventoryText.text = "";

        for (int i = 0; i < inventory.Length; i++)
        {
            string slotText = string.IsNullOrEmpty(inventory[i]) ? "[Empty]" : inventory[i];
            inventoryText.text += (i + 1) + ": " + slotText + "\n";
        }
    }
}
