using System;
using System.Collections.Generic;
using Game.Interface;
using Game.Simulation;
using HarmonyLib;
using Server.Shared.Info;
using Server.Shared.Messages;
using Server.Shared.State;
using Server.Shared.State.Chat;
using UnityEngine;
using UnityEngine.UI;

namespace TOSTeamVisitsIcons
{
    [HarmonyPatch(typeof(HudFactionTargetSelectionChatLogShared))]
    internal class Interpreter
    {
        static RoleCardPanel panel = null;
        [HarmonyPatch("HandleMessage")]
        [HarmonyPostfix]
        static void HandleMessages(ChatLogMessage chatLogMessage, bool isValidMessage)
        {
            if (isValidMessage)
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
                                    switch (data.menuChoiceType)
                                    {
                                        case MenuChoiceType.NightAbility:
                                            Manager.Instance.CancelTarget(Manager.AbilityType.nightAbility, data.teammateRole);
                                            break;
                                        case MenuChoiceType.NightAbility2:
                                            Manager.Instance.CancelTarget(Manager.AbilityType.nightAbility2, data.teammateRole);
                                            break;
                                        case MenuChoiceType.SpecialAbility:
                                            Manager.Instance.CancelTarget(Manager.AbilityType.specialAbility, data.teammateRole);
                                            break;
                                    }
                                }
                                else if (data.bIsChangingTarget)
                                {
                                    switch (data.menuChoiceType)
                                    {
                                        case MenuChoiceType.NightAbility:
                                            Manager.Instance.ChangeTarget(Manager.AbilityType.nightAbility,data.teammateTargetingPosition1,roleData.roleIcon,data.teammateRole);
                                            break;
                                        case MenuChoiceType.NightAbility2:
                                            Manager.Instance.ChangeTarget(Manager.AbilityType.nightAbility2, data.teammateTargetingPosition2, roleData.roleIcon, data.teammateRole);
                                            break;
                                        case MenuChoiceType.SpecialAbility:
                                            if (data.teammateTargetingPosition1 != -1)
                                            {
                                                Manager.Instance.ChangeTarget(Manager.AbilityType.specialAbility, data.teammateTargetingPosition1, roleData.roleIcon, data.teammateRole);
                                            }
                                            if (data.teammateTargetingPosition2 != -1)
                                            {
                                                Manager.Instance.ChangeTarget(Manager.AbilityType.specialAbility, data.teammateTargetingPosition2, roleData.roleIcon, data.teammateRole);
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (data.menuChoiceType)
                                    {
                                        case MenuChoiceType.NightAbility:
                                            Manager.Instance.AddTarget(Manager.AbilityType.nightAbility, data.teammateTargetingPosition1, roleData.roleIcon, data.teammateRole);
                                            break;
                                        case MenuChoiceType.NightAbility2:
                                            Manager.Instance.AddTarget(Manager.AbilityType.nightAbility2, data.teammateTargetingPosition2, roleData.roleIcon, data.teammateRole);
                                            break;
                                        case MenuChoiceType.SpecialAbility:
                                            if (data.teammateTargetingPosition1 != -1)
                                            {
                                                Manager.Instance.AddTarget(Manager.AbilityType.specialAbility, data.teammateTargetingPosition1, roleData.roleIcon, data.teammateRole);
                                            }
                                            if (data.teammateTargetingPosition2 != -1)
                                            {
                                                Manager.Instance.AddTarget(Manager.AbilityType.specialAbility, data.teammateTargetingPosition2, roleData.roleIcon, data.teammateRole);
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
        [HarmonyPatch("HandleGameInfo")]
        [HarmonyPostfix]
        static void ClearIcons(GameInfoObservation gameInfoObservation)
        {
            if (gameInfoObservation.Data.playPhase != PlayPhase.NIGHT)
            {
                Console.Write($"TOSTVI Requesting icons clear because of playphase: " + gameInfoObservation.Data.playPhase);
                Manager.Instance.Clear();
            }
        }
    }

    internal class Manager
    {
        internal enum AbilityType
        {
            nightAbility,
            nightAbility2,
            specialAbility
        }
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
        internal void AddTarget(AbilityType abilityId, int targetPlayer, Sprite sprite, Role role)
        {
            TosAbilityPanelListItem tagetPlayerPanel = Panel.playerListPlayers[targetPlayer];
            string targetName = role.ToString();
            if (abilityId == AbilityType.nightAbility2)
            {
                targetName += "2";
            }
            else if (abilityId == AbilityType.specialAbility)
            {
                targetName += "S";
            }
            foreach (Image img in visits[targetPlayer])
            {
                if (img.gameObject.name == targetName) return;
            }
            Image image = UnityEngine.Object.Instantiate(Panel.playerListPlayers[targetPlayer].effectImage2);
            image.gameObject.name = targetName;
            if (Panel.playerListPlayers[targetPlayer].roleIconButton.isActiveAndEnabled)
            {
                image.transform.SetParent(tagetPlayerPanel.roleIconButton.transform);
            }
            else
            {
                image.transform.SetParent(tagetPlayerPanel.playerNameButton.transform);
            }
            image.transform.localScale = Vector3.one;
            image.sprite = sprite;
            visits[targetPlayer].Add(image);
            image.transform.localPosition = new Vector3(80 + 32 * (visits[targetPlayer].Count - 1), 0, 0);
            image.gameObject.SetActive(true);
        }
        internal void CancelTarget(AbilityType abilityId, Role role)
        {
            bool removed = false;
            string roleName = role.ToString();
            if (abilityId == AbilityType.nightAbility2)
            {
                roleName += "2";
            }
            else if (abilityId == AbilityType.specialAbility)
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
        internal void ChangeTarget(AbilityType abilityId, int targetPlayer, Sprite sprite, Role role)
        {
            switch(role) 
            {
                case Role.POTIONMASTER:
                case Role.RITUALIST:
                    CancelTarget(AbilityType.nightAbility, role);
                    CancelTarget(AbilityType.nightAbility2, role);
                    CancelTarget(AbilityType.specialAbility, role);
                    break;
                case Role.NECROMANCER:
                case Role.RETRIBUTIONIST:
                    if (abilityId == AbilityType.nightAbility)
                    {
                        CancelTarget(AbilityType.nightAbility, role);
                        CancelTarget(AbilityType.nightAbility2, role);
                        CancelTarget(AbilityType.specialAbility, role);
                    }
                    else 
                    {
                        CancelTarget(AbilityType.nightAbility, role);
                        CancelTarget(abilityId, role);
                    }
                    break;
                case Role.ILLUSIONIST:
                case Role.POISONER:
                case Role.MEDUSA:
                    CancelTarget(AbilityType.nightAbility, role);
                    CancelTarget(AbilityType.nightAbility2, role);
                    break;
                case Role.COVENLEADER:
                    CancelTarget(AbilityType.specialAbility, role);
                    CancelTarget(AbilityType.nightAbility, role);
                    break;
                default:
                    CancelTarget(abilityId, role);
                    break;
            }
            AddTarget(abilityId,targetPlayer,sprite,role);
        }
        internal void Clear()
        {
            foreach (List<Image> imgs in visits.Values)
            {
                for (int i = imgs.Count - 1; i >= 0; i--)
                {
                    Image temp = imgs[i];
                    imgs.RemoveAt(i);
                    if (temp != null) UnityEngine.Object.DestroyImmediate(temp.gameObject);
                }
            }
        }
    }
}
