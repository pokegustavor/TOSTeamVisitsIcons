using System;
using System.Collections.Generic;
using System.ComponentModel;
using Game.Interface;
using Game.Simulation;
using HarmonyLib;
using Server.Shared.Info;
using Server.Shared.Messages;
using Server.Shared.State;
using Server.Shared.State.Chat;
using Services;
using Shared.Chat;
using SML;
using UnityEngine;
using UnityEngine.UI;

namespace TOSTeamVisitsIcons
{
    internal class Interpreter
    {
        static RoleCardPanel panel = null;
        internal static void HandleMessages(ChatLogMessage chatLogMessage)
        {
            if (Service.Game.Sim.info.gameInfo.Data.playPhase == PlayPhase.NIGHT && Service.Game.Sim.info.gameInfo.Data.gamePhase == GamePhase.PLAY && chatLogMessage.chatLogEntry is ChatLogFactionTargetSelectionFeedbackEntry)
            {
                try
                {
                    ChatLogFactionTargetSelectionFeedbackEntry data = chatLogMessage.chatLogEntry as ChatLogFactionTargetSelectionFeedbackEntry;
                    UIRoleData.UIRoleDataInstance roleData = null;
                    Console.Write($"TOSTVI recieved message: player {data.teammatePosition + 1} (role {data.teammateRole}) has decided to ");
                    if (data.bIsCancel)
                    {
                        Console.WriteLine($"Cancel their night ability");
                    }
                    else if (data.bIsChangingTarget)
                    {
                        Console.WriteLine($"Change their target to {data.teammateTargetingPosition1}");
                    }
                    else
                    {
                        Console.WriteLine($"Target {data.teammateTargetingPosition1}");
                    }
                    Console.WriteLine($"They {(data.bHasNecronomicon ? "have" : "don't have")} the necronomicon!");
                    if (panel == null)
                    {
                        panel = UnityEngine.Object.FindObjectOfType<RoleCardPanel>();
                        Console.WriteLine("TOSTVI panel was null, a new one was grabbed");
                    }
                    if (panel != null)
                    {
                        roleData = panel.roleData.roleDataList.Find((UIRoleData.UIRoleDataInstance d) => d.role == data.teammateRole);
                        if (roleData != null)
                        {
                            if (roleData.roleIcon != null)
                            {
                                Console.WriteLine("TOSTVI all roledata grabed with success");
                                if (data.bIsCancel)
                                {
                                    Manager.Instance.CancelTarget(data.menuChoiceType, data.teammateRole,data.teammatePosition);
                                }
                                else
                                {
                                    Console.WriteLine("TOSTVI grabbing sprite");
                                    Sprite sprite = roleData.roleIcon;
                                    if (ModSettings.GetString("Display Mode") == "Ability Icon")
                                    {
                                        if (data.menuChoiceType == MenuChoiceType.NightAbility || (data.teammateRole == Role.ILLUSIONIST && data.menuChoiceType == MenuChoiceType.NightAbility2))
                                        {
                                            if (data.bHasNecronomicon)
                                            {
                                                sprite = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                                            }
                                            else
                                            {
                                                sprite = roleData.abilityIcon;
                                            }
                                        }
                                        else if (data.menuChoiceType == MenuChoiceType.NightAbility2)
                                        {
                                            sprite = roleData.abilityIcon2;
                                        }
                                    }
                                    else if (data.bHasNecronomicon && ModSettings.GetString("Display Mode") == "Role + Book Icon") 
                                    {
                                        sprite = Service.Game.PlayerEffects.GetEffect(EffectType.NECRONOMICON).sprite;
                                    }
                                    if (data.teammateRole == Role.NECROMANCER) 
                                    {
                                        Console.WriteLine($"TOSTVIRI values: {ModSettings.GetBool("Role Revival Icon")}, {data.menuChoiceType}");
                                    }
                                    if (ModSettings.GetBool("Role Revival Icon") && (data.teammateRole == Role.NECROMANCER || data.teammateRole == Role.RETRIBUTIONIST) && data.menuChoiceType == MenuChoiceType.NightAbility2) 
                                    {
                                        //Find who is been revived
                                        int target1 = -1;
                                        int counter = -1;
                                        bool found = false;
                                        foreach (List<Image> imgs in Manager.Instance.visits.Values)
                                        {
                                            counter++;
                                            for (int i = 0; i < imgs.Count; i++)
                                            {
                                                if (imgs[i] != null && imgs[i].gameObject.name == $"{data.teammateRole}({data.teammatePosition})S")
                                                {
                                                    target1 = counter;
                                                    break;
                                                }
                                            }
                                            if (target1 != -1) break;
                                        }
                                        //Checks if revival target was found
                                        if (target1 != -1)
                                        {
                                            Role revivalRole = Manager.Instance.Panel.playerListPlayers[target1].playerRole;
                                            //Check if is valid know role
                                            if (revivalRole != Role.NONE && revivalRole != Role.STONED && revivalRole != Role.HIDDEN)
                                            {
                                                UIRoleData.UIRoleDataInstance revivalRoleData = panel.roleData.roleDataList.Find((UIRoleData.UIRoleDataInstance d) => d.role == revivalRole);
                                                if (revivalRoleData != null && revivalRoleData.roleIcon != null)
                                                {
                                                    sprite = revivalRoleData.roleIcon;
                                                    found = true;
                                                }
                                                else { Console.WriteLine("TOSTVIRI revival role icon not found"); }
                                            }
                                            else { Console.WriteLine("TOSTVIRI invalid revival role"); }
                                        }
                                        else { Console.WriteLine("TOSTVIRI summoning target not found"); }
                                        //If unable to get icon of the role been revived, put ability 2 icon
                                        if (!found) 
                                        {
                                            sprite = roleData.abilityIcon2;
                                        }
                                    }
                                    else if ((data.teammateRole == Role.WITCH || data.teammateRole == Role.NECROMANCER || data.teammateRole == Role.RETRIBUTIONIST || data.teammateRole == Role.POISONER) && data.menuChoiceType == MenuChoiceType.NightAbility2)
                                    {
                                        Console.WriteLine("TOSTVI ability 2 case scenario");
                                        sprite = roleData.abilityIcon2;
                                    }
                                    if (data.menuChoiceType == MenuChoiceType.SpecialAbility)
                                    {
                                        Console.WriteLine("TOSTVI special ability case scenario");
                                        sprite = roleData.specialAbilityIcon;
                                    }
                                    Console.WriteLine("TOSTVI starting the request");
                                    switch (data.menuChoiceType)
                                    {
                                        case MenuChoiceType.NightAbility:
                                            Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility, data.teammateTargetingPosition1, sprite, data.teammateRole, data.teammatePosition);
                                            break;
                                        case MenuChoiceType.NightAbility2:
                                            Manager.Instance.ChangeTarget(MenuChoiceType.NightAbility2, data.teammateTargetingPosition2, sprite, data.teammateRole, data.teammatePosition);
                                            break;
                                        case MenuChoiceType.SpecialAbility:
                                            if (data.teammateTargetingPosition1 != -1)
                                            {
                                                Manager.Instance.ChangeTarget(MenuChoiceType.SpecialAbility, data.teammateTargetingPosition1, sprite, data.teammateRole, data.teammatePosition);
                                            }
                                            if (data.teammateTargetingPosition2 != -1)
                                            {
                                                Manager.Instance.ChangeTarget(MenuChoiceType.SpecialAbility, data.teammateTargetingPosition2, sprite, data.teammateRole, data.teammatePosition);
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("TOSTVI There was no panel");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("TOSTVI Error! " + e.Message);
                }
            }
        }
    }
    [HarmonyPatch(typeof(GameObservations))]
    internal class Cleaner
    {
        static bool hooked = false;
        [HarmonyPatch("HandleGameInfo")]
        [HarmonyPostfix]
        static void ClearIcons(GameInfoObservation gameInfoObservation)
        {
            if (gameInfoObservation.Data.gamePhase == GamePhase.PLAY && !hooked)
            {
                hooked = true;
                Console.WriteLine("TOSTVI adding hook");
                Service.Game.Sim.simulation.incomingChatLogMessage.OnChanged += Interpreter.HandleMessages;
            }
            else if (gameInfoObservation.Data.gamePhase != GamePhase.PLAY && hooked) 
            {
                hooked = false;
                Console.WriteLine("TOSTVI removing hook");
                Service.Game.Sim.simulation.incomingChatLogMessage.OnChanged -= Interpreter.HandleMessages;
            }
            if (gameInfoObservation.Data.gamePhase == GamePhase.PLAY && gameInfoObservation.Data.playPhase != PlayPhase.NIGHT)
            {
                Console.WriteLine($"TOSTVI Requesting icons clear because of playphase: " + gameInfoObservation.Data.playPhase);
                Manager.Instance.Clear();
            }
        }
    }

    internal class Manager
    {
        internal Dictionary<int, List<Image>> visits = new Dictionary<int, List<Image>>();
        TosAbilityPanel _panel = null;
        static Manager _instance = null;
        internal static Manager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Manager();
                }
                return _instance;
            }
        }
        internal TosAbilityPanel Panel
        {
            get
            {
                if (_panel == null)
                {
                    foreach (TosAbilityPanel pan in UnityEngine.Object.FindObjectsOfType<TosAbilityPanel>())
                    {
                        if (pan.playerListPlayers.Count > 0)
                        {
                            _panel = pan;
                            visits = new Dictionary<int, List<Image>>();
                            for (int i = 0; i < pan.playerListPlayers.Count; i++)
                            {
                                visits.Add(i, new List<Image>());
                            }
                            break;
                        }
                    }
                }
                return _panel;
            }
        }
        internal void AddTarget(MenuChoiceType abilityId, int targetPlayer, Sprite sprite, Role role, int actorPlayer)
        {
            TosAbilityPanelListItem tagetPlayerPanel = Panel.playerListPlayers[targetPlayer];
            string targetName = $"{role}({actorPlayer})";
            if (abilityId == MenuChoiceType.NightAbility2)
            {
                targetName += "2";
            }
            else if (abilityId == MenuChoiceType.SpecialAbility)
            {
                targetName += "S";
            }
            Image image = UnityEngine.Object.Instantiate(Panel.playerListPlayers[targetPlayer].effectImage2);
            image.gameObject.name = targetName;
            image.name = targetName;
            if (Panel.playerListPlayers[targetPlayer].roleIconButton.isActiveAndEnabled)
            {
                image.transform.SetParent(tagetPlayerPanel.roleIconButton.transform);
            }
            else
            {
                image.transform.SetParent(tagetPlayerPanel.playerNameButton.transform);
            }
            Console.WriteLine("TOSTVI adding icon " + image.name);
            image.transform.localScale = Vector3.one;
            image.sprite = sprite;
            visits[targetPlayer].Add(image);
            image.transform.localPosition = new Vector3(80 + 32 * (visits[targetPlayer].Count - 1), 0, 0);
            image.gameObject.SetActive(true);
        }
        internal void CancelTarget(MenuChoiceType abilityId, Role role, int actorPlayer)
        {
            bool removed = false;
            string roleName = $"{role}({actorPlayer})";
            if (abilityId == MenuChoiceType.NightAbility2)
            {
                roleName += "2";
            }
            else if (abilityId == MenuChoiceType.SpecialAbility)
            {
                roleName += "S";
            }
            Console.WriteLine("TOSTVID removal target: " + roleName);
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = 0; i < imgs.Count; i++)
                {
                    if (imgs[i].gameObject.name == roleName)
                    {
                        Image temp = imgs[i];
                        Console.WriteLine("TOSTVI removing " + temp.gameObject.name + " because of target change or cancel");
                        imgs.RemoveAt(i);
                        UnityEngine.Object.DestroyImmediate(temp);
                        removed = true;
                    }
                    if (removed && i < imgs.Count)
                    {
                        imgs[i].transform.localPosition -= new Vector3(32, 0, 0);
                    }
                }
                if (removed) break;
            }
        }
        internal void ChangeTarget(MenuChoiceType abilityId, int targetPlayer, Sprite sprite, Role role, int actorPlayer)
        {
            Console.WriteLine("TOSTVI requesting cancels for the change of target");
            switch (role)
            {
                case Role.POTIONMASTER:
                case Role.RITUALIST:
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                    CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    break;
                case Role.NECROMANCER:
                    if (abilityId == MenuChoiceType.NightAbility)
                    {
                        CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                        CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                        CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    }
                    else
                    {
                        CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                        CancelTarget(abilityId, role, actorPlayer);
                    }
                    break;
                case Role.ILLUSIONIST:
                case Role.POISONER:
                case Role.MEDUSA:
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                    break;
                case Role.COVENLEADER:
                    CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    break;
                default:
                    CancelTarget(abilityId, role, actorPlayer);
                    break;
            }
            Console.WriteLine("TOSTVI adding icon to new target");
            AddTarget(abilityId, targetPlayer, sprite, role, actorPlayer);
        }
        internal void Clear()
        {
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = imgs.Count - 1; i >= 0; i--)
                {
                    Image temp = imgs[i];
                    imgs.RemoveAt(i);
                    if (temp != null && temp.gameObject != null)
                    {
                        Console.WriteLine("TOSTIV deleting icon " + temp.gameObject.name);
                        temp.gameObject.SetActive(true);
                        UnityEngine.Object.DestroyImmediate(temp.gameObject);
                        
                    }
                }
            }
        }
    }
}
