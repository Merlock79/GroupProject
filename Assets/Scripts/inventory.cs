using UnityEngine;
using UnityEngine.UI;

public class UIPanelManager : MonoBehaviour
{
    [Header("Assign 1–3 Panels")]
    public GameObject[] panels = new GameObject[3];

    [Header("Assign 1–6 Buttons")]
    public Button[] buttons = new Button[6];

    [Header("Panel ID for each button (0–2)")]
    public int[] buttonPanelID = new int[6];

    void Start()
    {
        CloseAllPanels();

        // Hook up buttons
        for (int i = 0; i < buttons.Length; i++)
        {
            int id = i;
            if (buttons[i] != null)
                buttons[i].onClick.AddListener(() => OpenPanel(buttonPanelID[id]));
        }
    }

    public void OpenPanel(int id)
    {
        CloseAllPanels();

        if (id >= 0 && id < panels.Length && panels[id] != null)
            panels[id].SetActive(true);
    }

    public void CloseAllPanels()
    {
        foreach (var p in panels)
            if (p != null) p.SetActive(false);
    }
}
