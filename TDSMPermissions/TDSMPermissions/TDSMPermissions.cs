﻿using System;
using System.IO;
using System.Collections.Generic;

using Terraria_Server;
using Terraria_Server.Misc;
using Terraria_Server.Logging;
using Terraria_Server.Permissions;
using TDSMPermissions.Definitions;

using YaTools.Yaml;
using Terraria_Server.Plugins;

namespace TDSMPermissions
{
    public class TDSMPermissions : BasePlugin
    {
        /*
         * @Developers
         * 
         * Plugins need to be in .NET 4.0
         * Otherwise TDSM will be unable to load it. 
         */

        public Properties properties;
        public string pluginFolder;
        public string permissionsYML;

        public bool spawningAllowed = false;
        public bool tileBreakageAllowed = false;
        public bool explosivesAllowed = false;
        public static TDSMPermissions plugin;
        private List<Group> groups = new List<Group>();
        private Dictionary<string, User> users = new Dictionary<string, User>();
        private User currentUser = new User();
        private Group currentGroup;
        public string defaultGroup;
        private YamlScanner sc;
        private bool inUsers;
        private String currentUserName = null;

        private bool inGroups = false;

        protected override void Initialized(object state)
        {
            Name = "TDSMPermissions";
            Description = "Permissions for TDSM.";
            Author = "Malkierian";
            Version = "1";
            TDSMBuild = 32;

            plugin = this;

            pluginFolder = Statics.PluginPath + Path.DirectorySeparatorChar + "TDSMPermissions";
            permissionsYML = pluginFolder + Path.DirectorySeparatorChar + "permissions.yml";

            //Create folder if it doesn't exist
            CreateDirectory(pluginFolder);

            if (!File.Exists(permissionsYML))
                File.Create(permissionsYML).Close();

            //setup a new properties file
            //properties = new Properties(pluginFolder + Path.DirectorySeparatorChar + "tdsmplugin.properties");
            //properties.Load();
            //properties.pushData(); //Creates default values if needed. [Out-Dated]
            //properties.Save();

            //read properties data
            Node.isPermittedImpl = this.isPermitted;
        }

        protected override void Enabled()
        {
            ProgramLog.Log(base.Name + " enabled.");
            //Register Hooks
            //registerHook(Hooks.PLAYER_PRELOGIN);
            //registerHook(Hooks.PLUGINS_LOADED);

            Hook(HookPoints.PluginsLoaded, OnPluginsLoaded);

            //Add Commands
        }

        protected override void Disabled()
        {
            ProgramLog.Log(base.Name + " disabled.");
        }

        void OnPluginsLoaded(ref HookContext ctx, ref HookArgs.PluginsLoaded args)
        {
            LoadPerms();
        }

        //public override void onPlayerPreLogin(Terraria_Server.Events.PlayerLoginEvent Event)
        //{
        //	base.onPlayerPreLogin(Event);
        //}

        public void LoadPerms()
        {
            ProgramLog.Debug.Log("Loading Permissions");
            Token to;
            TextReader re = File.OpenText(permissionsYML);

            sc = new YamlScanner();
            sc.SetSource(re);

            inUsers = false;

            while ((to = sc.NextToken()) != Token.EndOfStream)
            {
                switch (to)
                {
                    case Token.TextContent:
                        {
                            switch (sc.TokenText)
                            {
                                case "info":
                                    {
                                        ProcessInfo();
                                        break;
                                    }
                                case "permissions":
                                    {
                                        //if (inUsers)
                                        //{
                                        //    ProcessUserPermissions();
                                        //}
                                        //else
                                        //{
                                        ProcessPermissions();
                                        //}
                                        break;
                                    }
                                case "inheritance":
                                    {
                                        ProcessInheritance();
                                        break;
                                    }
                                case "groups":
                                    {
                                        if (inUsers)
                                        {
                                            ProcessGroups();
                                        }
                                        break;
                                    }
                                case "users":
                                    {
                                        inUsers = true;
                                        ProgramLog.Debug.Log("Now parsing users");
                                        break;
                                    }
                                default:
                                    break;
                            }
                            break;
                        }
                    case Token.IndentSpaces:
                        {
                            ProcessIndent();
                            break;
                        }
                    case Token.Outdent:
                    case Token.ValueIndicator:
                    case Token.BlockSeqNext:
                    case Token.Comma:
                    case Token.Escape:
                    case Token.InconsistentIndent:
                    case Token.Unexpected:
                    case Token.DoubleQuote:
                    case Token.SingleQuote:
                    case Token.EscapedLineBreak:
                    default:
                        break;
                }
            }
            ProgramLog.Debug.Log("Permissions file loaded.");
        }

