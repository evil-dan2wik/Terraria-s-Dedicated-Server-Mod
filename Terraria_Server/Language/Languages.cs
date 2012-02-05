﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria_Server.Misc;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;
using Terraria_Server.Logging;

namespace Terraria_Server.Language
{
	public static class Languages
	{
#region Properties

		public static String Disconnected { get; set; }
		public static String PermissionsError { get; set; }
		public static String ExitRequestCommand { get; set; }
		public static String ExpiredCommandMessage { get; set; }
		public static String XRequestedShutdown { get; set; }
		public static String PropertiesReload { get; set; }
		public static String CurrentPlayers { get; set; }
		public static String NoPlayers { get; set; }
		public static String SavingWorld { get; set; }
		public static String SavingData { get; set; }
		public static String SavingComplete { get; set; }
		public static String InvalidPage { get; set; }
		public static String RemovedFrom { get; set; }
		public static String Add { get; set; }
		public static String Added { get; set; }
		public static String Remove { get; set; }
		public static String Removed { get; set; }
		public static String PleaseReview { get; set; }
		public static String WhilelistFailedSave { set; get; }
		public static String OPlistFailedSave { get; set; }
		public static String Command { get; set; }
		public static String IPExpected { get; set; }
		public static String TimeSet { get; set; }
		public static String CurrentTime { get; set; }
		public static String BossNotSpecified { get; set; }
		public static String BossSummonedBy { get; set; }
		public static String Clear { get; set; }
		public static String NoItemIDNameProvided { get; set; }
		public static String ExplosionsAreNow { get; set; }
		public static String ThisIsPlayerCommand { get; set; }
		public static String CannotQuestGiverWithoutTDCM { get; set; }
		public static String QuestGiverAlreadySpawned { get; set; }
		public static String YouMustWaitBeforeAnotherCommand { get; set; }

		public static String RPGMode_Allowed { get; set; }
		public static String RPGMode_Refused { get; set; }

		public static String ItemRejection_Added { get; set; }
		public static String ItemRejection_ItemExists { get; set; }
		public static String ItemRejection_Removed { get; set; }
		public static String ItemRejection_ItemDoesntExist { get; set; }
		public static String ItemRejection_Cleared { get; set; }

		public static String Ban_You { get; set; }
		public static String Ban_Banned { get; set; }
		public static String Ban_FailedToSave { get; set; }
		public static String Ban_UnBanned { get; set; }

		//Commands
		public static String CommandDescription_Exit { get; set; }
		public static String CommandDescription_SaveAll { get; set; }
		public static String CommandDescription_ReloadPlugins { get; set; }
		public static String CommandDescription_OnlinePlayers { get; set; }
		public static String CommandDescription_Me { get; set; }
		public static String CommandDescription_Say { get; set; }
		public static String CommandDescription_Slots { get; set; }
		public static String CommandDescription_Kick { get; set; }
		public static String CommandDescription_Ban { get; set; }
		public static String CommandDescription_UnBan { get; set; }
		public static String CommandDescription_Whitelist { get; set; }
		public static String CommandDescription_Rcon { get; set; }
		public static String CommandDescription_Status { get; set; }
		public static String CommandDescription_Time { get; set; }
		public static String CommandDescription_Help { get; set; }
		public static String CommandDescription_Give { get; set; }
		public static String CommandDescription_SpawnNpc { get; set; }
		public static String CommandDescription_Teleport { get; set; }
		public static String CommandDescription_TeleportHere { get; set; }
		public static String CommandDescription_SettleLiquids { get; set; }
		public static String CommandDescription_Op { get; set; }
		public static String CommandDescription_DeOp { get; set; }
		public static String CommandDescription_OpLogin { get; set; }
		public static String CommandDescription_OpLogout { get; set; }
		public static String CommandDescription_NpcSpawns { get; set; }
		public static String CommandDescription_Restart { get; set; }
		public static String CommandDescription_Purge { get; set; }
		public static String CommandDescription_Plugins { get; set; }
		public static String CommandDescription_Plugin { get; set; }
		public static String CommandDescription_SpawnBoss { get; set; }
		public static String CommandDescription_ItemRej { get; set; }
		public static String CommandDescription_Explosions { get; set; }
		public static String CommandDescription_MaxPlayers { get; set; }
		public static String CommandDescription_Q { get; set; }
		public static String CommandDescription_Refresh { get; set; }
		public static String CommandDescription_HardMode { get; set; }

#endregion

		public static void LoadClass(string filePath, bool restore = false, bool error = true)
		{
			if (!File.Exists(filePath) || restore)
			{
				if (File.Exists(filePath))
					File.Delete(filePath);

				using (var ctx = Assembly.GetExecutingAssembly().GetManifestResourceStream(Collections.Registries.DEFINITIONS + filePath))
				{
					using (var stream = File.OpenWrite(filePath))
					{
						var buff = new byte[ctx.Length];
						ctx.Read(buff, 0, buff.Length);
						stream.Write(buff, 0, buff.Length);
					}
				}
			}

			using (var stream = File.Open(filePath, FileMode.Open))
			{
				var document = new XmlDocument();
				document.Load(stream);

				var type = typeof(Languages);

				foreach (XmlNode node in document.ChildNodes[0].ChildNodes)
				{
					try
					{
						var property = node.Name;
						var value = node.InnerText;

						var properties = from x in type.GetProperties() where x.Name == property select x;

						foreach (var prop in properties)
							prop.SetValue(null, value, null);
					}
					catch (Exception e)
					{
						if (error)
							ProgramLog.Error.Log("Error parsing language file\n{0}", e);
					}
				}
			}
		}
	}
}