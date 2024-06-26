﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using TerminalPlus.Mods;

namespace TerminalPlus
{
    partial class Nodes
    {
        public static List<Item> storeItems = new List<Item>();
        public static List<TerminalNode> storeDecorations = new List<TerminalNode>();
        public static List<UnlockableItem> storeShipUpgrades = new List<UnlockableItem>();

        public string StorePage(Terminal terminal)
        {
            var instanceOnShip = GameObject.Find("/Environment/HangarShip").GetComponentsInChildren<GrabbableObject>().ToList();

            storeItems = terminal.buyableItemsList.OrderBy(x => x.itemName).ToList();
            if (PluginMain.LethalLibExists) LethalLibCompatibility.RemoveHiddenStoreItems(terminal);
            storeDecorations = terminal.ShipDecorSelection.OrderBy(x => x.creatureName).ToList();
            storeShipUpgrades = StartOfRound.Instance.unlockablesList.unlockables.Where(x => x.unlockableType == 1 && x.alwaysInStock == true).ToList();
            StringBuilder pageChart = new StringBuilder();

            pageChart.AppendLine("\n\n  ╔═══════════════════════════════════════════════╗");
            pageChart.AppendLine(@"  ║ ♥-  ╔╦╗╗╔╔╗  ╔╗╔╗╦╦╗╔╗╔╗╦╗╗╔  ╔╗╔╦╗╔╗╦╗╔╗  -♣ ║");
            pageChart.AppendLine(@"  ║ |    ║ ╠╣╠   ║ ║║║║║╠╝╠╣║║╠╝  ╚╗ ║ ║║║╣╠    | ║");
            pageChart.AppendLine(@"  ║ ♠-   ╩ ╝╚╚╝  ╚╝╚╝╩ ╩╩ ╩╩╩╚╚   ╚╝ ╩ ╚╝╩╚╚╝  -♦ ║");
            pageChart.AppendLine(@"  ╠══════════════════════╦═══════╦═════╦══════════╣");
            pageChart.AppendLine(@"  ║   <space=0.265en>ITEM/UNLOCKABLE<space=0.265en>   ║ PRICE ║<space=0.265en>SALE<space=0.265en>║  STATUS  ║");
            pageChart.AppendLine(@"  ╠══════════════════════╩═══════╩═════╩══════════╣");
            foreach (var item in storeItems)
            {
                if (terminal.buyableItemsList.ToList().IndexOf(item) < 0) continue;
                string cName = item.itemName.Length > 20 ? item.itemName.Substring(0, 17) + "..." : item.itemName.PadRight(20);

                int percentSale = terminal.itemSalesPercentages[terminal.buyableItemsList.ToList().IndexOf(item)];
                string itemSale = $"{100 - percentSale}%".PadLeft(3);
                if (itemSale == " 0%") itemSale = "   ";
                else if (itemSale == "100%") itemSale = "X$X";
                int itemPrice = (int)Math.Round(item.creditsWorth * (percentSale / 100f));
                if (itemPrice > 9999) itemPrice = 9999;
                int numOnShip = instanceOnShip.FindAll(x => x.itemProperties.itemName == item.itemName).Count;

                string displayNumOnShip = numOnShip.ToString();
                if (numOnShip > 99) displayNumOnShip = "Over 100";
                else if (numOnShip <= 0) displayNumOnShip = "        ";
                else if (numOnShip < 10) displayNumOnShip = numOnShip.ToString().PadLeft(2, '0') + " Owned";
                else displayNumOnShip = numOnShip.ToString().PadRight(2) + " Owned";

                pageChart.AppendLine($"  ║ {cName} |<cspace=-2> $</cspace>{itemPrice,-4}<cspace=-0.6> |</cspace> {itemSale} | {displayNumOnShip} ║");
            }
            pageChart.AppendLine("  ╠═══════════════════════════════════════════════╣"); //╠═══════════════════╬═══════╬══════╬════════════╣

            storeShipUpgrades.OrderBy(x => x.unlockableName).ToList();
            Dictionary<string, string> defaultUpgrades = new Dictionary<string, string>()
            {
                { "teleporter", "375 " },
                { "signal translator", "255 " },
                { "loud horn", "100 " },
                { "inverse teleporter", "425 " }
            };

            foreach (UnlockableItem upgrade in storeShipUpgrades)
            {
                TerminalNode upgradeNode = upgrade.shopSelectionNode;
                string upgradePrice = string.Empty;
                string upgradeName = upgrade.unlockableName;

                if (defaultUpgrades.ContainsKey(upgrade.unlockableName.ToLower())) upgradePrice = defaultUpgrades[upgrade.unlockableName.ToLower()];
                else if (upgradeNode != null) upgradePrice = upgradeNode.itemCost.ToString().PadRight(4);
                else
                {
                    upgradeNode = UnityEngine.Object.FindObjectsOfType<TerminalNode>().ToList().Find(x => x.shipUnlockableID ==
                    StartOfRound.Instance.unlockablesList.unlockables.ToList().IndexOf(upgrade));
                    if (upgradeNode != null) upgradePrice = upgradeNode.itemCost.ToString().PadRight(4);
                    else upgradePrice = "??? ";
                }
                upgradeName = upgradeName.Length > 20 ? upgradeName.Substring(0, 17) + "..." : upgradeName.PadRight(20);

                if (upgrade.alreadyUnlocked || upgrade.hasBeenUnlockedByPlayer)
                {
                    pageChart.AppendLine($"  ║ {upgradeName} |<cspace=-2> $</cspace>{upgradePrice}<cspace=-0.6> |</cspace>     | UNLOCKED ║");
                }
                else
                {
                    pageChart.AppendLine($"  ║ {upgradeName} |<cspace=-2> $</cspace>{upgradePrice}<cspace=-0.6> |</cspace>     |          ║");
                }
            }
            if (PluginMain.LGUExists && ConfigManager.showLGUStore) pageChart.Append(LGU.LGUCompatibility.LGUString());

            pageChart.AppendLine("  ╠═══════════════════════════════════════════════╣");

            foreach (TerminalNode decoration in storeDecorations)
            {
                UnlockableItem unlockable = StartOfRound.Instance.unlockablesList.unlockables[decoration.shipUnlockableID];
                decoration.creatureName = decoration.creatureName.Length > 20 ? decoration.creatureName.Substring(0, 17) + "..." : decoration.creatureName.PadRight(20);
                
                if (unlockable != null && (unlockable.alreadyUnlocked || unlockable.hasBeenUnlockedByPlayer))
                {
                    pageChart.AppendLine($"  ║ {decoration.creatureName} |<cspace=-2> $</cspace>{decoration.itemCost,-4}<cspace=-0.6> |</cspace>     | UNLOCKED ║");
                }
                else if (unlockable != null)
                {
                    pageChart.AppendLine($"  ║ {decoration.creatureName} |<cspace=-2> $</cspace>{decoration.itemCost,-4}<cspace=-0.6> |</cspace>     |          ║");
                }
            }
            pageChart.AppendLine("  ╚═══════════════════════════════════════════════╝");

            return pageChart.ToString();
        }
    }
}
