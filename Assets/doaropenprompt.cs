using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Assign Animator & Clips")]
    public Animator animator;
    public AnimationClip openClip;
    public AnimationClip closeClip;

    [Header("Interaction Settings")]
    public float interactDistance = 3f;

    private Transform player;
    private bool isOpen = false;
    private bool isAnimating = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        // Only allow interaction if close enough AND not currently animating
        if (dist <= interactDistance && !isAnimating)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!isOpen)
                {
                    PlayAnimation(openClip);
                    isOpen = true;
                }
                else
                {
                    PlayAnimation(closeClip);
                    isOpen = false;
                }
            }
        }
    }

    void PlayAnimation(AnimationClip clip)
    {
        animator.Play(clip.name);
        isAnimating = true;
        Invoke(nameof(EndAnimation), clip.length);
    }

    void EndAnimation()
    {
        isAnimating = false;
    }
}
