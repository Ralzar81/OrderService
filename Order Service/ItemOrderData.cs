using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;

namespace OrderService
{
    public class ItemOrderData
    {
        private string sceneName;
        private ulong timeStarted = 0;
        private ulong timeFinished = 0;
        private WeaponMaterialTypes pickedWpnMat = WeaponMaterialTypes.None;
        private ArmorMaterialTypes pickedArmrMat = ArmorMaterialTypes.None;
        private int pickedWpnTemplate;
        private Armor pickedArmr;
        private bool isPlate;
        private bool isChain;
        private bool isLeather;
        private DaggerfallUnityItem orderedItem;

        public void OrderNewItem(int forgeDays)
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (IsWeapon())
            {
                Debug.Log("templateIndex = " + pickedWpnTemplate.ToString());
                orderedItem = ItemBuilder.CreateItem(ItemGroups.Weapons, pickedWpnTemplate);
                Debug.Log("orderedItem = " + orderedItem.LongName);
                ItemBuilder.ApplyWeaponMaterial(orderedItem, pickedWpnMat);
                Debug.Log("orderedItem = " + orderedItem.LongName);
            }
                
            else if (IsArmor())
            {
                orderedItem = ItemBuilder.CreateArmor(playerEntity.Gender, playerEntity.Race, pickedArmr, pickedArmrMat);
                int templateIndex = orderedItem.TemplateIndex;
                if (OrderService.RpriArmrs && (isLeather || isChain))
                {
                    if (isLeather)
                    {
                        switch (pickedArmr)
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
                        switch (pickedArmr)
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

                    orderedItem = ItemBuilder.CreateItem(ItemGroups.Armor, templateIndex);
                    ItemBuilder.ApplyArmorSettings(orderedItem, playerEntity.Gender, playerEntity.Race, pickedArmrMat);
                }
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
            if (sceneName != null && timeStarted != 0 && timeFinished != 0 && orderedItem != null)
                return true;
            else
                return false;
        }

        public DaggerfallUnityItem GetItem()
        {
            if (orderedItem != null)
                return orderedItem;
            else
                return null;
        }

        public void SetArmor(Armor armor)
        {
            pickedArmr = armor;
        }

        public void SetWeapon(int weapon)
        {
            pickedWpnTemplate = weapon;
        }

        public void SetArmorMat(ArmorMaterialTypes material)
        {
            pickedArmrMat = material;
        }

        public void SetWeaponMat(WeaponMaterialTypes material)
        {
            Debug.Log("SetWeaponMat material = " + material.ToString());
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

        public Armor GetArmor()
        {
            return pickedArmr;
        }

        public Weapons GetWeapon()
        {
            if (pickedWpnTemplate >= 132)
                return Weapons.Arrow;
            else
                return (Weapons)pickedWpnTemplate;
        }

        public ArmorMaterialTypes GetArmorMat()
        {
            return pickedArmrMat;
        }

        public WeaponMaterialTypes GetWeaponMat()
        {
            return pickedWpnMat;
        }

        public bool IsHelmet()
        {
            if (orderedItem.EquipSlot == EquipSlots.Head)
                return true;
            else
                return false;
        }

        public bool IsShield()
        {
            return orderedItem.IsShield;
        }

        public int GetVariants()
        {
            if (IsArmor())
            {
                if (IsHelmet() || IsShield())
                    return orderedItem.TotalVariants;
                if (isPlate)
                {
                    switch ((int)orderedItem.TemplateIndex)
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
                else if (isLeather && (int)orderedItem.TemplateIndex == (int)Armor.Greaves)
                    return 2;
            }

            return 1;
        }
    }
}
