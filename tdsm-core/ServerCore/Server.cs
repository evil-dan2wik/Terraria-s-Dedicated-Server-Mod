﻿using Microsoft.Xna.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using tdsm.api;
using tdsm.api.Misc;
using tdsm.api.Plugin;
using tdsm.core.Logging;
using tdsm.core.Messages.Out;
using tdsm.core.Misc;
using Terraria;

namespace tdsm.core.ServerCore
{
    public static class Server
    {
        //public static ServerSlot[] slots = new ServerSlot[256];
        //public static ProgramThread updateThread = null;
        public static int OverlimitSlots = 1;

        private static System.Collections.Generic.List<String> _connections = new System.Collections.Generic.List<String>();
        public static int UniqueConnections
        {
            get { return _connections.Count; }
        }

        public static void AddUniqueConnection(string name, string ip)
        {
            var key = name + ip;
            lock (_connections)
            {
                if (!_connections.Contains(key))
                    _connections.Add(key);
            }
        }

        public static void SafeClose(this Socket socket)
        {
            if (socket == null) return;

            try
            {
                socket.Close();
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        public static void SafeShutdown(this Socket socket)
        {
            if (socket == null) return;

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        public static void ServerLoop()
        {
            if (Main.rand == null) Main.rand = new Random((new Random()).Next(Int32.MinValue, Int32.MaxValue));
            if (WorldGen.genRand == null) WorldGen.genRand = new Random((new Random()).Next(Int32.MinValue, Int32.MaxValue));

            //updateThread = new ProgramThread("Updt", UpdateLoop);

            Main.myPlayer = 255;
            Main.player[255].whoAmi = 255;
            Netplay.serverIP = IPAddress.Any;
            Netplay.serverListenIP = Netplay.serverIP;
            Netplay.disconnect = false;

            //			for (int i = 0; i < 256; i++)
            //			{
            ////                Netplay.slots[i] = new ServerSlot();
            //                Netplay.slots[i].whoAmI = i;
            //				Netplay.slots[i].Reset();
            //			}
            //Init();

            Netplay.tcpListener = new TcpListener(Netplay.serverListenIP, Netplay.serverPort);

            try
            {
                Netplay.tcpListener.Start();
            }
            catch (Exception exception)
            {
                ProgramLog.Log("Error Starting the Server: {0}", exception);
                Netplay.disconnect = true;
            }

            if (!Netplay.disconnect)
            {
                //if (!updateThread.IsAlive) updateThread.Start();
                ProgramLog.Log("{0} {1}:{2}", "Started server on", Netplay.serverIP.ToString(), Netplay.serverPort);
                //                ProgramLog.Log("Loading Plugins...");
                //				PluginManager.LoadAllPlugins();
                //                ProgramLog.Log("Plugins Loaded: " + PluginManager.Plugins.Count.ToString());
                //Statics.serverStarted = true;
            }
            else
                return;

            SlotManager.Initialize(Main.maxPlayers, OverlimitSlots);

            Netplay.ServerUp = true;
            var serverSock = Netplay.tcpListener.Server;

            try
            {
                while (!Netplay.disconnect)
                {
                    Netplay.anyClients = ClientConnection.All.Count > 0; //clientList.Count > 0;

                    serverSock.Poll(500000, SelectMode.SelectRead);

                    if (Netplay.disconnect) break;

                    // Accept new clients
                    while (Netplay.tcpListener.Pending())
                    {
                        var client = Netplay.tcpListener.AcceptSocket();
                        var accepted = AcceptClient(client);
                        if (accepted)
                        {
                            Netplay.anyClients = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ProgramLog.Log("ServerLoop terminated with exception: {0}", e);
            }

            Netplay.anyClients = false;

            try
            {
                Netplay.tcpListener.Stop();
            }
            catch (SocketException) { }

            lock (ClientConnection.All)
            {
                var conns = ClientConnection.All.ToArray();

                foreach (var conn in conns)
                {
                    conn.Kick("Server is shutting down.");
                }
            }

            for (int i = 0; i < 255; i++)
            {
                try
                {
                    (tdsm.api.Callbacks.NetplayCallback.slots[i] as ServerSlot).Kick("Server is shutting down.");
                }
                catch { }
            }

            Thread.Sleep(1000);

            for (int i = 0; i < 255; i++)
            {
                try
                {
                    (tdsm.api.Callbacks.NetplayCallback.slots[i] as ServerSlot).Reset();
                }
                catch { }
            }

            //if (!WorldFile.SaveWorld_Version2 (World.SavePath, true))
            //ProgramLog.Log("Saving failed.  Quitting without saving.");

            Netplay.ServerUp = false;
        }

        static bool AcceptClient(Socket client)
        {
            client.NoDelay = true;

            string addr;
            try
            {
                var rep = client.RemoteEndPoint;
                if (rep != null)
                    addr = rep.ToString();
                else
                {
                    ProgramLog.Log("Accepted socket disconnected");
                    return false;
                }
            }
            catch (Exception e)
            {
                ProgramLog.Log("Accepted socket exception ({0})", HandleSocketException(e));
                return false;
            }

            try
            {
                ProgramLog.Log("{0} is connecting...", addr);
                var conn = new ClientConnection(client, -1); //ignore the warning
            }
            catch (SocketException)
            {
                client.SafeClose();
                ProgramLog.Log("{0} disconnected.", addr);
            }

            return true;
        }

        static string HandleSocketException(Exception e)
        {
            if (e is SocketException)
                return e.Message;
            if (e is System.IO.IOException)
                return e.Message;
            else if (e is ObjectDisposedException)
                return "Socket already disposed";
            else
                throw new Exception("Unexpected exception in socket handling code", e);
        }

        private static ProgramThread serverThread;

        public static void StartServer()
        {
            //var ctx = new HookContext
            //{
            //    Sender = new ConsoleSender(),
            //};

            //var args = new HookArgs.ServerStateChange
            //{
            //    ServerChangeState = ServerState.STARTING
            //};

            //HookPoints.ServerStateChange.Invoke(ref ctx, ref args);

            if (serverThread == null)
            {
                serverThread = new ProgramThread("Serv", ServerLoopLoop);
                serverThread.Start();
            }
            Netplay.disconnect = false;
        }

        public static void StopServer()
        {
            var ctx = new HookContext
            {
                Sender = HookContext.ConsoleSender
            };

            var args = new HookArgs.ServerStateChange
            {
                ServerChangeState = ServerState.Stopping
            };

            HookPoints.ServerStateChange.Invoke(ref ctx, ref args);

            //Statics.IsActive = Statics.keepRunning; //To keep console active & program alive upon restart;
            ProgramLog.Log("Disabling Plugins");
            PluginManager.DisablePlugins();

            Console.Write("Saving world...");
            WorldFile.saveWorld(false);

            Thread.Sleep(5000);
            ProgramLog.Log("Closing Connections...");
            Netplay.disconnect = true;
        }

        private static void ServerLoopLoop()
        {
            Terraria.Main.netMode = 2;
            Terraria.Main.menuMode = 14;

            while (true)
            {
                ServerLoop();
                while (Netplay.disconnect) Thread.Sleep(100);
            }
        }

        public static bool IsInitialised { get; private set; }

        public static void Init()
        {
            tdsm.api.Callbacks.NetplayCallback.slots = new ServerSlot[256];
            tdsm.api.Callbacks.NetplayCallback.CheckSectionMethod = CheckSection;
            for (int i = 0; i < 256; i++)
            {
                var slot = new ServerSlot()
                {
                    whoAmI = i
                };
                slot.Reset();
                tdsm.api.Callbacks.NetplayCallback.slots[i] = slot;
            }

            Ops = new DataRegister(System.IO.Path.Combine(Globals.DataPath, "ops.txt"));
            Bans = new DataRegister(System.IO.Path.Combine(Globals.DataPath, "bans.txt"));
            Whitelist = new DataRegister(System.IO.Path.Combine(Globals.DataPath, "whitelist.txt"));
            ItemRejections = new DataRegister(System.IO.Path.Combine(Globals.DataPath, "itemrejection.txt"));
            IsInitialised = true;
        }

        public static DataRegister Ops { get; private set; }
        public static DataRegister Bans { get; private set; }
        public static DataRegister Whitelist { get; private set; }
        public static DataRegister ItemRejections { get; private set; }
        public static bool WhitelistEnabled { get; set; }

        public static int GetSectionX(int x)
        {
            return x / 200;
        }

        public static int GetSectionY(int y)
        {
            return y / 150;
        }

        public static void ResetSections()
        {
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < Main.maxSectionsX; j++)
                {
                    for (int k = 0; k < Main.maxSectionsY; k++)
                        tdsm.api.Callbacks.NetplayCallback.slots[i].tileSection[j, k] = false;
                }
            }
        }

        public static void CheckSection(int who, Vector2 position)
        {
            int sectionX = Netplay.GetSectionX((int)(position.X / 16f));
            int sectionY = Netplay.GetSectionY((int)(position.Y / 16f));
            int num = 0;
            for (int i = sectionX - 1; i < sectionX + 2; i++)
            {
                for (int j = sectionY - 1; j < sectionY + 2; j++)
                {
                    if (i >= 0 && i < Main.maxSectionsX && j >= 0 && j < Main.maxSectionsY && !tdsm.api.Callbacks.NetplayCallback.slots[who].tileSection[i, j])
                    {
                        num++;
                    }
                }
            }
            if (num > 0)
            {
                int num2 = num;
                NewNetMessage.SendData(9, who, -1, Lang.inter[44], num2, 0f, 0f, 0f, 0);
                tdsm.api.Callbacks.NetplayCallback.slots[who].statusText2 = "is receiving tile data";
                tdsm.api.Callbacks.NetplayCallback.slots[who].statusMax += num2;
                for (int k = sectionX - 1; k < sectionX + 2; k++)
                {
                    for (int l = sectionY - 1; l < sectionY + 2; l++)
                    {
                        if (k >= 0 && k < Main.maxSectionsX && l >= 0 && l < Main.maxSectionsY && !tdsm.api.Callbacks.NetplayCallback.slots[who].tileSection[k, l])
                        {
                            NewNetMessage.SendSection(who, k, l, false);
                            NewNetMessage.SendData(11, who, -1, String.Empty, k, (float)l, (float)k, (float)l, 0);
                        }
                    }
                }
            }
        }
    }
}
