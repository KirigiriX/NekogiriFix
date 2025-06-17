using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using Newtonsoft.Json;
using Steamworks.Data;

namespace NekogiriMod
{
    [BepInPlugin("kirigiri.repo.nekogiri", "Nekogiri", "1.1.0.2")]
    public class NekogiriMod : BaseUnityPlugin
    {
        private void Awake()
        {
            // Set up plugin logging
            Logger.LogInfo(@"
 ██ ▄█▀ ██▓ ██▀███   ██▓  ▄████  ██▓ ██▀███   ██▓
 ██▄█▒ ▓██▒▓██ ▒ ██▒▓██▒ ██▒ ▀█▒▓██▒▓██ ▒ ██▒▓██▒
▓███▄░ ▒██▒▓██ ░▄█ ▒▒██▒▒██░▄▄▄░▒██▒▓██ ░▄█ ▒▒██▒
▓██ █▄ ░██░▒██▀▀█▄  ░██░░▓█  ██▓░██░▒██▀▀█▄  ░██░
▒██▒ █▄░██░░██▓ ▒██▒░██░░▒▓███▀▒░██░░██▓ ▒██▒░██░
▒ ▒▒ ▓▒░▓  ░ ▒▓ ░▒▓░░▓   ░▒   ▒ ░▓  ░ ▒▓ ░▒▓░░▓  
░ ░▒ ▒░ ▒ ░  ░▒ ░ ▒░ ▒ ░  ░   ░  ▒ ░  ░▒ ░ ▒░ ▒ ░
░ ░░ ░  ▒ ░  ░░   ░  ▒ ░░ ░   ░  ▒ ░  ░░   ░  ▒ ░
░  ░    ░     ░      ░        ░  ░     ░      ░  
                                                 
");
            Logger.LogInfo("Nekogiri has loaded!");

            // Create a Harmony instance and apply the patch
            var harmony = new Harmony("kirigiri.repo.nekogiri");
            harmony.PatchAll();  // Automatically patch all methods that have the PatchAttribute

            // Optionally log that the patch has been applied
            Logger.LogInfo("Made with <3 By Kirigiri \nhttps://discord.gg/P5cDx4Fyfc");
        }

        // The custom method to replace the original Start method
        private void CustomStart()
        {
            Logger.LogInfo("Custom Start method executed!");

            string configFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Kirigiri.ini");

            // Default App IDs
            string appIdRealtime = "99515094-45b7-4c98-ab70-a448c548c83d";
            string appIdVoice = "dfc40a01-c3a4-4395-9707-b71118816d2b";
            bool useDefaultRepoServers = false;

            if (File.Exists(configFilePath))
            {
                try
                {
                    var settings = File.ReadAllLines(configFilePath)
                                       .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                                       .Select(line => line.Split('='))
                                       .Where(parts => parts.Length == 2)
                                       .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    // Check for UseDefaultRepoServers flag
                    if (settings.TryGetValue("UseDefaultRepoServers", out string useDefaultValue) && useDefaultValue == "1")
                    {
                        useDefaultRepoServers = true;
                    }

                    // If not using defaults, try to get AppIdRealtime and AppIdVoice from INI file
                    if (!useDefaultRepoServers)
                    {
                        if (settings.TryGetValue("AppIdRealtime", out string iniAppIdRealtime) && !string.IsNullOrWhiteSpace(iniAppIdRealtime))
                        {
                            appIdRealtime = iniAppIdRealtime;
                            Debug.Log($"[Photon] AppIdRealtime from INI: {appIdRealtime}");
                        }

                        if (settings.TryGetValue("AppIdVoice", out string iniAppIdVoice) && !string.IsNullOrWhiteSpace(iniAppIdVoice))
                        {
                            appIdVoice = iniAppIdVoice;
                            Debug.Log($"[Photon] AppIdVoice from INI: {appIdVoice}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error reading INI file, using default App IDs. Details: {ex.Message}");
                }
            }
            else
            {
                Logger.LogWarning($"INI file not found at {configFilePath}, using default App IDs.");
                useDefaultRepoServers = true; // Use default if the INI file is missing
            }

            // Apply the final AppId settings based on the condition
            SemiLogger.LogAxel("PhotonSetAppId", null, null);
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = appIdRealtime;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice = appIdVoice;
        }





        // Custom method to initialize Steam with a dynamic App ID from the INI file
        private void CustomSteamAppID()
        {
            Logger.LogInfo("Custom Steam AppID method executed!");
            string configFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Kirigiri.ini");

            uint appId = 3241660U; // Default value for AppId if not found
            bool useDefaultRepoServers = false;

            try
            {
                if (File.Exists(configFilePath))
                {
                    // Read all lines from the INI file
                    var settings = File.ReadAllLines(configFilePath)
                                       .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                                       .Select(line => line.Split('='))
                                       .Where(parts => parts.Length == 2)
                                       .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    // Check for UseDefaultRepoServers flag
                    if (settings.TryGetValue("UseDefaultRepoServers", out string useDefaultValue) && useDefaultValue == "1")
                    {
                        useDefaultRepoServers = true;
                        Logger.LogInfo("Using default R.E.P.O. servers !.");
                    }

                    // If not using defaults, try to get SteamAppId from the INI file
                    if (!useDefaultRepoServers)
                    {
                        if (settings.ContainsKey("SteamAppId"))
                        {
                            // Try to parse the App ID from the file, if available
                            if (uint.TryParse(settings["SteamAppId"], out uint parsedAppId))
                            {
                                appId = parsedAppId;
                            }
                            else
                            {
                                Logger.LogWarning("Invalid SteamAppId in the INI file, defaulting to 3241660.");
                            }
                        }
                        else
                        {
                            Logger.LogWarning("SteamAppId not found in the INI file, defaulting to 3241660.");
                        }
                    }
                }
                else
                {
                    Logger.LogWarning($"Settings file not found at {configFilePath}. Using default App ID 3241660.");
                    useDefaultRepoServers = true; // Use default if the INI file is missing
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading SteamAppId from INI: {ex.Message}. Defaulting to App ID 3241660.");
            }

            // Initialize Steam client with the dynamic AppId
            SteamClient.Init(appId, true);
            Logger.LogInfo($"Steam client initialized with AppId {appId}");
        }


        public void CustomAuth()
        {
            Logger.LogInfo("Custom Auth method executed!");
            string configFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Kirigiri.ini");
            string authSetting = "Steam"; // Default value for Auth setting if not found or invalid
            bool useDefaultRepoServers = false;

            try
            {
                if (File.Exists(configFilePath))
                {
                    // Read all lines from the INI file
                    var settings = File.ReadAllLines(configFilePath)
                                       .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                                       .Select(line => line.Split('='))
                                       .Where(parts => parts.Length == 2)
                                       .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    // Check for UseDefaultRepoServers flag
                    if (settings.TryGetValue("UseDefaultRepoServers", out string useDefaultValue) && useDefaultValue == "1")
                    {
                        useDefaultRepoServers = true;
                    }

                    // If not using defaults, try to get Auth setting from the INI file
                    if (!useDefaultRepoServers)
                    {
                        if (settings.ContainsKey("Auth"))
                        {
                            authSetting = settings["Auth"];
                        }
                        else
                        {
                            Logger.LogWarning("Auth setting not found in the INI file, defaulting to 'Steam'.");
                        }
                    }
                }
                else
                {
                    Logger.LogWarning($"Settings file not found at {configFilePath}. Using default Auth value 'Steam'.");
                    useDefaultRepoServers = true; // Use default if the INI file is missing
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading Auth setting from INI: {ex.Message}. Defaulting to 'Steam'.");
            }

            // Map the string to the corresponding CustomAuthenticationType
            PhotonNetwork.AuthValues = new AuthenticationValues();
            PhotonNetwork.AuthValues.UserId = SteamClient.SteamId.ToString();

            CustomAuthenticationType authType = CustomAuthenticationType.Steam; // Default to Steam

            // Check the Auth setting and set the corresponding authentication type
            switch (authSetting.ToLower())
            {
                case "custom":
                    authType = CustomAuthenticationType.Custom;
                    break;
                case "steam":
                    authType = CustomAuthenticationType.Steam;
                    break;
                case "facebook":
                    authType = CustomAuthenticationType.Facebook;
                    break;
                case "oculus":
                    authType = CustomAuthenticationType.Oculus;
                    break;
                case "playstation4":
                    authType = CustomAuthenticationType.PlayStation4;
                    break;
                case "xbox":
                    authType = CustomAuthenticationType.Xbox;
                    break;
                case "viveport":
                    authType = CustomAuthenticationType.Viveport;
                    break;
                case "nintendoswitch":
                    authType = CustomAuthenticationType.NintendoSwitch;
                    break;
                case "playstation5":
                    authType = CustomAuthenticationType.PlayStation5;
                    break;
                case "epic":
                    authType = CustomAuthenticationType.Epic;
                    break;
                case "facebookgaming":
                    authType = CustomAuthenticationType.FacebookGaming;
                    break;
                case "none":
                default:
                    authType = CustomAuthenticationType.None;
                    break;
            }

            PhotonNetwork.AuthValues.AuthType = authType;

            // Add the Auth parameter (e.g., the Steam ticket)
            string value = this.GetSteamAuthTicket(out this.steamAuthTicket);
            PhotonNetwork.AuthValues.AddAuthParameter("ticket", value);

            Logger.LogInfo($"Patched Auth to {PhotonNetwork.AuthValues.AuthType}!");
        }


        private void WelcomeMessage()
        {
            string configFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Kirigiri.ini");

            try
            {
                if (File.Exists(configFilePath))
                {
                    // Read all lines from the INI file while preserving sections and comments
                    var lines = File.ReadAllLines(configFilePath).ToList();
                    bool welcomeReadUpdated = false;

                    for (int i = 0; i < lines.Count; i++)
                    {
                        // Look for the line containing "FirstLaunch"
                        if (lines[i].StartsWith("FirstLaunch"))
                        {
                            // If FirstLaunch is 1, show the message and update to 0
                            if (lines[i].Contains("FirstLaunch=1"))
                            {
                                // Show the welcome message with the correct PagePopUp signature
                                MenuManager.instance.PagePopUp(
                                    "Nekogiri",
                                    UnityEngine.Color.magenta,
                                    "<size=20>This mod has been made by Kirigiri.\nMake sure to create an account on <color=#808080>https://www.photonengine.com/</color> and to fill the values inside the <color=#cc00ff>Kirigiri.ini</color> file !\nThis message will appear only once, Have fun !",
                                    "OK",
                                    true // richText enabled
                                );

                                // Update FirstLaunch to 0
                                lines[i] = "FirstLaunch=0";
                                welcomeReadUpdated = true;
                            }
                            break;
                        }
                    }

                    // If the FirstLaunch value was updated, write back the modified lines
                    if (welcomeReadUpdated)
                    {
                        File.WriteAllLines(configFilePath, lines);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading or updating Kirigiri.ini: {ex.Message}");
            }
        }




        // Patch the original Start method with the custom one
        [HarmonyPatch(typeof(DataDirector), "PhotonSetAppId")]
        public class NetworkConnectPatch
        {
            // Prefix is called before the original method is called
            // Suffix is called after the original method is executed

            [HarmonyPrefix]
            public static bool Prefix()
            {
                // Instead of the original Start method, call CustomStart
                Debug.Log("Patching DataDirector.PhotonSetAppId method.");
                new NekogiriMod().CustomStart();

                // Return false to skip the original Start method
                return true; // Skipping the original Start method
            }
        }

        [HarmonyPatch(typeof(MenuPageMain), "Start")]
        public class MenuPageMainPatch
        {
            // Prefix is called before the original method is called
            // Suffix is called after the original method is executed

            [HarmonyPrefix]
            public static bool Prefix()
            {
                // Instead of the original Start method, call WelcomeMessage
                new NekogiriMod().WelcomeMessage();

                // Return false to skip the original Start method
                return true; // Skipping the original Start method
            }
        }

        [HarmonyPatch(typeof(SteamManager), "Awake")]
        public class SteamManagerPatch
        {
            // Prefix is called before the original method is called
            // Suffix is called after the original method is executed

            [HarmonyPrefix]
            public static bool Prefix()
            {
                // Instead of the original Start method, call CustomSteamAppID
                Debug.Log("Patching SteamManager.Awake method.");
                new NekogiriMod().CustomSteamAppID();

                // Return false to skip the original Start method
                return true; // Skipping the original Start method
            }
        }

        [HarmonyPatch(typeof(SteamManager), "SendSteamAuthTicket")]
        public class SendSteamAuthTicketPatch
        {
            // Prefix is called before the original method is called
            // Suffix is called after the original method is executed

            [HarmonyPrefix]
            public static bool Prefix()
            {
                // Instead of the original Start method, call CustomAuth
                Debug.Log("Patching SteamManager.SendSteamAuthTicket method.");
                new NekogiriMod().CustomAuth();

                // Return false to skip the original Start method
                return false; // Skipping the original Start method
            }
        }

        private string GetSteamAuthTicket(out AuthTicket ticket)
        {
            Debug.Log("Getting Steam Auth Ticket...");
            ticket = SteamUser.GetAuthSessionTicket();
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < ticket.Data.Length; i++)
            {
                stringBuilder.AppendFormat("{0:x2}", ticket.Data[i]);
            }
            return stringBuilder.ToString();
        }

        internal AuthTicket steamAuthTicket;
    }
}
