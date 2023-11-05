
using System;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Utility.AssetInjection;
using System.Linq;
using DaggerfallWorkshop.Game.Items;
using System.Collections.Generic;
using DaggerfallWorkshop.Utility;

namespace OrderService
{
    public class OrderServiceWindow : DaggerfallPopupWindow
    {
        int orderCost;
        static string currentScene;
        static ItemOrderData orderData;
        static bool orderingHelmet;
        static bool orderingShield;
        #region UI Rects

        Rect repairButtonRect = new Rect(5, 5, 120, 7); 
        Rect talkButtonRect = new Rect(5, 14, 120, 7);
        Rect orderButtonRect = new Rect(5, 23, 120, 7);
        Rect sellButtonRect = new Rect(5, 32, 120, 7);
        Rect exitButtonRect = new Rect(44, 42, 43, 15);

        #endregion

        #region UI Controls

        Panel mainPanel = new Panel();
        protected Button repairButton;
        protected Button talkButton;
        protected Button orderButton;
        protected Button sellButton;
        protected Button exitButton;
        protected TextLabel repairLabel = new TextLabel();
        protected TextLabel talkLabel = new TextLabel();
        protected TextLabel orderLabel = new TextLabel();
        protected TextLabel sellLabel = new TextLabel();

        #endregion

        #region UI Textures

        protected Texture2D baseTexture;

        #endregion

        #region Fields

        const string baseTextureName = "BLANKMENU_4";

        protected StaticNPC merchantNPC;

        #endregion

        #region Constructors

        public OrderServiceWindow(IUserInterfaceManager uiManager, StaticNPC npc)
            : base(uiManager)
        {
            ParentPanel.BackgroundColor = Color.clear;
            merchantNPC = npc;
        }

        #endregion

        #region Setup Methods

        protected override void Setup()
        {
            base.Setup();

            // Load all textures
            Texture2D tex;
            TextureReplacement.TryImportTexture(baseTextureName, true, out tex);
            baseTexture = tex;

            SetOrderReady();

            // Create interface panel
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Middle;
            mainPanel.BackgroundTexture = baseTexture;
            mainPanel.Position = new Vector2(0, 50);
            mainPanel.Size = new Vector2(130, 60);

            // repair button
            repairLabel.Position = new Vector2(0, 1);
            repairLabel.ShadowPosition = Vector2.one;
            repairLabel.HorizontalAlignment = HorizontalAlignment.Center;
            repairLabel.Text = "REPAIR";
            repairButton = DaggerfallUI.AddButton(repairButtonRect, mainPanel);
            repairButton.Components.Add(repairLabel);
            repairButton.OnMouseClick += RepairButton_OnMouseClick;
            //roomButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TavernRoom);

            // Talk button
            talkLabel.Position = new Vector2(0, 1);
            talkLabel.ShadowPosition = Vector2.one;
            talkLabel.HorizontalAlignment = HorizontalAlignment.Center;
            talkLabel.Text = "TALK";
            talkButton = DaggerfallUI.AddButton(talkButtonRect, mainPanel);
            talkButton.Components.Add(talkLabel);
            talkButton.OnMouseClick += TalkButton_OnMouseClick;
            //talkButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TavernTalk);
            //talkButton.OnKeyboardEvent += TalkButton_OnKeyboardEvent;

            // Order Items button
            orderLabel.Position = new Vector2(0, 1);
            orderLabel.ShadowPosition = Vector2.one;
            orderLabel.HorizontalAlignment = HorizontalAlignment.Center;
            if (orderData.OrderedHere(currentScene) && !orderData.OrderReady())
                orderLabel.TextColor = Color.gray;
            orderLabel.Text = GetButtonLabel();
            orderButton = DaggerfallUI.AddButton(orderButtonRect, mainPanel);
            orderButton.Components.Add(orderLabel);
            orderButton.OnMouseClick += OrderButton_OnMouseClick;


            // Sell Items button
            sellLabel.Position = new Vector2(0, 1);
            sellLabel.ShadowPosition = Vector2.one;
            sellLabel.HorizontalAlignment = HorizontalAlignment.Center;
            sellLabel.Text = "SELL ITEMS";
            sellButton = DaggerfallUI.AddButton(sellButtonRect, mainPanel);
            sellButton.Components.Add(sellLabel);
            sellButton.OnMouseClick += SellButton_OnMouseClick;
            //drinksButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TavernFood);
            //sellButton.OnKeyboardEvent += SellButton_OnKeyboardEvent;

            // Exit button
            exitButton = DaggerfallUI.AddButton(exitButtonRect, mainPanel);
            exitButton.OnMouseClick += ExitButton_OnMouseClick;
            //exitButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TavernExit);
            //exitButton.OnKeyboardEvent += ExitButton_OnKeyboardEvent;

            NativePanel.Components.Add(mainPanel);
        }

