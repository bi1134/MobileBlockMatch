using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask groundLayer;

    private Vector3 lastTouchPosition;

    public Vector3 GetSelectedMapPosition()
    {
        Vector2 screenPos;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Mouse.current == null) return lastTouchPosition;
        screenPos = Mouse.current.position.ReadValue();
#elif UNITY_ANDROID || UNITY_IOS
        if (Touchscreen.current == null || Touchscreen.current.primaryTouch.press.isPressed == false)
            return lastTouchPosition;
        screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
#else
        return lastTouchPosition;
#endif

        Ray ray = sceneCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            lastTouchPosition = hit.point;
        }

        return lastTouchPosition;
    }

    public bool IsTouchPressed()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Mouse.current?.leftButton.isPressed ?? false;
#elif UNITY_ANDROID || UNITY_IOS
        return Touchscreen.current?.primaryTouch.press.isPressed ?? false;
#else
        return false;
#endif
    }

    public bool WasTouchPressedThisFrame()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Mouse.current?.leftButton.wasPressedThisFrame ?? false;
#elif UNITY_ANDROID || UNITY_IOS
        return Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false;
#else
        return false;
#endif
    }

    public bool WasTouchReleasedThisFrame()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Mouse.current?.leftButton.wasReleasedThisFrame ?? false;
#elif UNITY_ANDROID || UNITY_IOS
        return Touchscreen.current?.primaryTouch.press.wasReleasedThisFrame ?? false;
#else
        return false;
#endif
    }   
}
