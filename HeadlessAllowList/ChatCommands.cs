using FrooxEngine;
using HeadlessTweaks;
using SkyFrost.Base;

namespace HeadlessAllowList;

public class ChatCommands {
	public static void Init() {
		MessageCommands.RegisterCommands(typeof(ChatCommands));
	}

	[MessageCommands.Command("allowlist", "", "Moderation", PermissionLevel.Moderator)]
	public async static void Allowlist(UserMessages userMessages, Message msg, string[] args) {
		HeadlessAllowList.CommandHandler(null,Engine.Current.WorldManager.FocusedWorld, args.ToList(),msg);
	}

	[MessageCommands.Command("focusWorld", "Changes world Focus", "Moderation", PermissionLevel.Moderator)]
	public static void FocusWorld(UserMessages userMessages, Message msg, string[] args) {
		if (args.Length != 1) {
			userMessages.SendTextMessage("No world index provided");
		}

		if (!int.TryParse(args[0], out int result)) {
			userMessages.SendTextMessage("Can't parse world number");
			return;
		}
		Engine engine = Engine.Current;
		List<World> worlds = engine.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld).ToList();
		if (result > worlds.Count) {
			userMessages.SendTextMessage("index out of range");
			return;
		}
		engine.WorldManager.FocusWorld(worlds[result]);
	}
	
	[MessageCommands.Command("worlds", "Lists open worlds", "Moderation", PermissionLevel.Moderator)]
	public static void Worlds(UserMessages userMessages, Message msg, string[] args) {
		if (args.Length != 1) {
			userMessages.SendTextMessage("No world index provided");
		}
		Engine engine = Engine.Current;
		List<World> worlds = engine.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld).ToList();
		int num = 0;
		List<string> worldString = [];
		foreach (World world in worlds)
		{
			worldString.Add($"[{num.ToString()}] Users: {world.UserCount}\tPresent: {world.ActiveUserCount}\tAccessLevel: {world.AccessLevel}\tMaxUsers: {world.MaxUsers}");
			++num;
		}
		string result = String.Join("<br>", worldString);
		userMessages.SendTextMessage(result);
	}
}
