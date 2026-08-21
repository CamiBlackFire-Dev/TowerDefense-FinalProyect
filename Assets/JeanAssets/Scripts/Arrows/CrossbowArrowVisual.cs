using UnityEngine;

public class CrossbowArrowVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Arrow Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite fireSprite;
    [SerializeField] private Sprite iceSprite;
    [SerializeField] private Sprite poisonSprite;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        transform.forward = mainCamera.transform.forward;
    }

    public void UpdateVisual(ArrowData arrowData)
    {
        if (arrowData == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = true;

        switch (arrowData.effectType)
        {
            case ArrowEffectType.Normal:
                spriteRenderer.sprite = normalSprite;
                break;

            case ArrowEffectType.Fire:
                spriteRenderer.sprite = fireSprite;
                break;

            case ArrowEffectType.Ice:
                spriteRenderer.sprite = iceSprite;
                break;

            case ArrowEffectType.Poison:
                spriteRenderer.sprite = poisonSprite;
                break;
        }
    }
}