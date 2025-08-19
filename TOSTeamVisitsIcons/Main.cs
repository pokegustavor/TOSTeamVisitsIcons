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
                DictionaryExtensions.SetValue(Settings.SettingsCache, "Role Revival Icon", ModSettings.GetBool("Role Revival Icon", "pokegustavo.FactionVisits"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("The rainbow faction crashed the mod. Contact pokegustavo. Error: " + ex.Message);
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
            },
            {
                "Revival Icon",
                false
            }
        };

        public ModSettings.DropdownSetting DisplayMode 
        {
            get
            {
                ModSettings.DropdownSetting dropdownSetting = new ModSettings.DropdownSetting
                {
                    Name = "Display Mode",
                    Description = "When showing the icons of your teammates you can choose to show the icon of their role (with some exceptions for roles that have 2 targets), " +
                    "you can choose to show the ability icon (with book holder showing the book icon), or choose to combine role icon and book icon.",
                    Options = DisplaySettings,
                    AvailableInGame = false,
                    Available = true,
                    OnChanged = delegate (string s)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Display Mode", s);
                    }
                };
                return dropdownSetting;
            }
        }

        public ModSettings.CheckboxSetting RevivalIcon 
        {
            get 
            {
                ModSettings.CheckboxSetting checkboxSetting = new ModSettings.CheckboxSetting 
                {
                    Name = "Role Revival Icon",
                    Description = "If enabled whenever a necromancer or retri in your team revives someone the role icon of the revived person will be shown visiting the target" +
                    "instead of the secondary ability icon.",
                    DefaultValue = false,
                    AvailableInGame = false,
                    Available = true,
                    OnChanged = delegate (bool b)
                    {
                        DictionaryExtensions.SetValue(SettingsCache, "Role Revival Icon", b);
                    }
                };
                return checkboxSetting;

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
