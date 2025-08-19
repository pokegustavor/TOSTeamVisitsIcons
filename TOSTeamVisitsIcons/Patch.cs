using System;
using System.Collections.Generic;
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
                    Console.Write($"TOSTVI recieved message: player {data.teammatePosition + 1} (role {data.teammateRole}) has decided to");
                    if (data.bIsCancel)
                    {
                        Console.Write($"Cancel their night ability");
                    }
                    else if (data.bIsChangingTarget)
                    {
                        Console.Write($"Change their target to {data.teammateTargetingPosition1}");
                    }
                    else
                    {
                        Console.Write($"Target {data.teammateTargetingPosition1}");
                    }
                    Console.WriteLine($"They {(data.bHasNecronomicon ? "have" : "don't have")} the necronomicon!");
                    if (panel == null)
                    {
                        panel = UnityEngine.Object.FindObjectOfType<RoleCardPanel>();
                    }
                    if (panel != null)
                    {
                        roleData = panel.roleData.roleDataList.Find((UIRoleData.UIRoleDataInstance d) => d.role == data.teammateRole);
                        if (roleData != null)
                        {
                            if (roleData.roleIcon != null)
                            {
                                if (data.bIsCancel)
                                {
                                    Manager.Instance.CancelTarget(data.menuChoiceType, data.teammateRole);
                                }
                                else
                                {
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
                                    if ((data.teammateRole == Role.WITCH || data.teammateRole == Role.NECROMANCER || data.teammateRole == Role.RETRIBUTIONIST || data.teammateRole == Role.POISONER) && data.menuChoiceType == MenuChoiceType.NightAbility2)
                                    {
                                        sprite = roleData.abilityIcon2;
                                    }
                                    if (data.menuChoiceType == MenuChoiceType.SpecialAbility)
                                    {
                                        sprite = roleData.specialAbilityIcon;
                                    }
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
                    Console.WriteLine("TeamVisitsError! " + e.Message);
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
        Dictionary<int, List<Image>> visits = new Dictionary<int, List<Image>>();
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
        TosAbilityPanel Panel
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
            TosAbilityPanelListItem actorPlayerPanel = Panel.playerListPlayers[actorPlayer];
            string targetName = role.ToString();
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
        internal void CancelTarget(MenuChoiceType abilityId, Role role)
        {
            bool removed = false;
            string roleName = role.ToString();
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
                    CancelTarget(MenuChoiceType.NightAbility, role);
                    CancelTarget(MenuChoiceType.NightAbility2, role);
                    CancelTarget(MenuChoiceType.SpecialAbility, role);
                    break;
                case Role.NECROMANCER:
                    if (abilityId == MenuChoiceType.NightAbility)
                    {
                        CancelTarget(MenuChoiceType.NightAbility, role);
                        CancelTarget(MenuChoiceType.NightAbility2, role);
                        CancelTarget(MenuChoiceType.SpecialAbility, role);
                    }
                    else
                    {
                        CancelTarget(MenuChoiceType.NightAbility, role);
                        CancelTarget(abilityId, role);
                    }
                    break;
                case Role.ILLUSIONIST:
                case Role.POISONER:
                case Role.MEDUSA:
                    CancelTarget(MenuChoiceType.NightAbility, role);
                    CancelTarget(MenuChoiceType.NightAbility2, role);
                    break;
                case Role.COVENLEADER:
                    CancelTarget(MenuChoiceType.SpecialAbility, role);
                    CancelTarget(MenuChoiceType.NightAbility, role);
                    break;
                default:
                    CancelTarget(abilityId, role);
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
