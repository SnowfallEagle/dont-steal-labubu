using System;
using UnityEngine;

public class ShopChooseController : MonoBehaviour
{
    [SerializeField] ShopObjectController[] shirtColorsArray;
    [SerializeField] ShopObjectController[] pantsColorsArray;
    [SerializeField] ShopObjectController[] specialSkinsNamesArray;
    [SerializeField] string choosedShirtName;
    [SerializeField] string choosedPantsName;
    [SerializeField] string choosedSpecialName;
    [SerializeField] BodySkinsController bodySkinsController;
    [SerializeField] GameObject buyButtonObject;
    [SerializeField] BuySkinButtonController buySkinButtonController;
    [SerializeField] SoundController soundController;
    ShopObjectController prevChoosedPants, prevChoosedShirt, prevChoosedSpecial;
    ShopObjectController rewardingSkin;
    int progressCounnter;
    bool isReward;

#if false

    private void Awake()
    {
        transform.SetParent(null);
    }

    private void Start()
    {
#if false
        buyButtonObject.SetActive(false);
        choosedShirtName = YandexGame.savesData.choosedShirtName;//Interop.save.choosedShirtColor;
        choosedPantsName = YandexGame.savesData.choosedPantsName;//Interop.save.choosedPantsColor;
        choosedSpecialName = YandexGame.savesData.choosedSpecialName;//Interop.save.choosedSpecialColor;

        shirtColorsArray[0].SetBuyState(true);
        pantsColorsArray[0].SetBuyState(true);
        Interop.save.colorsShirtBuyState[0] = true;
        Interop.save.colorsPantsBuyState[0] = true;
        if (choosedShirtName == null && choosedPantsName == null && choosedSpecialName == null)
        {
            ChooseShirt(shirtColorsArray[0]);          
            ChoosePants(pantsColorsArray[0]);
 
            Interop.save.PushSave();
        }

        progressCounnter = 0;     
        foreach (ShopObjectController obj in shirtColorsArray)
        {
            obj.isBuy = Interop.save.colorsShirtBuyState[progressCounnter];
            if (!obj.isBuy && progressCounnter != 0)
            {
                obj.ShowLockImage(true);
            }
            
            if (choosedShirtName != "" && obj.colorName == choosedShirtName)
            {
                prevChoosedShirt = obj;
                ChooseShirt(obj);              
            }
            progressCounnter++;
        }
        
        progressCounnter = 0;
        foreach (ShopObjectController obj in pantsColorsArray)
        {
            obj.isBuy = Interop.save.colorsPantsBuyState[progressCounnter];
            if (!obj.isBuy && progressCounnter != 0)
                obj.ShowLockImage(true);
            if (choosedPantsName != "" && obj.colorName == choosedPantsName)
            {
                prevChoosedPants = obj;
                ChoosePants(obj);               
            }
            progressCounnter++;
        }
        
        progressCounnter = 0;
        foreach (ShopObjectController obj in specialSkinsNamesArray)
        {
            obj.isBuy = Interop.save.specialsBuyState[progressCounnter];
            if (!obj.isBuy)
                obj.ShowLockImage(true);
            if (choosedSpecialName != "" && obj.colorName == choosedSpecialName)
            {
                prevChoosedSpecial = obj;
                ChooseSpecialSkin(obj);
            }

            progressCounnter++;
        }
#endif
    }

    //�� ������
    public void CheckClick(ShopObjectController shopObject)
    {
        ShowBuyButton(!shopObject.isBuy);
        buySkinButtonController.ShowInfo(shopObject);

        if (shopObject.isChoose)
            return;
        else if (shopObject.isBuy)
        {          
            switch (shopObject.skinType)
            {
                case typeOfSkin.shirt:
                    ChooseShirt(shopObject);
                    break;
                case typeOfSkin.pants:
                    ChoosePants(shopObject);
                    break;
                case typeOfSkin.special:
                    ChooseSpecialSkin(shopObject);                    
                    break;
            }
        }
    }

