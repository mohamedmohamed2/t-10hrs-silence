using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TenHoursOfSilence
{
	public class TenHoursOfSilence : Mod
	{
		// Every audio file in Sounds/Interrupts, sorted by filename. Death 1 = first file, death 2 = second, ...
		// After the last file it wraps around, so stacks keep growing past the number of clips.
		public static List<SoundStyle> Sounds = new();
		public static List<string> Names = new();

		public override void Load()
		{
			Sounds.Clear();
			Names.Clear();
			string[] exts = { ".wav", ".ogg", ".mp3" };
			var files = GetFileNames()
				.Select(f => f.Replace('\\', '/'))
				.Where(f => f.StartsWith("Sounds/Interrupts/") && exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

			foreach (string f in files) {
				string noExt = f[..^Path.GetExtension(f).Length];
				Sounds.Add(new SoundStyle($"{Name}/{noExt}") { MaxInstances = 0 });
				Names.Add(Path.GetFileName(noExt).TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_', '-', ' ').Replace('_', ' '));
			}
		}

		// Server tells every client "play sound #i now", so everyone hears it at the same moment.
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			int i = reader.ReadInt32();
			if (Main.netMode == NetmodeID.MultiplayerClient && i >= 0 && i < Sounds.Count)
				SoundEngine.PlaySound(Sounds[i]);
		}
	}

	public class SilenceSystem : ModSystem
	{
		const int MinTicks = 15 * 60 * 60; // 15 minutes (60 ticks per second)
		const int MaxTicks = 45 * 60 * 60; // 45 minutes

		int tracks; // +1 per player death, -1 per boss defeated
		readonly List<int> timers = new(); // one countdown per track; not saved, re-randomized on load

		// Fires once per boss fight, exactly when the purple "X has been defeated!" message posts
		// (once for The Twins, once for Eater of Worlds). Server/singleplayer only. Same hook CalRemix uses.
		public override void Load() => On_NPC.DoDeathEvents_CelebrateBossDeath += (orig, self, typeName) => {
			orig(self, typeName);
			if (Main.netMode != NetmodeID.MultiplayerClient)
				RemoveTrack();
		};

		public override void ClearWorld()
		{
			tracks = 0;
			timers.Clear();
		}

		public override void SaveWorldData(TagCompound tag) => tag["tracks"] = tracks;

		public override void LoadWorldData(TagCompound tag) => tracks = tag.GetInt("tracks");

		public void AddDeath()
		{
			tracks++;
			Announce($"Death! Now also playing \"10 hours of silence interrupted by {NameOf(tracks - 1)}\" ({tracks} tracks)");
		}

		// Removes the newest track; the next death brings that same sound back.
		public void RemoveTrack()
		{
			if (tracks == 0)
				return;
			tracks--;
			Announce($"Boss defeated! \"10 hours of silence interrupted by {NameOf(tracks)}\" stopped ({tracks} tracks left)");
		}

		static string NameOf(int track) => TenHoursOfSilence.Names.Count == 0 ? "nothing" : TenHoursOfSilence.Names[track % TenHoursOfSilence.Names.Count];

		static void Announce(string msg)
		{
			if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(msg), Color.Gray);
			else
				Main.NewText(msg, Color.Gray);
		}

		// Runs on the server (multiplayer) or locally (singleplayer), never on multiplayer clients.
		public override void PostUpdateWorld()
		{
			var sounds = TenHoursOfSilence.Sounds;
			if (sounds.Count == 0)
				return;

			while (timers.Count < tracks)
				timers.Add(Main.rand.Next(MinTicks, MaxTicks + 1));
			if (timers.Count > tracks)
				timers.RemoveRange(tracks, timers.Count - tracks);

			for (int t = 0; t < timers.Count; t++) {
				if (--timers[t] > 0)
					continue;
				timers[t] = Main.rand.Next(MinTicks, MaxTicks + 1);
				int i = t % sounds.Count;
				if (Main.netMode == NetmodeID.Server) {
					ModPacket p = Mod.GetPacket();
					p.Write(i);
					p.Send();
				}
				else {
					SoundEngine.PlaySound(sounds[i]);
				}
			}
		}
	}

	public class SilencePlayer : ModPlayer
	{
		// Kill runs on local, server and remote clients; only count it on the server / in singleplayer so it counts once.
		// Slot 255 is the game's placeholder player, not a real one (same guard as the Boss Checklist mod).
		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient && Player.whoAmI != 255)
				ModContent.GetInstance<SilenceSystem>().AddDeath();
		}
	}
}
