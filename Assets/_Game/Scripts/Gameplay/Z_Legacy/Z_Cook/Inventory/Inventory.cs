using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

#if GAME_COOK

namespace Cook
{
    public struct InventorySlot
    {
        public const int NoneIdx = -1;

        public int Slot;
        public Item Item;
        public int Count;
        public UIInventorySlot UIView;
        public Inventory Inventory;

        public bool Freed => Item == null;
        public bool Filled => Item != null;

        public InventorySlot(int Slot = NoneIdx, Item Item = null, int Count = 0, UIInventorySlot UIView = null, Inventory Inventory = null)
        {
            this.Slot = Slot;
            this.Item = Item;
            this.Count = Count;
            this.UIView = UIView;
            this.Inventory = Inventory;
        }

        public void Save(ref ItemSlotSaveData Data)
        {
            if (Item)
            {
                Item.Save(ref Data.Item);
            }
            else
            {
                Data.Item.Id = Item.NoneId;
            }

            Data.Count = Count;
        }

        public void Free()
        {
            Item = null;
            Count = 0;
        }
    }

    [Serializable]
    public class Inventory
    {
        public const int MaxSlots = 10;

        private Controller m_Owner;

        [NonSerialized] public InventorySlot[] Slots = new InventorySlot[MaxSlots];
        public int EquippedSlot = InventorySlot.NoneIdx;
        public bool HasEquippedItem => EquippedSlot != InventorySlot.NoneIdx && Slots[EquippedSlot].Item;

        [SerializeField] private Transform m_InventoryAttachmentPoint;
        /** Begin Player Only */
        [SerializeField] private RectTransform m_UIInventoryTransform;
        [SerializeField] private GameObject m_UIInventorySlotPrefab;
        /** End Player Only */

        public void Save(out ItemSlotSaveData[] Data)
        {
            Data = new ItemSlotSaveData[MaxSlots];
            
            for (int i = 0; i < MaxSlots; ++i)
            {
                Slots[i].Save(ref Data[i]);
            }
        }

        public void Load(ItemSlotSaveData[] Data)
        {
            if (Data == null)
            {
                return;
            }

            for (int i = 0; i < Data.Length && i < MaxSlots; ++i)
            {
                for (int j = 0; j < Data[i].Count; ++j)
                {
                    LoadItem(in Data[i].Item);
                }
            }
        }

        public void Init(Controller Owner)
        {
            Assert.IsNotNull(Owner);
            m_Owner = Owner;

            Assert.IsNotNull(m_InventoryAttachmentPoint);
            if (m_Owner.PlayerController)
            {
                Assert.IsNotNull(m_UIInventoryTransform);
                Assert.IsNotNull(m_UIInventorySlotPrefab);
            }

            for (int i = 0; i < Slots.Length; ++i) 
            {
                Slots[i].Slot = i;
                Slots[i].Inventory = this;

                if (m_Owner.PlayerController)
                {
                    GameObject ViewObject = GameObject.Instantiate(m_UIInventorySlotPrefab, m_UIInventoryTransform);

                    Slots[i].UIView = ViewObject.GetComponent<UIInventorySlot>();
                    Assert.IsNotNull(Slots[i].UIView);

                    Slots[i].UIView.UpdateUI(in Slots[i]);
                }
            }

            UpdateLogicAndUI();
        }

        public int GetItemSlot(Item Item)
        {
            if (Item)
            {
                for (int i = 0; i < MaxSlots; ++i)
                {
                    ref InventorySlot Slot = ref Slots[i];

                    if (Item == Slot.Item)
                    {
                        return i;
                    }
                }
            }

            return InventorySlot.NoneIdx;
        }

        // Only for stacking. It doesn't check any free slots
        private int GetStackableItemSlot(Item Item)
        {
            Assert.IsNotNull(Item);

            if (Item.Stackable)
            {
                for (int i = 0; i < MaxSlots; ++i)
                {
                    ref InventorySlot Slot = ref Slots[i];

                    if (Slot.Filled &&
                        Slot.Item.Stackable &&
                        Slot.Item.Type == Item.Type &&
                        Slot.Item.Id == Item.Id &&
                        ((Item.Type == EItemType.Food && Item.Food.Type == Slot.Item.Food.Type) ||
                         (Item.Type == EItemType.Chef))
                    )
                    {
                        return i;
                    }
                }
            }

            return InventorySlot.NoneIdx;
        }

        private int GetFreeSlot()
        {
            for (int i = 0; i < MaxSlots; ++i)
            {
                if (Slots[i].Freed)
                {
                    return i;
                }
            }

            return InventorySlot.NoneIdx;
        }

        private int GetFreeSlotFor(Item Item)
        {
            Assert.IsNotNull(Item);

            int Slot = GetStackableItemSlot(Item);
            if (Slot != InventorySlot.NoneIdx)
            {
                return Slot;
            }

            return GetFreeSlot();
        }

