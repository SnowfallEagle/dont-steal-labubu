using System;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using MenteBacata.ScivoloCharacterControllerDemo;

public class Player : MonoBehaviour
{
#if GAME_COOK
    private Cook.Controller m_Owner;
    public Cook.Controller Owner => m_Owner;

    [SerializeField] private Cook.Inventory m_Inventory = new();
    public Cook.Inventory Inventory => m_Inventory;
#endif

#if GAME_SING
    [SerializeField] private Sing.Controller m_Owner;
    public Sing.Controller Owner => m_Owner;

    [SerializeField] private Rigidbody m_Rigidbody;
    public Rigidbody Rigidbody => m_Rigidbody;

#endif

    public event Action OnMoneyChanged;

    [SerializeField] private float m_InteractableRayDistance = 100f;
    public float InteractableRayDistance => m_InteractableRayDistance;

    private double m_Money = 0f;
    public double Money
    {
        get => m_Money;
        private set
        {
            m_Money = value;
#if GAME_COOK
            m_MoneyText.text = $"${Money:F0}";
#else
            m_MoneyText.text = $"{Money:F0}";
#endif
            OnMoneyChanged?.Invoke();
        }
    }
    [SerializeField] private TextMeshProUGUI m_MoneyText;

    private SimpleCharacterController m_CharMoveController;
    public SimpleCharacterController CharMoveController => m_CharMoveController;

    [SerializeField] private Outline m_Outline;
    public Outline Outline => m_Outline;

    private void Awake()
    {
#if GAME_COOK
        m_Owner = GetComponent<Cook.Controller>();
        Assert.IsNotNull(m_Owner);

        m_Inventory.Init(m_Owner);
#endif

#if GAME_SING
        Assert.IsNotNull(m_Owner);
        Assert.IsNotNull(m_Rigidbody);
#endif

        m_CharMoveController = GetComponent<SimpleCharacterController>();
        Assert.IsNotNull(m_CharMoveController);

        Assert.IsNotNull(m_Outline);

        m_CharMoveController.OnJumped += OnPlayerJumped;
    }

    // @returns 0f on success, > 0f when hit money limit
    public double AddMoney(
        double Amount
#if GAME_SING
        , bool NotifyMoney = true
#endif
    )
    {
#if GAME_COOK
        Cook.CookManager.Instance.Notify($"+${Amount:F0}", 2f, Color.green, false);
        SoundController.Instance.Play("CashMoney");
#endif
#if GAME_SING
        if (NotifyMoney)
        {
            Sing.SingManager.Instance.NotifyMoney(Amount);
        }
#endif

        double NewMoney = Money + Amount;
        double RemainingPart = Constants.MaxMoney - NewMoney;

        if (RemainingPart < 0f)
        {
            Money = Constants.MaxMoney;
            return Math.Abs(RemainingPart);
        }

        Money = NewMoney;
        return 0f;
    }

    // @returns: 0f on success, negative value is how much money is missing
    public double WithdrawMoney(double Amount)
    {
        double NewMoney = Money - Amount;

        if (NewMoney >= 0f)
        {
            // Success
            Money = NewMoney;
            return 0f;
        }

        // Money missing
        return NewMoney;
    }

    public void SetMoney(double Amount)
    {
        Money = Amount;
    }

    public void UpdateOutline()
    {
        m_Outline.Custom_OnMeshChanged();
    }

    private void OnPlayerJumped()
    {
        SoundController.Instance.Play("Jump");
    }
}
