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
    [FullSerializer.fsObject("v1")]
    public class OrderServiceSaveData
    {
        public List<ItemOrderData> SavedOrderedItems;
        public bool rprWpnsMod;
        public bool rprArmrMod;
        public bool crossbwsMod;
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
                SavedOrderedItems = OrderedItems,
                rprWpnsMod = RpriWpns,
                rprArmrMod = RpriArmrs,
                crossbwsMod = CrossbowsMod
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var orderServiceSaveData = (OrderServiceSaveData)saveData;

            if ((orderServiceSaveData.rprWpnsMod && !RpriWpns) || (orderServiceSaveData.rprArmrMod && !RpriArmrs) || (orderServiceSaveData.crossbwsMod && !CrossbowsMod))
                OrderedItems = new List<ItemOrderData>();
            else
                OrderedItems = orderServiceSaveData.SavedOrderedItems;
        }

        private static Mod mod;
        private static OrderService instance;
        RepairOrderWindow orderWindow;
        internal RepairOrderWindow GetOrderWindow() { return orderWindow; }

        static StaticNPC npc = QuestMachine.Instance.LastNPCClicked;
        static PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        public static DFLocation.BuildingTypes buildingType;
        static public List<ItemOrderData> OrderedItems = new List<ItemOrderData>();
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
        }

        private void UIManager_OnWindowChange(object sender, EventArgs e)
        {
            buildingType = GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.buildingType;
            if ((DaggerfallUI.Instance.UserInterfaceManager.TopWindow as DaggerfallMerchantRepairPopupWindow) != null || (DaggerfallUI.Instance.UserInterfaceManager.TopWindow as DaggerfallMerchantServicePopupWindow) != null)
            {
                    ReplaceMenu();
            }
        }

        public static void ReplaceMenu()
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
                    buildingType = GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.buildingType;
                    if (buildingType == DFLocation.BuildingTypes.Armorer || buildingType == DFLocation.BuildingTypes.WeaponSmith)
                    {
                        (DaggerfallUI.Instance.UserInterfaceManager.TopWindow as DaggerfallMerchantRepairPopupWindow).CloseWindow();
                        RepairOrderWindow orderWindow = new RepairOrderWindow(DaggerfallUI.UIManager, npc);
                        DaggerfallUI.UIManager.PushWindow(orderWindow);
                    }
                    else if (buildingType == DFLocation.BuildingTypes.ClothingStore)
                    {
                        (DaggerfallUI.Instance.UserInterfaceManager.TopWindow as DaggerfallMerchantServicePopupWindow).CloseWindow();
                        TailorOrderWindow orderWindow = new TailorOrderWindow(DaggerfallUI.UIManager, npc);
                        DaggerfallUI.UIManager.PushWindow(orderWindow);
                    }
                        
                }
            }

        }
    }

}