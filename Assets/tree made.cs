using UnityEngine;

[ExecuteInEditMode]
public class EditorTreeGenerator : MonoBehaviour
{
    public Material barkMaterial;
    public Material leafMaterial;

    public float trunkHeight = 4f;
    public float trunkRadius = 0.3f;

    public int branchCount = 6;
    public float branchLength = 1.5f;

    private bool generated = false;

    void Update()
    {
        if (!Application.isPlaying && !generated)
        {
            GenerateTree();
            generated = true;
        }
    }

    void GenerateTree()
    {
        GameObject tree = new GameObject("EditorTree");
        tree.transform.position = transform.position;

        // Trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localScale = new Vector3(trunkRadius, trunkHeight / 2f, trunkRadius);
        trunk.transform.localPosition = new Vector3(0, trunkHeight / 2f, 0);
        trunk.GetComponent<MeshRenderer>().material = barkMaterial;

        // Branches + Leaves
        for (int i = 0; i < branchCount; i++)
        {
            float angle = (360f / branchCount) * i;
            Vector3 direction = Quaternion.Euler(0, angle, 30) * Vector3.up;

            GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            branch.transform.SetParent(tree.transform);
            branch.transform.localScale = new Vector3(0.1f, branchLength / 2f, 0.1f);
            branch.transform.localPosition = new Vector3(0, trunkHeight * 0.75f, 0);
            branch.transform.rotation = Quaternion.LookRotation(direction);
            branch.GetComponent<MeshRenderer>().material = barkMaterial;

            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.SetParent(tree.transform);
            leaves.transform.localScale = Vector3.one * 1.2f;
            leaves.transform.position = branch.transform.position + direction * branchLength;
            leaves.GetComponent<MeshRenderer>().material = leafMaterial;
        }
    }
}
