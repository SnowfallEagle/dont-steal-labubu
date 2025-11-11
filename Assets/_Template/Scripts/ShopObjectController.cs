using UnityEngine;

public enum typeOfSkin
{ 
    shirt = 1,
    pants = 2,
    special = 3
};

public class ShopObjectController : MonoBehaviour
{
    [SerializeField] public bool isBuy;
    [SerializeField] public bool isChoose;
    [SerializeField] public bool isAdsSell;
    [SerializeField] public bool isRateSell;
    [SerializeField] public int price;
    [SerializeField] public string colorName;

    private GameObject m_LockImageObject;
    private GameObject lockImageObject
    {
        get
        {
            if (!m_LockImageObject)
            {
                m_LockImageObject = transform.GetChild(1).gameObject;     
            }
            return m_LockImageObject;
        }

        set => m_LockImageObject = value;
    }

    private GameObject m_ChooseImageObject;
    private GameObject chooseImageObject
    {
        get
        {
            if (!m_ChooseImageObject)
            {
                m_ChooseImageObject = transform.GetChild(2).gameObject;      
            }
            return m_ChooseImageObject;
        }

        set => m_ChooseImageObject = value;
    }

    [SerializeField] public typeOfSkin skinType;
    [SerializeField] ShopChooseController shopChooseController;

#if false

    public void OnClickButton()
    {
        shopChooseController.CheckClick(this);
    }
    public void ShowChooseImage(bool state)
    {
        chooseImageObject.SetActive(state);
    }
    public void ShowLockImage(bool state)
    {
        lockImageObject.SetActive(state);
    }
    public void SetChooseState(bool state)
    {
        isChoose = state;
        //Сохранение
    }
    public void SetBuyState(bool state)
    {
        isBuy = state;
        //Сохранение
    }

    public void OnClick1()
    {
        
    }
#endif
}