    public void UnlockSkin(ShopObjectController shopObject)
    {
        soundController.Play("PositiveClick");
        shopObject.SetBuyState(true);
        int tempIndex;
        switch (shopObject.skinType)
        {          
            case typeOfSkin.shirt:
                tempIndex = Array.IndexOf(shirtColorsArray, shopObject);
                Interop.save.colorsShirtBuyState[tempIndex] = shopObject.isBuy;
                Interop.save.PushSave();
                break;
            case typeOfSkin.pants:
                tempIndex = Array.IndexOf(pantsColorsArray, shopObject);
                Interop.save.colorsPantsBuyState[tempIndex] = shopObject.isBuy;
                Interop.save.PushSave();
                break;
            case typeOfSkin.special:
                tempIndex = Array.IndexOf(specialSkinsNamesArray, shopObject);
                Interop.save.specialsBuyState[tempIndex] = shopObject.isBuy;
                Interop.save.PushSave();
                break;
        }

        shopObject.ShowLockImage(false);
    }

    void ShowBuyButton(bool state)
    {
        buyButtonObject.SetActive(state);       
    }

    public void SetRewardSkin(ShopObjectController shopObj)
    {
        isReward = false;
        rewardingSkin = shopObj;
    }
    //� jslib
    public void UnlockRewardSkin()
    {
        if (isReward)
            UnlockSkin(rewardingSkin);
    }
    //� jslib
    public void SetRewardingState()
    {
        isReward = true;
    }
    void ChooseShirt(ShopObjectController obj)
    {
        UnchooseAllSpecials();
        UnchooseAllShirts();
        bodySkinsController.ChangeShirtColor(obj.colorName);
        obj.SetChooseState(true);
        obj.ShowChooseImage(true);
        choosedShirtName = obj.colorName;
        prevChoosedShirt = obj;
        SaveNames();
    }

    void ChoosePants(ShopObjectController obj)
    {
        UnchooseAllSpecials();
        UnchooseAllPants();
        bodySkinsController.ChangePantsColor(obj.colorName);
        obj.SetChooseState(true);
        obj.ShowChooseImage(true);
        choosedPantsName = obj.colorName;
        prevChoosedPants = obj;
        SaveNames();
    }

    void ChooseSpecialSkin(ShopObjectController obj)
    {
        UnchooseAllSpecials();
        UnchooseAllShirts();
        UnchooseAllPants();       
        bodySkinsController.ChangeSpecialSkin(obj.colorName);
        obj.SetChooseState(true);
        obj.ShowChooseImage(true);
        choosedSpecialName = obj.colorName;
        prevChoosedSpecial = obj;
        SaveNames();
    }

    void SaveNames()
    {
        /*Interop.save.choosedShirtColor*/
        YandexGame.savesData.choosedShirtName = choosedShirtName;
        /*Interop.save.choosedPantsColor*/
        YandexGame.savesData.choosedPantsName = choosedPantsName;
        /*Interop.save.choosedSpecialColor*/
        YandexGame.savesData.choosedSpecialName = choosedSpecialName;
        //Interop.save.PushSave();
        YandexGame.SaveProgress();
    }

    void UnchooseAllPants()
    {
        if (prevChoosedPants != null)
        {
            prevChoosedPants.ShowChooseImage(false);
            prevChoosedPants.isChoose = false;
        }
        choosedPantsName = "";
    }
    void UnchooseAllShirts()
    {
        if (prevChoosedShirt != null)
        {
            prevChoosedShirt.ShowChooseImage(false);
            prevChoosedShirt.isChoose = false;
        }
        choosedShirtName = "";
    }
    void UnchooseAllSpecials()
    {
        if (prevChoosedSpecial != null)
        {
            prevChoosedSpecial.ShowChooseImage(false);
            prevChoosedSpecial.isChoose = false;
        }
        choosedSpecialName = "";
    }

    
#endif
}
