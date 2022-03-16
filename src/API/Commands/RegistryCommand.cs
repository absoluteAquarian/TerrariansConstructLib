using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace TerrariansConstructLib.API.Commands {
	internal abstract class RegistryCommand<T> : ModCommand {
		public sealed override CommandType Type => CommandType.Chat;

		public sealed override string Usage => $"[c/ff6a00:Usage: {UsageString}]";

		public abstract Dictionary<int, T> GetRegistry();

		public abstract string GetReplyString(int id, T data);

		public abstract string UsageString { get; }

		public abstract string ChatString { get; }

		public sealed override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				caller.Reply("Command expects no arguments.", Color.Red);
				return;
			}

			caller.Reply("[c/999999:" + ChatString + "]");

			foreach (var (id, data) in GetRegistry()) {
				string reply = GetReplyString(id, data);

				foreach (var str in reply.Split('\n', StringSplitOptions.RemoveEmptyEntries))
					caller.Reply("[c/cccccc:" + str + "]");
			}
		}
	}
}
