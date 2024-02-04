
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
    public class TailorOrderWindow : DaggerfallPopupWindow
    {
        int orderCost;
        static string currentScene;
        static ItemOrderData orderData;
        static bool orderingHelmet;
        static bool orderingShield;
        #region UI Rects

        Rect talkButtonRect = new Rect(5, 5, 120, 7); 
        Rect sellButtonRect = new Rect(5, 14, 120, 7);
        Rect orderButtonRect = new Rect(5, 23, 120, 7);
        Rect exitButtonRect = new Rect(44, 32, 43, 15);

        #endregion

        #region UI Controls

        Panel mainPanel = new Panel();
        protected Button talkButton;
        protected Button orderButton;
        protected Button sellButton;
        protected Button exitButton;
        protected TextLabel talkLabel = new TextLabel();
        protected TextLabel orderLabel = new TextLabel();
        protected TextLabel sellLabel = new TextLabel();

        #endregion

        #region UI Textures

        protected Texture2D baseTexture;

        #endregion

        #region Fields

        const string baseTextureName = "BLANKMENU_3";

        protected StaticNPC merchantNPC;

        #endregion

        #region Constructors

        public TailorOrderWindow(IUserInterfaceManager uiManager, StaticNPC npc)
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
            mainPanel.Size = new Vector2(130, 51);

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
                OfferSlots();
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





        protected virtual void OfferSlots()
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();

            DaggerfallListPickerWindow slotPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
            slotPicker.OnItemPicked += OfferTypes_OnSlotPicked;
            slotPicker.ListBox.AddItem("Chest");
            slotPicker.ListBox.AddItem("Legs");
            slotPicker.ListBox.AddItem("Feet");
            slotPicker.ListBox.AddItem("Cloaks");

            if (slotPicker.ListBox.Count > 0)
                uiManager.PushWindow(slotPicker);
            else
                DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");
        }

        protected virtual void OfferTypes_OnSlotPicked(int index, string slotString)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();

            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            EquipSlots pickedSlot = GetSlotFromIndex(index);
            string clothName;
            DaggerfallListPickerWindow clothPicker = new DaggerfallListPickerWindow(uiManager, uiManager.TopWindow);
            clothPicker.OnItemPicked += ClothOrder_OnClothPicked;

            if (playerEntity.Gender == Genders.Male)
            {
                List<MensClothing> clothList = new List<MensClothing>();
                switch (index)
                {
                    case 0:
                        clothList = Enum.GetValues(typeof(MensChest)).Cast<MensClothing>().ToList();
                        break;
                    case 1:
                        clothList = Enum.GetValues(typeof(MensLegs)).Cast<MensClothing>().ToList();
                        break;
                    case 2:
                        clothList = Enum.GetValues(typeof(MensFeet)).Cast<MensClothing>().ToList();
                        break;
                    case 3:
                        clothList = Enum.GetValues(typeof(MensCloaks)).Cast<MensClothing>().ToList();
                        break;
                }

                foreach (MensClothing cloth in clothList)
                {
                    clothName = cloth.ToString().Replace("_", " ");
                    clothPicker.ListBox.AddItem(clothName);
                }
            }
            else
            {
                List<WomensClothing> clothList = new List<WomensClothing>();
                switch (index)
                {
                    case 0:
                        clothList = Enum.GetValues(typeof(WomensChest)).Cast<WomensClothing>().ToList();
                        break;
                    case 1:
                        clothList = Enum.GetValues(typeof(WomensLegs)).Cast<WomensClothing>().ToList();
                        break;
                    case 2:
                        clothList = Enum.GetValues(typeof(WomensFeet)).Cast<WomensClothing>().ToList();
                        break;
                    case 3:
                        clothList = Enum.GetValues(typeof(WomensCloaks)).Cast<WomensClothing>().ToList();
                        break;
                }

                foreach (WomensClothing cloth in clothList)
                {
                    clothName = cloth.ToString().Replace("_", " ");
                    clothPicker.ListBox.AddItem(clothName);
                }
            }


            if (clothPicker.ListBox.Count > 0)
                uiManager.PushWindow(clothPicker);
            else
                DaggerfallUI.MessageBox("Sorry, I don't have the capacity to take new orders.");

        }
       

        protected virtual void ClothOrder_OnClothPicked(int index, string clothName)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            orderData.SetTemplate((int)GetClothTemplate(clothName));
            orderData.SetArmorBools(false, false, false);
            orderData.SetArmorMat(ArmorMaterialTypes.None);
            orderData.OrderNewItem(1);
            OfferOrderPopupBox(1);
        }

        protected virtual void OfferOrderPopupBox(int days)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            int itemCost = (int)(orderData.value * 1.6f);
            int buildQuality = GameManager.Instance.PlayerEnterExit.Interior.BuildingData.Quality;
            orderCost = FormulaHelper.CalculateTradePrice(itemCost, buildQuality, false);

            DaggerfallMessageBox offerPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            string[] message =
            {
                "You want me to make you a " + orderData.orderName + " eh?",
                "That will cost you " + orderCost.ToString() + " gold. Payment in advance of course.",
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
            Debug.Log("[Order Service] ReceiveOrderedItem()");
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();


            int flags = 0;

            if (orderData != null)
            {
                Debug.Log("[Order Service] Clothes");
                ItemCollection orderedItemDyes = new ItemCollection();
                int dyesCounter = 9;
                if (GameManager.Instance.PlayerEntity.Gender == Genders.Male)
                {
                    while (dyesCounter >= 0)
                    {
                        DaggerfallUnityItem clothItem = ItemBuilder.CreateItem(ItemGroups.MensClothing, orderData.pickedItemTemplate);
                        clothItem.dyeColor = (DyeColors)dyesCounter;
                        orderedItemDyes.AddItem(clothItem);

                        dyesCounter--;
                    }
                }
                else
                {
                    while (dyesCounter >= 0)
                    {
                        DaggerfallUnityItem clothItem = ItemBuilder.CreateItem(ItemGroups.WomensClothing, orderData.pickedItemTemplate);
                        clothItem.dyeColor = (DyeColors)dyesCounter;
                        orderedItemDyes.AddItem(clothItem);

                        dyesCounter--;
                    }
                }

                if (orderedItemDyes.Count >= 1)
                {
                    DaggerfallUI.Instance.InventoryWindow.SetChooseOne(orderedItemDyes, item => flags = flags);
                    DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenInventoryWindow);
                }
            }
            else
            {
                DaggerfallUI.MessageBox("What are you talking about?.");
            }


            
            OrderService.OrderedItems.Remove(orderData);
            orderData = new ItemOrderData();
        }


        static EquipSlots GetSlotFromIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return EquipSlots.ChestClothes;
                case 1:
                    return EquipSlots.LegsClothes;
                case 2:
                    return EquipSlots.Feet;
                case 3:
                    return EquipSlots.Cloak1;
                default:
                    return EquipSlots.None;
            }
        }


        static MensClothing GetClothTemplate(string clothString)
        {
            clothString = clothString.Replace(" ", "_");

            List<MensClothing> mensClothing = Enum.GetValues(typeof(MensClothing)).Cast<MensClothing>().ToList();
            MensClothing returnCloth = MensClothing.Anticlere_Surcoat;
            foreach (MensClothing cloth in mensClothing)
            {

                if (cloth.ToString() == clothString)
                    returnCloth = cloth;
            }
            return returnCloth;
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

        public enum MensChest //check
        {
            Straps = 141,
            Armbands = 142,
            Kimono = 143,
            Fancy_Armbands = 144,
            Sash = 145,
            Eodoric = 146,
            Dwynnen_surcoat = 157,
            Short_tunic = 158,
            Formal_tunic = 159,
            Toga = 160,
            Reversible_tunic = 161,
            Plain_robes = 163,
            Priest_robes = 164,
            Short_shirt = 165,
            Long_shirt = 167,
            Open_Tunic = 173,
            Wrap = 174,
            Anticlere_Surcoat = 176,
            Challenger_Straps = 177,
            Vest = 180,
            Champion_straps = 181,
        }

        public enum MensLegs  //check
        {
            Casual_pants = 151,
            Breeches = 152,
            Short_skirt = 153,
            Khajiit_suit = 156,
            Loincloth = 162,
            Long_Skirt = 175,
        }

        public enum MensFeet  //check
        {
            Shoes = 147,
            Tall_Boots = 148,
            Boots = 149,
            Sandals = 150,
        }

        public enum MensCloaks  //check
        {
            Casual_cloak = 154,
            Formal_cloak = 155,
        }

        public enum WomensChest //check
        {
            Brassier = 182,
            Formal_brassier = 183,
            Peasant_blouse = 184,
            Eodoric = 185,
            Formal_eodoric = 194,
            Evening_gown = 195,
            Day_gown = 196,
            Casual_dress = 197,
            Strapless_dress = 198,
            Plain_robes = 200,
            Priestess_robes = 201,
            Short_shirt = 202,
            Long_shirt = 204,
            Open_tunic = 210,
            Vest = 216,
        }

        public enum WomensLegs  //check
        {
            Casual_pants = 190,
            Khajiit_suit = 193,
            Loincloth = 199,
            Wrap = 211,
            Long_skirt = 212,
            Tights = 213,
        }

        public enum WomensFeet  //check
        {
            Shoes = 186,
            Tall_boots = 187,
            Boots = 188,
            Sandals = 189,
        }

        public enum WomensCloaks  //check
        {
            Casual_cloak = 191,
            Formal_cloak = 192,
        }


        #endregion
    }

}