        public bool HasFreeSlot() => GetFreeSlot() != InventorySlot.NoneIdx;
        public bool HasFreeSlotFor(Item Item) => Item != null ? GetFreeSlotFor(Item) != InventorySlot.NoneIdx : false;
        public bool HasFreeSlotFor(GameObject Prefab) => GetFreeSlotFor(Prefab.GetComponent<Item>()) != InventorySlot.NoneIdx;

        private Item Spawn(GameObject Prefab)
        {
            Assert.IsNotNull(Prefab);

            var ItemObject = GameObject.Instantiate(Prefab, Vector3.zero, Quaternion.identity);
            if (!ItemObject)
            {
                return null;
            }

            var Item = ItemObject.GetComponent<Item>();
            Item.Owner = m_Owner;

            return Item;
        }

        // @returns: InventorySlot.NoneIdx if can't be added
        public int SpawnAndAdd(GameObject Prefab)
        {
            var Item = Spawn(Prefab);
            if (!Item)
            {
                return InventorySlot.NoneIdx;
            }

            int Slot = Add(Item);
            if (Slot == InventorySlot.NoneIdx)
            {
                Debug.LogError($"{nameof(SpawnAndAdd)}: Can't add Spawned Item");
            }

            return Slot;
        }

        // @returns: InventorySlot.NoneIdx if can't be added
        public void LoadItem(in ItemSaveData Data)
        {
            if (Data.Id == Item.NoneId)
            {
                return;
            }

            if (!CookManager.Instance.ItemDB.TryGetValue(Data.Id, out ItemDBEntry Entry))
            {
                Debug.LogError($"{nameof(Inventory)}.{nameof(SpawnAndAdd)}: Can't find item {Data.Id} in db");
                return;
            }

            var ItemToLoad = Spawn(Entry.Prefab);
            ItemToLoad.Load(in Data);
        }

        // @returns: InventorySlot.NoneIdx if can't be added
        public int Add(Item Item)
        {
            Assert.IsNotNull(Item);

            int Slot;

            if (Item.Stackable)
            {
                Slot = GetStackableItemSlot(Item);

                if (Slot != InventorySlot.NoneIdx)
                {
                    ++Slots[Slot].Count;
                    GameObject.Destroy(Item.gameObject);

                    UpdateLogicAndUI();
                    return Slot;
                }
            }

            Slot = GetFreeSlot();
            if (Slot == InventorySlot.NoneIdx)
            {
                if (m_Owner.PlayerController)
                {
                    CookManager.Instance.Notify("ui_notify_inventory_is_full", 2f, Color.white, true, "NegativeClick");
                }
                return InventorySlot.NoneIdx;
            }

            Slots[Slot].Item = Item;
            Slots[Slot].Count = 1;

            LeanTween.cancel(Item.gameObject, false);

            Slots[Slot].Item.transform.SetParent(m_InventoryAttachmentPoint, false);
            Slots[Slot].Item.transform.localPosition = Vector3.zero;
            Slots[Slot].Item.transform.localRotation = Quaternion.identity;
            HideItem(Slots[Slot].Item);

            UpdateLogicAndUI();
            return Slot;
        }

        // @Count: on < 0 will remove fully
        // @sa: RemoveFully()
        public Item[] Remove(int Slot, int Count = 1)
        {
            Assert.IsTrue(IsValidSlot(Slot));

            int PreviousEquippedSlot = EquippedSlot;

            if (Count < 0 || Count > Slots[Slot].Count)
            {
                Count = Slots[Slot].Count;
            }

            Item SlotItem = Slots[Slot].Item;
            Slots[Slot].Count -= Count;

            if (Slots[Slot].Count <= 0)
            {
                if (SlotItem)
                {
                    SlotItem.transform.parent = null;
                }

                if (Slots[Slot].Slot == EquippedSlot)
                {
                    Unequip();
                    ShowItem(Slots[Slot].Item);
                }

                Slots[Slot].Free();
            }

            Item[] Items = new Item[Count];

            if (SlotItem)
            {
                for (int i = 0; i < Items.Length; ++i)
                {
                    // Make last element the Item
                    if (Slots[Slot].Count <= 0 && i >= Items.Length - 1)
                    {
                        Items[i] = SlotItem;
                    }
                    else
                    {
                        Items[i] = GameObject.Instantiate(SlotItem.gameObject).GetComponent<Item>();
                    }

                    Assert.IsNotNull(Items[i]);
                    Items[i].transform.SetParent(null, true);
                    ShowItem(Items[i]);
                }
            }

            if (PreviousEquippedSlot != EquippedSlot && EquippedSlot == InventorySlot.NoneIdx)
            {
                int BestSlot = InventorySlot.NoneIdx;

                if (Slots[PreviousEquippedSlot].Filled)
                {
                    BestSlot = PreviousEquippedSlot;
                }
                else
                {
                    for (int i = 0; i < MaxSlots; ++i)
                    {
                        if (Slots[i].Filled)
                        {
                            BestSlot = i;
                            break;
                        }
                    }
                }

                if (BestSlot != InventorySlot.NoneIdx)
                {
                    Equip(BestSlot);
                }
            }

            UpdateLogicAndUI();
            return Items;
        }

