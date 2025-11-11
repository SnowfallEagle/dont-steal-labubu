using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FirstSceneLoader : MonoBehaviour
{
    [SerializeField] private int m_SceneIndex = 1;
    [SerializeField] private Image m_ProgressBar;

    private AsyncOperation m_LoadingOperation;

    private void Awake()
    {
        Debug.Log($"{nameof(FirstSceneLoader)}.{nameof(Awake)}()");

        Assert.IsNotNull(m_ProgressBar);

        m_LoadingOperation = SceneManager.LoadSceneAsync(m_SceneIndex, LoadSceneMode.Single);
    }

    private void Update()
    {
        m_ProgressBar.fillAmount = m_LoadingOperation.progress;
    }
}