        private void ProcessGroups()
        {
            while (sc.NextToken() != Token.Outdent)
            {
                while (sc.NextToken() != Token.TextContent)
                {
                    if (sc.Token == Token.Outdent)
                        return;
                }
                foreach (Group group in groups)
                {
                    if (group.Name == sc.TokenText)
                    {
                        foreach (string node in group.permissions.Keys)
                        {
                            bool toggle = false;
                            group.permissions.TryGetValue(node, out toggle);
                            if (toggle)
                            {
                                currentUser.hasPerm.Add(node);
                            }
                            else
                            {
                                currentUser.notHasPerm.Add(node);
                            }
                        }
                    }
                }
                currentUser.group.Add(sc.TokenText);
            }
        }

        private void ProcessIndent()
        {
            string tokenText = sc.TokenText;
            if (sc.NextToken() == Token.IndentSpaces)
                tokenText += sc.TokenText;
            if (tokenText == "    ")
            {
                while (sc.NextToken() != Token.TextContent)
                {
                }
                if (!inUsers)
                {
                    currentGroup = new Group(sc.TokenText);
                }
                else
                {
                    currentUserName = sc.TokenText;
                    currentUser = new User();
                }
            }
        }

        private void ProcessInfo()
        {
            bool Default;
            string Prefix;
            string Suffix;
            Color color;
            while (sc.TokenText != "default")
            {
                sc.NextToken();
            }
            while (sc.NextToken() != Token.TextContent)
            { }
            Default = Convert.ToBoolean(sc.TokenText);
            if (Default)
            {
                defaultGroup = currentGroup.Name;
            }
            while (sc.TokenText != "prefix")
            {
                sc.NextToken();
            }
            while (sc.NextToken() != Token.TextContent)
            { }
            Prefix = sc.TokenText;
            while (sc.TokenText != "suffix")
            {
                sc.NextToken();
            }
            while (sc.NextToken() != Token.TextContent)
            { }
            Suffix = sc.TokenText;
            while (sc.TokenText != "color")
            {
                sc.NextToken();
                if (sc.TokenText == "")
                    break;
            }

            if (sc.TokenText == "")
                color = ChatColor.White; 
            else
                Color.TryParseRGB(sc.TokenText, out color);

            while (sc.NextToken() != Token.TextContent)
            { }
        }

        private void ProcessInheritance()
        {
            while (sc.NextToken() != Token.TextContent)
            {
                if (sc.Token == Token.Outdent)
                    return;
            }
            if (sc.TokenText == "permissions")
            {
                ProcessPermissions();
                return;
            }
            while (sc.NextToken() != Token.Outdent)
            {
                if (sc.Token == Token.TextContent)
                {
                    currentGroup.Inherits.Add(sc.TokenText);
                }
            }
        }

        private void ProcessPermissions()
        {
            while (sc.NextToken() != Token.Outdent)
            {
                while (sc.NextToken() != Token.TextContent)
                {
                    if (sc.Token == Token.Outdent)
                        return;
                }
                bool toggle;
                string tokenText;
                if (sc.TokenText.Contains("-"))
                {
                    toggle = false;
                    tokenText = sc.TokenText.Substring(1, sc.TokenText.Length - 1);
                }
                else
                {
                    toggle = true;
                    tokenText = sc.TokenText;
                }
                if (!inUsers)
                {
                    if (toggle)
                    {
                        if (tokenText == "*")
                        {
                            foreach (string s in Node.ActiveNodes)
                            {
                                currentGroup.permissions.Add(s, toggle);
                            }
                        }
                        else if (tokenText.Contains("*"))
                        {
                            string temp = tokenText.Remove(tokenText.Length - 2);
                            foreach (string s in Node.ActiveNodes)
                            {
                                if (s.Contains(temp))
                                {
                                    currentGroup.permissions.Add(s, toggle);
                                }
                            }
                        }
                    }
                    currentGroup.permissions.Add(tokenText, toggle);
                }
                else
                {
                    if (toggle)
                    {
                        if (tokenText == "*")
                        {
                            foreach (string s in Node.ActiveNodes)
                            {
                                currentUser.hasPerm.Add(s);
                            }
                        }
                        else if (tokenText.Contains("*"))
                        {
                            string temp = tokenText.Remove(tokenText.Length - 2);
                            foreach (string s in Node.ActiveNodes)
                            {
                                if (s.Contains(temp))
                                {
                                    currentUser.hasPerm.Add(s);
                                }
                            }
                        }
                    }
                    else
                    {
                        currentUser.notHasPerm.Add(tokenText);
                    }
                }
            }
            if (!inUsers)
            {
                groups.Add(currentGroup);
            }
            else
            {
                ProgramLog.Debug.Log("Player " + currentUserName + " has permissions:");
                currentUser.hasPerm.ForEach(delegate(string name)
                {
                    ProgramLog.Debug.Log(name);
                });
                users.Add(currentUserName, currentUser);
            }
        }

        private void ProcessUserPermissions()
        {

        }

        public bool isPermitted(Node node, Player player)
        {
            return false;
        }

        private static void CreateDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

    }
}