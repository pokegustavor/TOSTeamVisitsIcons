using SalemModLoaderUI;
using Server.Shared.Extensions;
using Services;
using SML;
using System;
using System.Collections.Generic;

namespace TOSTeamVisitsIcons
{
    [Mod.SalemMod]
    public class Main
    {
        public void Start()
        {
            Console.WriteLine("Modding time!");
            try
            {
                DictionaryExtensions.SetValue(Settings.SettingsCache, "Display Mode", ModSettings.GetString("Display Mode", "pokegustavo.FactionVisits"));
            }
            catch 
            {
                Console.WriteLine("The rainbow faction crashed the mod. Contact pokegustavo");
            }
        }
    }

    [DynamicSettings]
    public class Settings
    {
        public static Dictionary<string, object> SettingsCache = new Dictionary<string, object>
        {
            {
                "Display Mode",
                "Role Icon"
            }
        };

        public ModSettings.DropdownSetting DisplayMode 
        {
            get
            {
                ModSettings.DropdownSetting dropdownSetting = new ModSettings.DropdownSetting();
                dropdownSetting.Name = "Display Mode";
                dropdownSetting.Description = "When showing the icons of your teammates you can choose to show the icon of their role (with some exceptions for roles that have 2 targets), " +
                    "you can choose to show the ability icon (with book holder showing the book icon), or choose to combine role icon and book icon.";
                dropdownSetting.Options = DisplaySettings;
                dropdownSetting.AvailableInGame = false;
                dropdownSetting.Available = true;
                dropdownSetting.OnChanged = delegate (string s)
                {
                    DictionaryExtensions.SetValue(Settings.SettingsCache, "Show Faction Color", s);
                };
                return dropdownSetting;
            }
        }

        private readonly List<string> DisplaySettings = new List<string>(3)
        {
            "Role Icon",
            "Ability Icon",
            "Role + Book Icon"
        };
    }
}