        public Item[] Remove(Item Item, int Count = 1) => Remove(GetItemSlot(Item), Count);
        public Item[] RemoveFully(int Slot) => Remove(Slot, -1);
        public Item[] RemoveFully(Item Item) => Remove(Item, -1);

        public void Equip(int Slot)
        {
            if (Slot == EquippedSlot)
            {
                Slot = InventorySlot.NoneIdx;
            }

            if (HasEquippedItem)
            {
                HideItem(Slots[EquippedSlot].Item);
            }

            EquippedSlot = (!IsValidSlot(Slot) || Slots[Slot].Freed) ? InventorySlot.NoneIdx : Slot;

            if (m_Owner.PlayerController && HasEquippedItem)
            {
                ShowItem(Slots[EquippedSlot].Item);
                Cook.CookManager.Instance.OnInventoryItemEquipped?.Invoke();
            }

            UpdateLogicAndUI();
        }

        public void Unequip() => Equip(InventorySlot.NoneIdx);

        static public bool IsValidSlot(int Slot)
        {
            return Slot >= 0 && Slot < MaxSlots;
        }

        public bool HasTakenFood()
        {
            for (int i = 0; i < MaxSlots; ++i)
            {
                if (!Slots[i].Filled)
                {
                    continue;
                }

                var Item = Slots[i].Item;

                if (Item.Type == EItemType.Food && Item.Food.Type == EFoodType.Taken)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasPlaceableItems()
        {
            for (int i = 0; i < MaxSlots; ++i)
            {
                if (!Slots[i].Filled)
                {
                    continue;
                }

                var Item = Slots[i].Item;

                if (Item.Type == EItemType.Chef ||
                    (Item.Type == EItemType.Food && Item.Food.Type == EFoodType.Unprepared)
                )
                {
                    return true;
                }
            }

            return false;
        }

        // @TODO: @SPEED: Should be just marking when LogicAndUI are dirty and do update on LateUpdate()
        public void UpdateLogicAndUI()
        {
            for (int i = MaxSlots - 1; i >= 0; --i)
            {
                if (Slots[i].Filled)
                {
                    continue;
                }

                // We need to save stuff like UIView
                InventorySlot SavedEmptySlot = Slots[i];

                for (int j = i + 1; j < MaxSlots; ++j)
                {
                    Slots[j - 1] = Slots[j];
                    Slots[j - 1].Slot = j - 1;
                }

                // Make last slot empty
                Slots[MaxSlots - 1] = SavedEmptySlot;
                Slots[MaxSlots - 1].Slot = MaxSlots - 1;

                if (EquippedSlot != InventorySlot.NoneIdx && EquippedSlot > i)
                {
                    --EquippedSlot;
                    Assert.IsTrue(IsValidSlot(EquippedSlot));
                }
            }

            // UI update
            if (m_Owner.AIController)
            {
                return;
            }

            int FilledSlots = 0;
            for (int i = 0; i < MaxSlots; ++i)
            {
                Slots[i].UIView.UpdateUI(in Slots[i]);

                if (Slots[i].Filled)
                {
                    ++FilledSlots;
                }
            }

            var DefaultSlotTransform = m_UIInventorySlotPrefab.GetComponent<RectTransform>();
            Assert.IsNotNull(DefaultSlotTransform);

            float Width = MathF.Abs(DefaultSlotTransform.rect.xMin - DefaultSlotTransform.rect.xMax);
            const float Padding = 10f;

            float PanelWidth = Width * FilledSlots + Padding * (FilledSlots - 1);
            float HalfPanelWidth = PanelWidth * 0.5f;

            for (int i = 0; i < FilledSlots; ++i)
            {
                // Starting from the left border of Panel, make sure always have half of (Width + Padding)
                // so our Slot left border is the same as Panel left border
                float X = -HalfPanelWidth + (Width + Padding) * i + Width * 0.5f;

                var Rect = Slots[i].UIView.GetComponent<RectTransform>();
                Assert.IsNotNull(Rect);

                Vector3 Position = Rect.localPosition;
                Position.x = X;
                Rect.localPosition = Position;
            }
        }

        private void ShowItem(Item Item, bool Show = true)
        {
            Assert.IsNotNull(Item);
            Item.gameObject.SetActive(Show);
        }

        private void HideItem(Item Item) => ShowItem(Item, false);
    }
}

#endif
