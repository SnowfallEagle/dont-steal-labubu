using MenteBacata.ScivoloCharacterControllerDemo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] List<SpawnPoint> spawnPointsList;
    //int lastSpawnPoint;
    [SerializeField] GameObject playerObject;
    [SerializeField] GameObject deathMenu;
    [SerializeField] GameObject cameraObject;
    ///[SerializeField] AdvManager advManager;
    [SerializeField] InputGame inputGame;
    [SerializeField] NavigationController navigationController;
    [SerializeField] GameObject deathAlert;
    Animator deathAlertanimator;
    
#if false
    private void Start()
    {
        int Point = YandexGame.savesData.lastSpawnPoint;
        if (Point >= 0 && Point < spawnPointsList.Count && spawnPointsList[Point] != null)
        {
            playerObject.transform.position = spawnPointsList[Point].spawnCoordinates.position;
        }
        //YandexGame.savesData.lastSpawnPoint = Interop.save.spawnPointNumber;
        
        for(int i = 0; i < spawnPointsList.Count; i++)
        {
            spawnPointsList[i]?.AlreadySet(Interop.save.areSpawnpointsSet[i]);
        }

        deathAlertanimator = deathAlert.GetComponent<Animator>();
    }

    public void UpdatePointNumber(SpawnPoint point)
    {
        YandexGame.savesData.lastSpawnPoint = spawnPointsList.IndexOf(point);
        if(YandexGame.savesData.lastSpawnPoint == spawnPointsList.Count - 1) 
        {
            levelsNavigation.SetActiveState(true);
            navigationController.ShowLevelsNavHint(true);
        }
        Interop.save.spawnPointNumber = YandexGame.savesData.lastSpawnPoint;
        Interop.save.PushSave();
    }

    public void UpdatePointNumber(int pointNumber)
    {
        //lastSpawnPoint = pointNumber;
        // YandexGame.savesData.lastSpawnPoint = pointNumber;
        // YandexGame.SaveProgress();
    }
 
    public void RespawnPlayer()
    {
        ad.FullScreen_AD();
        deathMenu.SetActive(false);
        deathAlert.SetActive(false);

        playerObject.SetActive(true);

        int Point = YandexGame.savesData.lastSpawnPoint;
        if (Point >= 0 && Point < spawnPointsList.Count && spawnPointsList[Point] != null)
        {
            playerObject.transform.position = spawnPointsList[Point].spawnCoordinates.position;
            playerObject.transform.position = spawnPointsList[Point].spawnCoordinates.position;
        }
        else
        {
            Debug.Log($"Error: {nameof(RespawnPlayer)}: Invalid spawn point index {Point}");
        }

        // inputGame?.ShowCursorState(false);
        cameraObject.GetComponent<OrbitingCamera>().enabled = true;                        
    }

    public void BlockInput()
    {
        // inputGame.ShowCursorState(true);
        playerObject.SetActive(false);
        cameraObject.GetComponent<OrbitingCamera>().enabled = false;       
    }

    public IEnumerator DeathProccess()
    {
        BlockInput();
        deathAlert.SetActive(true);
        deathAlertanimator.SetBool("isDeath", true);
        yield return new WaitForSeconds(1.6f);
        deathMenu.SetActive(true);
        Application.ExternalCall("Interestial");
    }
    public void SaveSpawnpointState(SpawnPoint point)
    {
        int tempNumber = spawnPointsList.IndexOf(point);
        if (tempNumber == -1)
        {
            Debug.LogError($"{nameof(SaveSpawnpointState)}: point has invalid idx");
            return;
        }

        Interop.save.areSpawnpointsSet[tempNumber] = true;
        Interop.save.PushSave();
    }

    public void ResetSpawnpoints()
    {
        foreach(SpawnPoint point in spawnPointsList) 
        {
            point.AlreadySet(false);           
        }
        for (int i = 0; i < Interop.save.areSpawnpointsSet.Length; i++)
            Interop.save.areSpawnpointsSet[i] = false;
        Interop.save.PushSave();
    }    
#endif
}
