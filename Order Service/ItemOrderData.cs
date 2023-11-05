using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;

namespace OrderService
{
    [FullSerializer.fsObject("v1")]
    public class ItemOrderData
    {
        public string sceneName;
        public ulong timeStarted = 0;
        public ulong timeFinished = 0;
        public WeaponMaterialTypes pickedWpnMat = WeaponMaterialTypes.None;
        public ArmorMaterialTypes pickedArmrMat = ArmorMaterialTypes.None;
        public int pickedItemTemplate;
        public bool isPlate;
        public bool isChain;
        public bool isLeather;
        public bool isHelmet;
        public bool isShield;
        public string orderName;
        public int value;

        public void OrderNewItem(int forgeDays)
        {
            DaggerfallUnityItem orderedItem;
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (IsWeapon())
            {
                orderedItem = ItemBuilder.CreateItem(ItemGroups.Weapons, pickedItemTemplate);
                ItemBuilder.ApplyWeaponMaterial(orderedItem, pickedWpnMat);
                orderName = orderedItem.LongName;
                value = orderedItem.value;
            }   
            else if (IsArmor())
            {
                int templateIndex = pickedItemTemplate;
                if (OrderService.RpriArmrs && (isLeather || isChain))
                {
                    if (isLeather)
                    {
                        switch ((Armor)pickedItemTemplate)
                        {
                            case Armor.Cuirass:
                                templateIndex = 520;
                                break;
                            case Armor.Greaves:
                                templateIndex = 521;
                                break;
                            case Armor.Helm:
                                templateIndex = 522;
                                break;
                            case Armor.Boots:
                                templateIndex = 523;
                                break;
                            case Armor.Gauntlets:
                                templateIndex = 524;
                                break;
                            case Armor.Left_Pauldron:
                                templateIndex = 525;
                                break;
                            case Armor.Right_Pauldron:
                                templateIndex = 526;
                                break;
                        }
                    }
                    else
                        switch ((Armor)pickedItemTemplate)
                        {
                            case Armor.Cuirass:
                                templateIndex = 515;
                                break;
                            case Armor.Greaves:
                                templateIndex = 516;
                                break;
                            case Armor.Boots:
                                templateIndex = 519;
                                break;
                            case Armor.Left_Pauldron:
                                templateIndex = 517;
                                break;
                            case Armor.Right_Pauldron:
                                templateIndex = 518;
                                break;
                        }
                    pickedItemTemplate = templateIndex;
                }
                orderedItem = ItemBuilder.CreateItem(ItemGroups.Armor, pickedItemTemplate);
                ItemBuilder.ApplyArmorSettings(orderedItem, playerEntity.Gender, playerEntity.Race, pickedArmrMat);
                isHelmet = orderedItem.EquipSlot == EquipSlots.Head ? true : false;
                isShield = orderedItem.IsShield;
                orderName = orderedItem.LongName;
                value = orderedItem.value;

            }
            sceneName = GameManager.Instance.PlayerEnterExit.Interior.name;
            timeStarted = DaggerfallUnity.Instance.WorldTime.Now.ToSeconds();
            timeFinished = timeStarted + (ulong)(forgeDays * DaggerfallDateTime.SecondsPerDay);
        } 

        public bool OrderedHere(string interiorName)
        {
            if (interiorName == sceneName)
                return true;
            else
                return false;
        }

        public int DaysLeft()
        {
            ulong daysLeft = (timeFinished - DaggerfallUnity.Instance.WorldTime.Now.ToSeconds()) / DaggerfallDateTime.SecondsPerDay;
            ulong hoursLeft = (timeFinished - DaggerfallUnity.Instance.WorldTime.Now.ToSeconds()) / DaggerfallDateTime.SecondsPerHour;
            if (hoursLeft < 13)
                return 0;
            else if (hoursLeft < 36)
                return 1;
            return (int)daysLeft + 1;
        }

        public bool OrderReady()
        {
            if (IsCompleteOrder() && timeFinished <= DaggerfallUnity.Instance.WorldTime.Now.ToSeconds())
                return true;
            else
                return false;
        }

        public bool IsCompleteOrder()
        {
            if (sceneName != null && timeStarted != 0 && timeFinished != 0 && pickedItemTemplate != 0 && orderName != null)
                return true;
            else
                return false;
        }

        public void SetTemplate(int item)
        {
            pickedItemTemplate = item;
        }

        public void SetArmorMat(ArmorMaterialTypes material)
        {
            pickedArmrMat = material;
        }

        public void SetWeaponMat(WeaponMaterialTypes material)
        {
            pickedWpnMat = material;
        }

        public void SetArmorBools(bool setIsPlate, bool setIsChain, bool setIsLeather)
        {
            isPlate = setIsPlate;
            isChain = setIsChain;
            isLeather = setIsLeather;
        }

        public bool IsArmor()
        {
            if (pickedArmrMat != ArmorMaterialTypes.None)
                return true;
            else
                return false;
        }

        public bool IsWeapon()
        {
            if (pickedWpnMat != WeaponMaterialTypes.None)
                return true;
            else
                return false;
        }

        public int GetVariants()
        {
            if (IsArmor())
            {
                if (isHelmet && !isLeather)
                    return 6;
                if (isPlate)
                {
                    switch (pickedItemTemplate)
                    {
                        case (int)Armor.Cuirass:
                        case (int)Armor.Left_Pauldron:
                        case (int)Armor.Right_Pauldron:
                            return 3;
                        case (int)Armor.Greaves:
                            return 4;
                        case (int)Armor.Boots:
                            return 2;
                    }
                }
                else if (isLeather && pickedItemTemplate == (int)Armor.Greaves)
                    return 2;
            }

            return 1;
        }
    }
}
