using FrooxEngine;
using FrooxEngine.Headless;
using HarmonyLib;
using ResoniteModLoader;

namespace HeadlessAllowList;

public class HeadlessAllowList : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0"; 
	public override string Name => "HeadlessAllowList";
	public override string Author => "Grand";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/GrandtheUK/HeadlessAllowList/";

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<Dictionary<string, List<String>>> allowList =
		new ModConfigurationKey<Dictionary<string, List<string>>>("allowList",
			"A mapping of sessionIds to a list of allowed UserIds",
			() => new Dictionary<string, List<string>> { { "GLOBAL", [] } });

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<Dictionary<string, List<String>>> blockList =
		new ModConfigurationKey<Dictionary<string, List<string>>>("blockList",
			"a mapping of sessionIds to a list of blocked UserIds",
			() => new Dictionary<string, List<string>> { { "GLOBAL", [] } });
	
	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<List<string>> exclusive = new ModConfigurationKey<List<string>>("exclusive"," a list of sessions that exclude global allowlist", () => []);
	
	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<List<string>> enabled = new ModConfigurationKey<List<string>>("enabled", "a list of sessions covered by allowList", () => []);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<Dictionary<string, string>> DenyMessages =
		new ModConfigurationKey<Dictionary<string, string>>("denyMessage", "a mapping of session IDs to Deny Messages",
			(() => new Dictionary<string, string> {
				{ "GLOBAL", "Denied by HeadlessAllowList mod. Contact server owner/operator for access" }
			}));
	
	
	private static ModConfiguration Config;
	public override void OnEngineInit() {	
		Config = GetConfiguration();
		Config.Save(true);
		Msg($"Got the allowList: {Config.GetValue(allowList)}");
		Harmony harmony = new("com.GrandtheUK.HeadlessAllowList");
		harmony.PatchAll();
	}
	
	[HarmonyPatch(typeof(World), "VerifyJoinRequest")]
	public static class VerifyJoinRequestPatch {
		static async Task<JoinGrant> Postfix(Task<JoinGrant> __result, SessionConnection connection) {
			Msg("checking if user can join");
			JoinGrant originalResult = await __result;
			Dictionary<string, string> messages = Config?.GetValue(DenyMessages);
			Dictionary<string, List<string>> allow = Config?.GetValue(allowList) ?? new Dictionary<string, List<string>>();
			Dictionary<string, List<string>> block = Config?.GetValue(blockList) ?? new Dictionary<string, List<string>>();
			List<string> exclusiveList = Config?.GetValue(exclusive) ?? [];
			
			List<string> allowWorld = allow.GetValueSafe(connection.World.SessionId) ?? [];
			List<string> blockWorld = block.GetValueSafe(connection.World.SessionId) ?? [];
			List<string> allowGlobal = allow.GetValueSafe("GLOBAL") ?? [];
			List<string> blockGlobal = block.GetValueSafe("GLOBAL") ?? [];
			
			List<string> enable = Config?.GetValue(enabled) ?? [];
			Msg($"enabled sessions: {string.Join(",", enable)}");
			if (!enable.Contains(connection.World.SessionId)) {
				Msg($"Not enabled on session {connection.World.SessionId}. returning normal joinGrant result");
				return originalResult;
			}

			if ((allowWorld.Contains(connection.UserID) ||
			     (allowGlobal.Contains(connection.UserID) && !exclusiveList.Contains(connection.World.SessionId))
			    ) &&
			    !blockWorld.Contains(connection.UserID) && !blockGlobal.Contains(connection.UserID)) {
				Msg($"Allowed user: {connection.Username} ({connection.UserID}) to session: {connection.World.SessionId}");
				return originalResult;
			}

			Msg($"Denied user: {connection.Username} ({connection.UserID}) to session: {connection.World.SessionId}");
			string denyMessage = messages.GetValueSafe(connection.World.SessionId) ?? messages.GetValueSafe("GLOBAL") ?? "Denied by HeadlessAllowList mod. Contact server owner/operator for access";
			return JoinGrant.Deny(denyMessage);
		}
	}
	
	
	public static void AddUser(string UserId, string SessionId = "GLOBAL") {
		Dictionary<string, List<string>> allow = Config?.GetValue(allowList);
		if (allow[SessionId].Contains(UserId)) return;
		if (allow.TryGetValue(SessionId, out List<string> sessionList))
		{
			sessionList.Add(UserId);
			allow[SessionId] = sessionList;
		} else {
			allow.Add(SessionId,[UserId]);
		}
		Config?.Set(allowList,allow);
		Config?.Save();
	}
	public static void RemoveUser(string UserId, string SessionId = "GLOBAL") {
		Dictionary<string, List<string>> allow = Config?.GetValue(allowList);
		if (!allow.ContainsKey(SessionId)) return;
		if (!allow[SessionId].Contains(UserId)) return;
		allow[SessionId].Remove(UserId);
		Config?.Set(allowList,allow);
		Config?.Save();
	}
	public static void BlockUser(string UserId, string SessionId = "GLOBAL") {
		Dictionary<string, List<string>> block = Config?.GetValue(blockList);
		if (block[SessionId].Contains(UserId)) return;
		if (block.TryGetValue(SessionId, out List<string> blocks))
		{
			blocks.Add(UserId);
			block[SessionId] = blocks;
		} else {
			block.Add(SessionId,[UserId]);
		}
		Config?.Set(blockList,block);
		Config?.Save();
	}
	public static void UnblockUser(string UserId, string SessionId = "GLOBAL") {
		Dictionary<string, List<string>> block = Config?.GetValue(blockList);
		if (!block.ContainsKey(SessionId)) return;
		if (!block[SessionId].Contains(UserId)) return;
		block[SessionId].Remove(UserId);
		Config?.Set(blockList,block);
		Config?.Save();
	}
	public static void enable(string SessionId) {
		List<string> enable = Config?.GetValue(enabled);
		if (enable.Contains(SessionId)) return;
		enable.Add(SessionId);
		Config?.Set(enabled,enable);
		Config?.Save();
	}
	public static void disable(string SessionId) {
		List<string> enable = Config?.GetValue(enabled);
		if (!enable.Contains(SessionId)) return;
		enable.Remove(SessionId);
		Config?.Set(enabled,enable);
		Config?.Save();
	}
	public static void listUsers(string SessionId = "GLOBAL") {
		Dictionary<string,List<string>> allow = Config?.GetValue(allowList);
		if (!allow.ContainsKey(SessionId)) {
			Msg($"No allowlist for {SessionId}");
			return;
		}
		switch (SessionId)
		{
			case "GLOBAL":
				Msg($"Globally allowed users: {String.Join(",",allow["GLOBAL"])}");
				break;
			default:
				Msg($"Allowed Users for session {SessionId}: {String.Join(",",allow[SessionId])}");
				break;
		}
	}
	private static void exclusiveEnable(string SessionId) {
		List<string> exclusiveEnable = Config?.GetValue(exclusive);
		if (exclusiveEnable.Contains(SessionId)) return;
		exclusiveEnable.Add(SessionId);
		Config.Set(exclusive,exclusiveEnable);
		Config.Save();
	}
	private static void exclusiveDisable(string SessionId) {
		List<string> exclusiveEnable = Config?.GetValue(exclusive);
		if (!exclusiveEnable.Contains(SessionId)) return;
		exclusiveEnable.Remove(SessionId);
		Config?.Set(exclusive,exclusiveEnable);
		Config?.Save();
	}

	private static void denyMessageSet(string denyMessage, string SessionId = "GLOBAL") {
		Dictionary<string, string> denyMessages = Config?.GetValue(DenyMessages);
		if (denyMessages.ContainsKey(SessionId)) {
			denyMessages[SessionId] = denyMessage;	
		} else {
			denyMessages.Add(SessionId,denyMessage);
		}
		Config?.Set(DenyMessages,denyMessages);
		Config?.Save();
	}
	
	private static void denyMessageRemove(string SessionId = "GLOBAL") {
		Dictionary<string, string> denyMessages = Config?.GetValue(DenyMessages);
		if (!denyMessages.ContainsKey(SessionId))
			return;
		denyMessages.Remove(SessionId);
		Config?.Set(DenyMessages,denyMessages);
		Config?.Save();
	}
	

	[HarmonyPatch(typeof(HeadlessCommands), "SetupCommonCommands")]
	public static class CustomCommandPatch {
		static void Postfix(CommandHandler handler) {
			#pragma warning disable CS1998
		handler.RegisterCommand(new GenericCommand("allowlist","allowlist control command", "Session [SessionId] list, Session [SessionId]<enable/disable/add/remove/block> [UserId], Session [SessionId] exclusive <enable/disable>, global <add/remove> [UserId}, global list", CommandHandler));
#pragma warning restore CS1998
		}
	}
	public static void CommandHandler(CommandHandler h, World w, List<string> args) {
		if (args.Count < 1) {
			Msg("Must contain at least 1 subcommand. allowlist [global/session]");
			return;
		}
		

		switch (args[0].ToLower()) {
			case "help":
				Msg("allowlist [global/session]");
				break;
			case "global":
				if (args.Count < 2) {
					Msg("Must contain at least one subcommand. allowlist global [add/remove/list/message]");
					break;
				}
				switch (args[1].ToLower()) {
					case "add":
						if (args.Count < 3) {
							Msg("Must contain UserId. allowlist global add [UserID]");
							break;
						}
						AddUser(args[2]);
						break;
					case "remove":
						if (args.Count < 3) {
							Msg("Must contain UserId. allowlist global remove [UserID]");
							break;
						}
						RemoveUser(args[2]);
						break;
					case "list":
						listUsers();
						break;
					case "block":
						if (args.Count < 3) {
							Msg("Must contain UserId. allowlist global block [UserID]");
							break;
						}
						BlockUser(args[2]);
						break;
					case "unblock":
						if (args.Count < 4) {
							Msg("Must contain UserId. allowlist global unblock [UserID]");
							break;
						}
						UnblockUser(args[3]);
						break;
					case "message":
						if (args.Count < 3) {
							Msg("Must contain subcommand. allowlist global message [set/remove]");
							break;
						}
						switch (args[2].ToLower()) {
							case "set":
								if (args.Count < 4) {
									Msg("Must contain message. allowlist global message set [DenyMessage]");
									break;
								}
								string message = String.Join(" ", args.GetRange(3,args.Count - 3));
								message = message.Replace("\"", "");
								denyMessageSet(message);
								break;
							case "remove":
								denyMessageRemove();
								break;
							default:
								Msg("Not as valid sub-command. allowlist global message [set/remove]");
								break;
						}
						break;
					default:
						Msg("Not a valid sub-command. allowlist global [add/remove/list/message]");
						break;
				}
				break;
			case "session":
				switch (args.Count) {
					case 1:
						Msg("Must contain SessionId. allowlist session [SessionID] add/remove/block/unblock/enable/disable/exclusive/list ");
						break;
					case 2:
						Msg("Must contain at least one subcommand. allowlist session [SessionID] add/remove/block/unblock/enable/disable/exclusive/list");
						break;
				}

				string session;
				if (args[1] == "@") {
					session = w.SessionId;
				} else {
					session = args[1];
				}
				switch (args[2].ToLower()) {
					case "add":
						if (args.Count < 4) {
							Msg("Must contain UserId. allowlist session [SessionID] add [UserID]");
							break;
						}
						AddUser(args[3],session);
						break;
					case "remove":
						if (args.Count < 4) {
							Msg("Must contain UserId. allowlist session [SessionID] remove [UserID]");
							break;
						}
						RemoveUser(args[3],session);
						break;
					case "block":
						if (args.Count < 4) {
							Msg("Must contain UserId. allowlist session [SessionID] block [UserID]");
							break;
						}
						BlockUser(args[3],session);
						break;
					case "unblock":
						if (args.Count < 4) {
							Msg("Must contain UserId. allowlist session [SessionID] unblock [UserID]");
							break;
						}
						UnblockUser(args[3],session);
						break;
					case "list":
						listUsers(session);
						break;
					case "enable":
						enable(session);
						break;
					case "disable":
						disable(session);
						break;
					case "exclusive":
						if (args.Count < 4) {
							Msg("Must contain at least one subcommand. allowlist session [SessionID] exclusive enable/disable");
							return;
						}

						switch (args[4].ToLower()) {
							case "enable":
								exclusiveEnable(session);
								break;
							case "disable":
								exclusiveDisable(session);
								break;
							default:
								Msg("Not a valid sub-command. allowlist session [SessionID] exclusive enable/disable");
								break;
						}
						break;
					case "message":
						if (args.Count < 4) {
							Msg("Must contain subcommand. allowlist session [SessionID] message [set/remove]");
							break;
						}
						switch (args[3].ToLower()) {
							case "set":
								if (args.Count < 5) {
									Msg("Must contain message. allowlist session [SessionID] message set [DenyMessage]");
									break;
								}

								string message = String.Join(" ", args.GetRange(4,args.Count - 4));
								denyMessageSet(message,session);
								break;
							case "remove":
								denyMessageRemove(session);
								break;
							default:
								Msg("Not as valid sub-command. allowlist session [SessionID] message [set/remove]");
								break;
						}
						break;
					default:
						Msg("Not a valid sub-command. allowlist session [SessionID] add/remove/block/unblock/enable/disable/exclusive/list");
						break;
				}
				break;
			default:
				Msg("Not a valid sub-command");
				break;
		}
	}
}
