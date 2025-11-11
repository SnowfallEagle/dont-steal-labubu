using MenteBacata.ScivoloCharacterControllerDemo;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using MirraGames.SDK;

// @NOTE: Should be called more like UIController
public class NavigationController : MonoBehaviour
{
    [SerializeField] GameObject ingameCanvas;
    [SerializeField] GameObject shopCanvas;
    [SerializeField] GameObject alertCanvas;
    [SerializeField] Camera mainCamera;
    [SerializeField] Camera shopCamera;
    [SerializeField] GameObject playerObject;
    [SerializeField] GameObject deathMenu;
    GameObject prevPageObject;
    [SerializeField] SpawnManager spawnManager;
    //[SerializeField] AdvManager advManager;
    [SerializeField] InputGame inputGame;
    [SerializeField] GameObject levelsNavAlert;
    [SerializeField] GameObject levelsNavPanel;

    bool isPause;
    public bool IsPaused => isPause;

    bool isShop;
    bool isGame;

    SoundController soundController;

    private void Awake()
    {
        soundController = FindFirstObjectByType<SoundController>();
    }
    void Start()
    {
        ingameCanvas.SetActive(false);
        EnableCharacterControl(isGame);

#if false
        alertCanvas.SetActive(false);
        shopCanvas.SetActive(isShop);
        deathMenu.SetActive(isGame);
        levelsNavAlert.SetActive(false);
        levelsNavPanel.SetActive(false);
        shopCamera.gameObject.SetActive(isShop);                 
#endif

        ShowGame();
    }

    // Update is called once per frame
    void Update()
    {
        // Kinda workaround for pause to prevent pause when showing ad notice
        if (MirraSDK.Time.Scale <= 0f)
        {
            return;
        }

        // Toggle pause menu
        /*
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape)) 
        {
            soundController.MakeClickSound();
            ShowPauseMenu();
        }
        */
    }

    public void ShowGame()
    {
        isGame = !isGame;

#if false
        if (isGame)
            spawnManager.RespawnPlayer();
#endif

        // inputGame.ShowCursorState(!isGame);
        EnableCharacterControl(isGame);
        isPause = false;

        ingameCanvas.SetActive(isGame);

#if false
        deathMenu.SetActive(false);
#endif
    }

    public void ShowPauseMenu()
    {
        isPause = !isPause;       

#if false
        if (!deathMenu.activeSelf)
        {
            EnableCharacterControl(!isPause);
            // inputGame.ShowCursorState(isPause);
        }    
#endif
    }

    public void EnableCharacterControl(bool state)
    {
        mainCamera.GetComponent<OrbitingCamera>().enabled = state;
        playerObject.GetComponent<SimpleCharacterController>().enabled = state;
    }
    public void ShowShopMenu() 
    {      
        isShop = !isShop;     
        shopCamera.gameObject.SetActive(isShop);
        shopCanvas.SetActive(isShop);
        prevPageObject.SetActive(!isShop);
    }

    public void SetPrevPage(GameObject objectToHide)
    {
        prevPageObject = objectToHide;
    }

    public void ShowLevelsNavHint(bool state)
    {
        levelsNavAlert.SetActive(state);
        EnableCharacterControl(!state);
        // inputGame.ShowCursorState(state);
    }
}