        #endregion

        #region Event Handlers

        protected virtual void RepairButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            CloseWindow();
            uiManager.PushWindow(UIWindowFactory.GetInstanceWithArgs(UIWindowType.Trade, new object[] { uiManager, this, DaggerfallTradeWindow.WindowModes.Repair, null }));
        }

        private void TalkButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            GameManager.Instance.TalkManager.TalkToStaticNPC(merchantNPC);
        }

        private void OrderButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            bool ready = orderData.OrderReady();
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            if (ready)
            {
                ReceiveOrderedItem();
            }
            else if (orderData.IsCompleteOrder() && !ready)
            {
                if (orderData.DaysLeft() <= 0 )
                    DaggerfallUI.MessageBox("Your " + orderData.orderName + " should be ready later today.");
                else if (orderData.DaysLeft() == 1)
                    DaggerfallUI.MessageBox("Your " + orderData.orderName + " should be ready tomorrow.");
                else
                    DaggerfallUI.MessageBox("Your " + orderData.orderName + " should be ready in " + orderData.DaysLeft() + " days.");
            }
            else
            {
                CloseWindow();
                OfferTypes();
            }            
        }

        protected virtual void SellButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            CloseWindow();
            uiManager.PushWindow(UIWindowFactory.GetInstanceWithArgs(UIWindowType.Trade, new object[] { uiManager, this, DaggerfallTradeWindow.WindowModes.Sell, null }));
        }

        private void ExitButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            CloseWindow();
        }





        protected virtual void OfferTypes()
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            if (OrderService.buildingType == DFLocation.BuildingTypes.WeaponSmith)
            {
                DaggerfallListPickerWindow skillPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
                skillPicker.OnItemPicked += OfferWeaponType_OnWpnSkillPicked;
                skillPicker.ListBox.AddItem("Short Blades");
                skillPicker.ListBox.AddItem("Long Blades");
                skillPicker.ListBox.AddItem("Axes");
                skillPicker.ListBox.AddItem("Blunt Weapons");
                skillPicker.ListBox.AddItem("Missile Weapons");

                if (skillPicker.ListBox.Count > 0)
                    uiManager.PushWindow(skillPicker);
            }
            else if (OrderService.buildingType == DFLocation.BuildingTypes.Armorer)
            {
                string armorName;
                DaggerfallListPickerWindow armrPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
                armrPicker.OnItemPicked += OfferArmrTier_OnArmrTypePicked;
                List<Armor> armorList = Enum.GetValues(typeof(Armor)).Cast<Armor>().ToList();
                foreach (Armor armor in armorList)
                {
                    armorName = armor.ToString().Replace("_", " ");
                    armrPicker.ListBox.AddItem(armorName);
                }
                if (armrPicker.ListBox.Count > 0)
                    uiManager.PushWindow(armrPicker);
                else
                    DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");
            }

        }

        protected virtual void OfferWeaponType_OnWpnSkillPicked(int index, string skillString)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            string weaponName;
            DaggerfallListPickerWindow wpnPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
            wpnPicker.OnItemPicked += OfferMat_OnTypePicked;
            List<Weapons> weaponsList = Enum.GetValues(typeof(Weapons)).Cast<Weapons>().ToList();
            foreach (Weapons weapon in weaponsList)
            {
                if (GetSkillFromWeapon(weapon) == skillString)
                {
                    weaponName = weapon.ToString().Replace("_", " ");
                    wpnPicker.ListBox.AddItem(weaponName);
                }
            }
            if (OrderService.RpriWpns)
            {
                if (skillString == "Axes")
                    wpnPicker.ListBox.AddItem("Archers Axe");
                else if (skillString == "Blunt Weapons")
                    wpnPicker.ListBox.AddItem("Light Flail");
                else if (skillString == "Missile Weapons")
                    wpnPicker.ListBox.AddItem("Crossbow");
            }
            if (wpnPicker.ListBox.Count > 0)
                uiManager.PushWindow(wpnPicker);
            else
                DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");
        }

        protected virtual void OfferArmrTier_OnArmrTypePicked(int index, string armrString)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            orderData.SetTemplate((int)GetArmrTemplate(armrString));
            DaggerfallListPickerWindow typePicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
            typePicker.OnItemPicked += OfferMat_OnTypePicked;
            typePicker.ListBox.AddItem("Leather");
            typePicker.ListBox.AddItem("Chain");
            typePicker.ListBox.AddItem("Plate");

            if (ShieldCheck(GetArmrTemplate(armrString)))
                OfferMat_OnTypePicked(3, "Plate");
            else if (typePicker.ListBox.Count > 0)
                uiManager.PushWindow(typePicker);
            else
                DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");
        }


        protected virtual void OfferMat_OnTypePicked(int index, string typeString)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            DaggerfallListPickerWindow materialPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
            int buildQuality = GameManager.Instance.PlayerEnterExit.Interior.BuildingData.Quality;
            Debug.Log("[Order Service] buildingQuality = " + buildQuality.ToString());
            int maxMaterial = 1;
            if (buildQuality <= 3)
                maxMaterial = 2;       // 01 - 03
            else if (buildQuality <= 7)
                maxMaterial = 4;       // 04 - 07
            else if (buildQuality <= 13)
                maxMaterial = 6;       // 08 - 13
            else if (buildQuality <= 17)
                maxMaterial = 7;       // 14 - 17
            else
                maxMaterial = 8;
            Debug.Log("[Order Service] maxMaterial = " + maxMaterial.ToString());
            if (OrderService.buildingType == DFLocation.BuildingTypes.WeaponSmith)
            {
                orderData.SetTemplate(GetWpnTemplate(typeString));
                List<WeaponMaterialTypes> wpnMats = Enum.GetValues(typeof(WeaponMaterialTypes)).Cast<WeaponMaterialTypes>().ToList();
                materialPicker.OnItemPicked += WpnOrder_OnMatPicked;
                foreach (WeaponMaterialTypes material in wpnMats)
                {
                    if (maxMaterial >= 1)
                    {
                        materialPicker.ListBox.AddItem(material.ToString());
                        maxMaterial--;
                    }
                }
                if (materialPicker.ListBox.Count > 0)
                    uiManager.PushWindow(materialPicker);
                else
                    DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");
            }
            else if (OrderService.buildingType == DFLocation.BuildingTypes.Armorer)
            {
                orderData.SetArmorBools(index == 2 ? true : false, index == 1 ? true : false, index == 0 ? true : false);
                maxMaterial += 3;
                List<ArmorMaterialTypes> armrMats = Enum.GetValues(typeof(ArmorMaterialTypes)).Cast<ArmorMaterialTypes>().ToList();
                if (index == 0)
                {
                    if (OrderService.RpriArmrs)
                    {
                        Debug.Log("[Order Service] listing leather materials");
                        materialPicker.OnItemPicked += ArmrOrder_OnMatPicked;
                        foreach (ArmorMaterialTypes material in armrMats)
                        {
                            if (maxMaterial >= 1)
                            {

                                if (material == ArmorMaterialTypes.Chain)
                                {
                                    materialPicker.ListBox.AddItem("Fur");
                                }
                                else if (material != ArmorMaterialTypes.Chain2)
                                {
                                    materialPicker.ListBox.AddItem(material.ToString());
                                }
                                maxMaterial--;
                            }
                        }
                        if (materialPicker.ListBox.Count > 0)
                            uiManager.PushWindow(materialPicker);
                        else
                            DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");
                    }
                    else
                        ArmrOrder_OnMatPicked(0, "Leather");
                }     
                else if (index == 1)
                {
                    if (OrderService.RpriArmrs && !orderingHelmet && !orderingShield)
                    {
                        Debug.Log("[Order Service] listing chain materials");
                        materialPicker.OnItemPicked += ArmrOrder_OnMatPicked;
                        foreach (ArmorMaterialTypes material in armrMats)
                        {
                            if (maxMaterial >= 1)
                            {
                                if ((int)material >= (int)ArmorMaterialTypes.Iron)
                                {
                                    materialPicker.ListBox.AddItem(material.ToString());
                                }
                                maxMaterial--;
                            }
                        }
                        if (materialPicker.ListBox.Count > 0)
                            uiManager.PushWindow(materialPicker);
                        else
                            DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");
                    }
                    else
                        ArmrOrder_OnMatPicked(0, "Chain");
                }   
                else
                {
                    materialPicker.OnItemPicked += ArmrOrder_OnMatPicked;
                    foreach (ArmorMaterialTypes material in armrMats)
                    {
                        if (maxMaterial >= 1)
                        {
                            if ((int)material >= (int)ArmorMaterialTypes.Iron)
                            {
                                materialPicker.ListBox.AddItem(material.ToString());
                            }                       
                            maxMaterial--;
                        }
                    }
                    if (materialPicker.ListBox.Count > 0)
                        uiManager.PushWindow(materialPicker);
                    else
                        DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");
                }   
            }     
        }

        protected virtual void WpnOrder_OnMatPicked(int index, string matName)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            orderData.SetWeaponMat(GetWpnMaterial(matName));
            int days = GetDaysWpn(orderData.pickedItemTemplate, orderData.pickedWpnMat);
            orderData.OrderNewItem(days);
            OfferOrderPopupBox(days);
        }

        protected virtual void ArmrOrder_OnMatPicked(int index, string matName)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            orderData.SetArmorMat(GetArmrMaterial(matName));
            int days = GetDaysArmr(orderData.pickedItemTemplate, orderData.pickedArmrMat);
            orderData.OrderNewItem(days);
            OfferOrderPopupBox(days);
        }

        protected virtual void OfferOrderPopupBox(int days)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            int itemCost = (int)(orderData.value * 1.1f);
            int buildQuality = GameManager.Instance.PlayerEnterExit.Interior.BuildingData.Quality;
            orderCost = FormulaHelper.CalculateTradePrice(itemCost, buildQuality, false);

            DaggerfallMessageBox offerPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            string[] message =
            {
                "You want me to make you a " + orderData.orderName + " eh?",
                "That will take me about " + days.ToString() + " days to complete",
                "and cost you " + orderCost.ToString() + " gold. Payment in advance of course.",
                "",
                "Sound good?"
            };
            offerPopUp.SetText(message);
            offerPopUp.OnButtonClick += StartOrder_OnButtonClick;
            offerPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            offerPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
            offerPopUp.Show();
        }

        protected virtual void StartOrder_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

                sender.CloseWindow();
                if (playerEntity.GetGoldAmount() < orderCost)
                    DaggerfallUI.MessageBox("Come back when you can afford my services.");
                else
                {
                    if (orderData.DaysLeft() <=1)
                        DaggerfallUI.MessageBox("Thank you. Come back tomorrow.");
                    else
                        DaggerfallUI.MessageBox("Thank you. Come back in " + orderData.DaysLeft().ToString() + " days.");

                    playerEntity.DeductGoldAmount(orderCost);
                    OrderService.OrderedItems.Add(orderData);
                }    
            }
            else
            {
                sender.CloseWindow();
                orderData = new ItemOrderData();
            }

        }



        protected virtual void ReceiveOrderedItem()
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            int flags = 0;
            DaggerfallUnityItem variantItem = ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Parchment);
            int variants = orderData.GetVariants();

            if ((orderData.IsArmor() || orderData.IsWeapon()))
            {
                ItemCollection orderedItemVariants = new ItemCollection();
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
                List<int> usedVariants = new List<int>();
                int variantCounter = orderData.GetVariants();
                while (variantCounter >= 0)
                {
                    if (orderData.IsArmor())
                    {
                        variantItem = ItemBuilder.CreateItem(ItemGroups.Armor, orderData.pickedItemTemplate);
                        ItemBuilder.ApplyArmorSettings(variantItem, playerEntity.Gender, playerEntity.Race, orderData.pickedArmrMat);
                        if(orderData.GetVariants() >= 2)
                            ItemBuilder.SetVariant(variantItem, variantCounter);
                        if (variantItem.LongName == orderData.orderName && !usedVariants.Contains(variantItem.CurrentVariant))
                        {
                            usedVariants.Add(variantItem.CurrentVariant);
                            orderedItemVariants.AddItem(variantItem);
                        }       
                    }
                    else if (orderData.IsWeapon())
                    {
                        variantItem = ItemBuilder.CreateItem(ItemGroups.Weapons, orderData.pickedItemTemplate);
                        ItemBuilder.ApplyWeaponMaterial(variantItem, orderData.pickedWpnMat);
                        if (orderData.GetVariants() >= 2)
                            ItemBuilder.SetVariant(variantItem, variantCounter);
                        if (variantItem.LongName == orderData.orderName && !usedVariants.Contains(variantItem.CurrentVariant))
                        {
                            usedVariants.Add(variantItem.CurrentVariant);
                            orderedItemVariants.AddItem(variantItem);
                         }
                    }
                    
                    variantCounter--;
                }
                if (orderedItemVariants.Count >= 2)
                {
                    DaggerfallUI.Instance.InventoryWindow.SetChooseOne(orderedItemVariants, item => flags = flags);
                    DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenInventoryWindow);
                }
                else if (orderedItemVariants.Count == 1)
                {
                    DaggerfallLoot singleItem = GameObjectHelper.CreateDroppedLootContainer(GameManager.Instance.PlayerObject, DaggerfallUnity.NextUID);
                    singleItem.ContainerImage = InventoryContainerImages.Anvil;
                    singleItem.Items.AddItem(variantItem);
                    DaggerfallUI.Instance.InventoryWindow.LootTarget = singleItem;
                    DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenInventoryWindow);
                }
                else
                    DaggerfallUI.MessageBox("Hm, I can not seem to find your order.");
            }
            else
            {
                DaggerfallUI.MessageBox("What are you talking about?.");
            }


            
            OrderService.OrderedItems.Remove(orderData);
            orderData = new ItemOrderData();
        }


            

        static string GetSkillFromWeapon(Weapons weapon)
        {
            switch (weapon)
            {
                case Weapons.Battle_Axe:
                case Weapons.War_Axe:
                    return "Axes";
                case Weapons.Broadsword:
                case Weapons.Claymore:
                case Weapons.Dai_Katana:
                case Weapons.Katana:
                case Weapons.Longsword:
                case Weapons.Saber:
                    return "Long Blades";
                case Weapons.Dagger:
                case Weapons.Shortsword:
                case Weapons.Tanto:
                case Weapons.Wakazashi:
                    return "Short Blades";
                case Weapons.Flail:
                case Weapons.Mace:
                case Weapons.Staff:
                case Weapons.Warhammer:
                    return "Blunt Weapons";
                case Weapons.Long_Bow:
                case Weapons.Short_Bow:
                    return "Missile Weapons";
                default:
                    return "none";
            }
        }

        static WeaponMaterialTypes GetWpnMaterial(string materialString)
        {
            Debug.Log("GetWpnMaterial materialString = " + materialString);
            List<WeaponMaterialTypes> wpnMats = Enum.GetValues(typeof(WeaponMaterialTypes)).Cast<WeaponMaterialTypes>().ToList();
            foreach (WeaponMaterialTypes material in wpnMats)
            {
                Debug.Log("material = " + material.ToString());
                if (material.ToString() == materialString)
                    return material;
            }
            return WeaponMaterialTypes.Iron;
        }

        static ArmorMaterialTypes GetArmrMaterial(string materialString)
        {
            List<ArmorMaterialTypes> armrMats = Enum.GetValues(typeof(ArmorMaterialTypes)).Cast<ArmorMaterialTypes>().ToList();
            foreach (ArmorMaterialTypes material in armrMats)
            {
                if (material == ArmorMaterialTypes.Chain && materialString == "Fur")
                    return material;
                else if (material.ToString() == materialString)
                    return material;
            }
            return ArmorMaterialTypes.Iron;
        }


        static int GetWpnTemplate(string weaponString)
        {
            weaponString = weaponString.Replace(" ", "_");
            List<Weapons> weapons = Enum.GetValues(typeof(Weapons)).Cast<Weapons>().ToList();
            foreach (Weapons weapon in weapons)
            {
                if (weapon.ToString() == weaponString)
                    return (int)weapon;
            }
            if (OrderService.RpriWpns)
            {
                if (weaponString == "Archers_Axe")
                    return 513;
                else if (weaponString == "Light_Flail")
                    return 514;
            }
            if (OrderService.CrossbowsMod)
            {
                if (weaponString == "Crossbow")
                    return 289;
            }

            return (int)Weapons.Dagger;
        }

        static Armor GetArmrTemplate(string armorString)
        {

            armorString = armorString.Replace(" ", "_");

            List<Armor> armors = Enum.GetValues(typeof(Armor)).Cast<Armor>().ToList();
            Armor returnArmor = Armor.Boots;
            foreach (Armor armor in armors)
            {

                if (armor.ToString() == armorString)
                    returnArmor = armor;
            }
            orderingHelmet = returnArmor == Armor.Helm ? true : false;
            orderingShield = (returnArmor == Armor.Buckler || returnArmor == Armor.Round_Shield || returnArmor == Armor.Kite_Shield || returnArmor == Armor.Tower_Shield) ? true : false;
            return returnArmor;
        }

        static bool ShieldCheck(Armor armor)
        {
            if (armor == Armor.Buckler || armor == Armor.Round_Shield || armor == Armor.Kite_Shield || armor == Armor.Tower_Shield)
                return true;
            else
                return false;
        }

        static int GetDaysWpn(int wpnTemplate, WeaponMaterialTypes wpnMat)
        {
            Weapons weapon = Weapons.Arrow;
            if (wpnTemplate <= 131)
                weapon = (Weapons)wpnTemplate;

            int days = 0;
            switch (weapon)
            {
                case Weapons.Claymore:
                case Weapons.War_Axe:
                case Weapons.Dai_Katana:
                    days += 3;
                    break;
                case Weapons.Broadsword:
                case Weapons.Katana:
                case Weapons.Longsword:
                case Weapons.Saber:
                case Weapons.Warhammer:
                case Weapons.Flail:
                case Weapons.Long_Bow:
                    days += 2;
                    break;
                case Weapons.Short_Bow:
                case Weapons.Shortsword:
                case Weapons.Wakazashi:
                case Weapons.Mace:
                case Weapons.Arrow:
                    days += 1;
                    break;
            }

            switch (wpnMat)
            {
                case WeaponMaterialTypes.Silver:
                case WeaponMaterialTypes.Elven:
                    days += 1;
                    break;
                case WeaponMaterialTypes.Dwarven:
                case WeaponMaterialTypes.Mithril:
                    days += 2;
                    break;
                case WeaponMaterialTypes.Adamantium:
                    days += 3;
                    break;
                case WeaponMaterialTypes.Ebony:
                    days += 4;
                    break;
                case WeaponMaterialTypes.Orcish:
                case WeaponMaterialTypes.Daedric:
                    days += 5;
                    break;
            }
            if (days <= 0)
                days = 1;

            return days;
        }

        static int GetDaysArmr(int itemTemplate, ArmorMaterialTypes armrMat)
        {
            int days = 0;
            switch ((Armor)itemTemplate)
            {
                case Armor.Cuirass:
                case Armor.Tower_Shield:
                    days += 2;
                    break;
                case Armor.Kite_Shield:
                case Armor.Greaves:
                case Armor.Left_Pauldron:
                case Armor.Right_Pauldron:
                    days += 1;
                    break;
            }

            switch (armrMat)
            {
                case ArmorMaterialTypes.Silver:
                case ArmorMaterialTypes.Elven:
                    days += 1;
                    break;
                case ArmorMaterialTypes.Dwarven:
                case ArmorMaterialTypes.Mithril:
                    days += 2;
                    break;
                case ArmorMaterialTypes.Adamantium:
                    days += 3;
                    break;
                case ArmorMaterialTypes.Ebony:
                    days += 4;
                    break;
                case ArmorMaterialTypes.Orcish:
                case ArmorMaterialTypes.Daedric:
                    days += 5;
                    break;
            }
            if (days <= 0)
                days = 1;

            return days;
        }

        static void SetOrderReady()
        {
            currentScene = GameManager.Instance.PlayerEnterExit.Interior.name;
            orderData = new ItemOrderData();
            orderingHelmet = false;
            orderingShield = false;

            Debug.Log("currentScene = " + currentScene.ToString());
            foreach (ItemOrderData order in OrderService.OrderedItems)
            {
                if (order.OrderedHere(currentScene))
                    orderData = order;
            }

            Debug.Log("[Order Service] OrderReady = " + orderData.OrderReady().ToString());
            Debug.Log("[Order Service] IsCompleteOrder = " + orderData.IsCompleteOrder().ToString());
        }

        static string GetButtonLabel()
        {
            if (orderData.OrderedHere(currentScene) && !orderData.OrderReady())
                return "PICK UP ORDER";
            else if (!orderData.OrderReady())
            {
                if (OrderService.buildingType == DFLocation.BuildingTypes.Armorer)
                    return "ORDER ARMOR";
                else if (OrderService.buildingType == DFLocation.BuildingTypes.WeaponSmith)
                    return "ORDER WEAPON";
                else
                    return "ORDER ITEM";
            }
            else
                return "PICK UP ORDER"; 
        }

       

        #endregion
    }

}