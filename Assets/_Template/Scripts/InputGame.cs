using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using MenteBacata.ScivoloCharacterControllerDemo;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Cook;
using MirraGames.SDK;
using MirraGames.SDK.Fallback;

public class InputGame : Singleton<InputGame>
{
    private NavigationController m_UIController;
    private OrbitingCamera m_OrbitingCamera;

#if GAME_COOK
    private Vector3 m_LastCursorPos = Vector3.zero;
    private float m_LastFingerTimeStart = 0f;

    private int m_LastFingerId = FingerIdNone;
    private const int FingerIdNone = -1;

    [SerializeField] private float m_TimeToCountFingerTap = 0.1f;
#endif

    public bool Focused = true;
    public bool WantsCursorVisible = false;
    public bool CursorVisible => MirraSDK.Device.CursorLock != CursorLockMode.Locked;
    
    // Track cursor state for proper restoration after focus loss
    private bool m_IsInPause = false;
    private bool m_IsInShopMenu = false;
    private bool m_IsInTutorial = false;
    public bool ShouldShowCursor => m_IsInPause || m_IsInShopMenu || m_IsInTutorial;

    public void SetCursorVisible(bool visible)
    {
        WantsCursorVisible = visible;

        MirraSDK.Device.CursorVisible = visible;
        MirraSDK.Device.CursorLock = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
    
    public void SetPauseState(bool inPause)
    {
        m_IsInPause = inPause;
        UpdateCursorVisibility();
    }
    
    public void SetShopMenuState(bool inShopMenu)
    {
        m_IsInShopMenu = inShopMenu;
        UpdateCursorVisibility();
    }
    
    public void SetTutorialState(bool inTutorial)
    {
        m_IsInTutorial = inTutorial;
        UpdateCursorVisibility();
    }
    
    private void UpdateCursorVisibility()
    {
        // Only manage cursor visibility on desktop platforms
        if (!GameManager.Instance.Mobile)
        {
            SetCursorVisible(ShouldShowCursor);
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        Focused = focus;
        
        // Only manage cursor visibility on desktop platforms
        if (!GameManager.Instance.Mobile)
        {
            if (focus)
            {
                // Restore cursor state based on whether we're in menu/pause or gameplay
                SetCursorVisible(ShouldShowCursor);
            }
            else
            {
                // Hide cursor when losing focus
                SetCursorVisible(true);
            }
        }

    }

    private void Awake()
    {
        m_UIController = FindFirstObjectByType<NavigationController>(FindObjectsInactive.Exclude);
        m_OrbitingCamera = FindFirstObjectByType<OrbitingCamera>(FindObjectsInactive.Exclude);

        Assert.IsNotNull(m_UIController);
        Assert.IsNotNull(m_OrbitingCamera);
    }

    private void Update()
    {
        if (m_UIController.IsPaused)
        {
            return;
        }

#if GAME_COOK
        var Inventory = GameManager.Instance.Player.Inventory;

        CookManager.Instance.OnItemUnhovered();

        if (GameManager.Instance.Mobile)
        {
            SetCursorVisible(false);
        }
        else
        {
            // Keyboard
            System.Action<KeyCode> TryEquipInput = (Key) =>
            {
                if (Input.GetKeyDown(Key))
                {
                    int EquipIdx;

                    if (Key == KeyCode.Alpha0)
                    {
                        EquipIdx = 9;
                    }
                    else
                    {
                        EquipIdx = (int)Key - (int)KeyCode.Alpha1;
                    }

#if GAME_COOK
                    Inventory.Equip(EquipIdx);
#endif
                }
            };

            if (CookManager.Instance.InventoryWrapper.gameObject.activeSelf)
            {
                for (KeyCode Key = KeyCode.Alpha0; Key <= KeyCode.Alpha9; ++Key)
                {
                    TryEquipInput(Key);
                }
            }

            // Mouse
            if (MirraSDK.Device.CursorLock == CursorLockMode.Locked)
            {
                float X = Input.GetAxisRaw("Mouse X");
                float Y = Input.GetAxisRaw("Mouse Y");

                m_OrbitingCamera.UpdateMouse(new Vector2(X, Y));
            }

            if (!EventSystem.current || EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
        }

        // Look for interactable hovered objects
        bool MobileUseItem = false;
        if (GameManager.Instance.Mobile)
        {
            if (Input.touchCount > 0)
            {
                // Pick last touch and see if it's new or moved
                Touch Touch = Input.touches[Input.touchCount - 1];

                if (!EventSystem.current || EventSystem.current.IsPointerOverGameObject(Touch.fingerId))
                {
                    return;
                }

                if (m_LastFingerId != Touch.fingerId)
                {
                    m_LastFingerId = FingerIdNone;
                }

                if (Touch.phase == UnityEngine.TouchPhase.Began)
                {
                    m_LastFingerId = Touch.fingerId;
                    m_LastFingerTimeStart = Time.time;
                }

                if (Touch.phase == UnityEngine.TouchPhase.Began || Touch.phase == UnityEngine.TouchPhase.Moved)
                {
                    m_LastCursorPos = Touch.position;
                    return;
                }

                if (Touch.phase == UnityEngine.TouchPhase.Ended &&
                    m_LastFingerId == Touch.fingerId &&
                    Time.time - m_LastFingerTimeStart <= m_TimeToCountFingerTap)
                {
                    MobileUseItem = true;
                }
            }
        }
        else
        {
            m_LastCursorPos = Input.mousePosition;
        }

        m_LastCursorPos.z = Camera.main.nearClipPlane;

        Vector3 WorldMousePos = Camera.main.ScreenToWorldPoint(m_LastCursorPos);
        Vector3 Direction = (WorldMousePos - Camera.main.transform.position).normalized;

        CookPlatform BestToUpgrade = null;

        if (GameManager.Instance.Mobile)
        {
            // @SPEED: We can do it not every frame
            RaycastHit[] HitResults = Physics.RaycastAll(
                Camera.main.transform.position,
                Direction,
                GameManager.Instance.Player.InteractableRayDistance,
                1 << LayerMask.NameToLayer("Interactable"),
                QueryTriggerInteraction.Collide
            );

            // Find best hovered item and cook platform
            Item BestItemToHover = null;
            float BestItemToHoverDistance = float.MaxValue;

            bool EquippedItemIsChefOrUnpreparedFood =
                Inventory.HasEquippedItem &&
                (Inventory.Slots[Inventory.EquippedSlot].Item.Type == EItemType.Chef ||
                 (Inventory.Slots[Inventory.EquippedSlot].Item.Type == EItemType.Food &&
                  Inventory.Slots[Inventory.EquippedSlot].Item.Food.Type == EFoodType.Unprepared));

            float BestToUpgradeDistance = float.MaxValue;

            for (int i = 0; i < HitResults.Length; ++i)
            {
                ref RaycastHit Hit = ref HitResults[i];

                if (Hit.transform.CompareTag("upgrade-platform-brainrot") && Hit.transform.parent?.GetComponent<CookPlatform>() is var Platform && Platform != null)
                {
                    if (Hit.distance < BestToUpgradeDistance)
                    {
                        BestToUpgrade = Platform;
                        BestToUpgradeDistance = Hit.distance;
                    }
                    continue;
                }

                var HitItem = Hit.transform?.parent?.GetComponent<Item>();
                if (HitItem)
                {
                    if (HitItem.Platform != null && Hit.distance < BestItemToHoverDistance)
                    {
                        BestItemToHover = HitItem;
                        BestItemToHoverDistance = Hit.distance;
                    }

                    continue;
                }
            }

            CookManager.Instance.OnItemHovered(BestItemToHover, new Vector2(m_LastCursorPos.x, m_LastCursorPos.y));

            if (!MobileUseItem)
            {
                return;
            }
        }
        else
        {
            // Left Mouse Button Interaction
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }
        }

        if (Inventory.HasEquippedItem)
        {
            // Inventory.Slots[Inventory.EquippedSlot].Item.Use(HitResults);
        }
#endif
    }
}


