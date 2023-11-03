// Project:         Order Service mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2023 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using UnityEngine;
using DaggerfallConnect;
using System;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallConnect.Arena2;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Serialization;


namespace OrderService
{
    public class OrderServiceSaveData
    {
        public List<ItemOrderData> SavedOrderedItems;
    }
    public class OrderService : MonoBehaviour, IHasModSaveData
    {
        public Type SaveDataType
        {
            get { return typeof(OrderServiceSaveData); }
        }

        public object NewSaveData()
        {
            return new OrderServiceSaveData
            {
                SavedOrderedItems = new List<ItemOrderData>()
            };
        }

        public object GetSaveData()
        {
            return new OrderServiceSaveData
            {
                SavedOrderedItems = OrderedItems
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var orderServiceSaveData = (OrderServiceSaveData)saveData;
            OrderedItems = orderServiceSaveData.SavedOrderedItems;
        }

        private static Mod mod;
        private static OrderService instance;
        OrderServiceWindow orderWindow;
        internal OrderServiceWindow GetOrderWindow() { return orderWindow; }

        static StaticNPC npc = QuestMachine.Instance.LastNPCClicked;
        static PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        public static DFLocation.BuildingTypes buildingType;
        static public List<ItemOrderData> OrderedItems;
        static public bool RpriArmrs = false;
        static public bool RpriWpns = false;
        static public bool CrossbowsMod = false;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            instance = go.AddComponent<OrderService>();
            mod.SaveDataInterface = instance;

            Mod rpri = ModManager.Instance.GetMod("RoleplayRealism-Items");

            if (rpri != null)
            {
                ModSettings rpriSettings = rpri.GetSettings();
                RpriArmrs = rpriSettings.GetBool("Modules", "newArmor");
                RpriWpns = rpriSettings.GetBool("Modules", "newWeapons");
            }

            Mod crssbw = ModManager.Instance.GetMod("Crossbows");

            if (crssbw != null)
            {
                CrossbowsMod = true;
            }

            mod.IsReady = true;
        }

        private void Start()
        {
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChange;
            OrderedItems = new List<ItemOrderData>();
        }

        private void UIManager_OnWindowChange(object sender, EventArgs e)
        {
            buildingType = GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.buildingType;
            if ((DaggerfallUI.Instance.UserInterfaceManager.TopWindow as DaggerfallMerchantRepairPopupWindow) != null
                && (buildingType == DFLocation.BuildingTypes.Armorer || buildingType == DFLocation.BuildingTypes.WeaponSmith))
                ReplaceRepairMenu();
        }

        public static void ReplaceRepairMenu()
        {
            Debug.Log("[Order Service] Replacing Shop Menu");
            FactionFile.FactionData factionData;
            UserInterfaceManager uiManager = DaggerfallUI.Instance.UserInterfaceManager;
            npc = QuestMachine.Instance.LastNPCClicked;
            QuestResourceBehaviour questResourceBehaviour = npc.gameObject.GetComponent<QuestResourceBehaviour>();
            if (playerEnterExit.IsPlayerInsideOpenShop && GameManager.Instance.PlayerEntity.FactionData.GetFactionData(npc.Data.factionID, out factionData))
            {

                if (!npc || DaggerfallUI.Instance.FadeBehaviour.FadeInProgress || questResourceBehaviour || QuestMachine.Instance.HasFactionListener(npc.Data.factionID))
                    return;

                if ((FactionFile.SocialGroups)factionData.sgroup == FactionFile.SocialGroups.Merchants && GameManager.Instance.PlayerEntity.FactionData.GetFactionData(npc.Data.factionID, out factionData))
                {
                    (DaggerfallUI.Instance.UserInterfaceManager.TopWindow as DaggerfallMerchantRepairPopupWindow).CloseWindow();
                    OrderServiceWindow orderWindow = new OrderServiceWindow(DaggerfallUI.UIManager, npc);
                    DaggerfallUI.UIManager.PushWindow(orderWindow);
                }
            }
            
        }
    }

}