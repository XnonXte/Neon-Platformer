using System.Collections;
using UnityEngine;

public class CelesteSpring : MonoBehaviour
{
    public enum SpringDirection { Up, Down, Left, Right }

    [Header("Spring Settings")]
    [SerializeField] private SpringDirection launchDirection = SpringDirection.Up;
    [SerializeField] private float launchForce = 25f;

    [Header("Visuals (4-Frame Direct Swap)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D boxCollider;

    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite loadedSprite;
    [SerializeField] private Sprite launchSprite1;
    [SerializeField] private Sprite launchSprite2;

    [SerializeField] private float frameDelay = 0.05f;

    private Coroutine bounceRoutine;

    private void Awake()
    {
        // Auto-assign references if left empty
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        // Initialize visuals and match the collider to the idle frame
        SetSpriteAndMatchCollider(idleSprite);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // FIX 1: Search the object AND its parent (in case the player's collider is on a child object)
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null)
        {
            player = collision.GetComponentInParent<PlayerController>();
        }

        if (player != null)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                LaunchPlayer(player, playerRb);
            }
        }
    }

    private void LaunchPlayer(PlayerController player, Rigidbody2D playerRb)
    {
        Vector2 targetVelocity = Vector2.zero;
        bool isHorizontal = false;

        switch (launchDirection)
        {
            case SpringDirection.Up:
                targetVelocity = new Vector2(playerRb.linearVelocity.x, launchForce);
                break;

            case SpringDirection.Down:
                targetVelocity = new Vector2(playerRb.linearVelocity.x, -launchForce);
                break;

            case SpringDirection.Left:
                targetVelocity = new Vector2(-launchForce, 0f);
                isHorizontal = true;
                break;

            case SpringDirection.Right:
                targetVelocity = new Vector2(launchForce, 0f);
                isHorizontal = true;
                break;
        }

        player.SpringLaunch(targetVelocity, isHorizontal);

        if (spriteRenderer != null)
        {
            if (bounceRoutine != null) StopCoroutine(bounceRoutine);
            bounceRoutine = StartCoroutine(BounceSequence());
        }
    }

    private IEnumerator BounceSequence()
    {
        // Frame 2: Loaded / Squished
        SetSpriteAndMatchCollider(loadedSprite);
        yield return new WaitForSeconds(frameDelay);

        // Frame 3: Extended Launch 1
        SetSpriteAndMatchCollider(launchSprite1);
        yield return new WaitForSeconds(frameDelay);

        // Frame 4: Extended Launch 2
        SetSpriteAndMatchCollider(launchSprite2);
        yield return new WaitForSeconds(frameDelay);

        // Return to rest
        SetSpriteAndMatchCollider(idleSprite);
        bounceRoutine = null;
    }

    /// <summary> Swaps the active sprite frame and instantly reshapes the box collider to match its dimensions. </summary>
    private void SetSpriteAndMatchCollider(Sprite newSprite)
    {
        if (newSprite == null || spriteRenderer == null) return;

        spriteRenderer.sprite = newSprite;

        // Automatically alters collider shape to snap perfectly to the active pixel boundary
        if (boxCollider != null)
        {
            boxCollider.size = newSprite.bounds.size;
            boxCollider.offset = newSprite.bounds.center;
        }
    }

    private void OnValidate()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        // Corrected switch statement without the rogue typo
        switch (launchDirection)
        {
            case SpringDirection.Up: transform.rotation = Quaternion.Euler(0, 0, 0); break;
            case SpringDirection.Down: transform.rotation = Quaternion.Euler(0, 0, 180); break;
            case SpringDirection.Left: transform.rotation = Quaternion.Euler(0, 0, 90); break;
            case SpringDirection.Right: transform.rotation = Quaternion.Euler(0, 0, -90); break;
        }

        SetSpriteAndMatchCollider(idleSprite);
    }
}