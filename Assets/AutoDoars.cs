using UnityEngine;

public class AutoDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public float interactDistance = 3f;
    public string openAnimation = "Door1_Open";
    public string closeAnimation = "Door1_Close";

    [Header("Floating E Prompt")]
    public Vector3 promptOffset = new Vector3(0, 2f, 0);
    public float floatSpeed = 1f;

    private Animator animator;
    private Transform player;
    private TextMesh floatingText;
    private bool isOpen = false;

    void Start()
    {
        // Add animator if missing
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();

        // Find player
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Create floating E text
        GameObject textObj = new GameObject("DoorPrompt");
        floatingText = textObj.AddComponent<TextMesh>();
        floatingText.text = "E";
        floatingText.fontSize = 80;
        floatingText.color = Color.white;
        floatingText.alignment = TextAlignment.Center;

        // Hide at start
        floatingText.gameObject.SetActive(false);
    }

    void Update()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        // Show prompt if close
        if (dist <= interactDistance)
        {
            floatingText.gameObject.SetActive(true);

            // Position + floating animation
            floatingText.transform.position = transform.position + promptOffset +
                new Vector3(0, Mathf.Sin(Time.time * floatSpeed) * 0.2f, 0);

            floatingText.transform.rotation = Camera.main.transform.rotation;

            // Interaction
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!isOpen)
                {
                    animator.Play(openAnimation);
                    isOpen = true;
                }
                else
                {
                    animator.Play(closeAnimation);
                    isOpen = false;
                }
            }
        }
        else
        {
            floatingText.gameObject.SetActive(false);
        }
    }
}
