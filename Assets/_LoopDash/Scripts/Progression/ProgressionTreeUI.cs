using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ProgressionTreeUI : MonoBehaviour
{
    private static readonly Color LockedLinkColor = new Color(0.39f, 0.58f, 0.73f);
    private static readonly Color MetLinkColor = new Color(0.28f, 0.92f, 0.85f);

    [SerializeField] private ProgressionGraphScrollRect scrollRect;
    [SerializeField] private UpgradeNodeUI nodePrefab;
    [SerializeField] private UnityEngine.UI.Button closeButton;
    [SerializeField] private TMPro.TextMeshProUGUI balanceLabel;
    [SerializeField, Min(0f)] private float padding = 32f;
    [SerializeField, Min(0f)] private float siblingSpacing = 64f;
    [SerializeField, Min(0f)] private float tierSpacing = 80f;

    private readonly Dictionary<UpgradeDefinition, UpgradeNodeUI> nodes =
        new Dictionary<UpgradeDefinition, UpgradeNodeUI>();
    private readonly List<DependencyLink> links = new List<DependencyLink>();
    private ProgressionManager manager;
    private bool framed;

    public event Action Closed;

    private void Awake()
    {
        manager = ProgressionManager.Instance;
        BuildTree();
    }

    private void OnEnable()
    {
        manager.ProgressionChanged += Refresh;
        CoinStorage.CoinsChanged += OnCoinsChanged;
        closeButton.onClick.AddListener(Close);
        if (!framed)
        {
            Canvas.ForceUpdateCanvases();
            framed = scrollRect.FrameGraph();
        }

        Refresh();
        closeButton.Select();
    }

    private void OnDisable()
    {
        manager.ProgressionChanged -= Refresh;
        CoinStorage.CoinsChanged -= OnCoinsChanged;
        closeButton.onClick.RemoveListener(Close);
    }

    private void Close()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    private void BuildTree()
    {
        IReadOnlyList<UpgradeDefinition> upgrades = manager.Tree.Upgrades;
        RectTransform content = scrollRect.content;
        Vector2 nodeSize = ((RectTransform)nodePrefab.transform).sizeDelta;
        var layout = new ProgressionGraphLayout(upgrades, nodeSize, padding, siblingSpacing, tierSpacing);
        content.sizeDelta = layout.Size;

        const float linkThickness = 4f;
        foreach (UpgradeDefinition upgrade in upgrades)
        {
            foreach (UpgradeDefinition.Requirement requirement in upgrade.Requirements)
            {
                layout.GetLinkEndpoints(requirement.Upgrade, upgrade, out Vector2 start, out Vector2 end);
                UnityEngine.UI.Image link = AddLine(content, "Dependency Link", start, end, linkThickness);
                links.Add(new DependencyLink(requirement, link));
            }
        }

        var centerOffset = new Vector2(nodeSize.x * 0.5f, -nodeSize.y * 0.5f);
        foreach (UpgradeDefinition upgrade in upgrades)
        {
            UpgradeNodeUI node = Instantiate(nodePrefab, content);
            ((RectTransform)node.transform).anchoredPosition = layout[upgrade] - centerOffset;
            node.Bind(upgrade, manager);
            nodes.Add(upgrade, node);
        }
    }

    private static UnityEngine.UI.Image AddLine(RectTransform content, string name, Vector2 start, Vector2 end, float thickness)
    {
        var line = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var lineRect = (RectTransform)line.transform;
        lineRect.SetParent(content, false);
        lineRect.anchorMin = Vector2.up;
        lineRect.anchorMax = Vector2.up;
        lineRect.anchoredPosition = (start + end) * 0.5f;
        Vector2 difference = end - start;
        lineRect.sizeDelta = new Vector2(difference.magnitude, thickness);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg);

        var graphic = line.GetComponent<UnityEngine.UI.Image>();
        graphic.raycastTarget = false;
        return graphic;
    }

    private void OnCoinsChanged(float balance)
    {
        Refresh();
    }

    private void Refresh()
    {
        balanceLabel.text = $"Coins {CoinStorage.GetCoins():0.##}";
        foreach (UpgradeNodeUI node in nodes.Values)
            node.Refresh();

        foreach (DependencyLink link in links)
        {
            bool requirementMet = manager.GetLevel(link.Requirement.Upgrade) >= link.Requirement.Level;
            link.Graphic.color = requirementMet ? MetLinkColor : LockedLinkColor;
        }
    }

    private readonly struct DependencyLink
    {
        public readonly UpgradeDefinition.Requirement Requirement;
        public readonly UnityEngine.UI.Image Graphic;

        public DependencyLink(UpgradeDefinition.Requirement requirement, UnityEngine.UI.Image graphic)
        {
            Requirement = requirement;
            Graphic = graphic;
        }
    }
}
