using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintShow : MonoBehaviour
{
    [SerializeField] Text hintText;

    private void Start()
    {
        hintText.gameObject.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
            hintText.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            hintText.gameObject.SetActive(false);
    }

}
