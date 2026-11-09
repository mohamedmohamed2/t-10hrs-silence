using System;
using System.ComponentModel;
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
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace TenHoursOfSilence
{
	public class TenHoursOfSilence : Mod
	{
		// Every audio file in Sounds/Interrupts. Keys (file names) are what worlds save, so adding/removing files
		// never changes which sound an existing track plays. Same list on server and clients (same mod build).
		public static List<SoundStyle> Sounds = new();
		public static List<string> Keys = new();
		public static List<string> Names = new();

		public override void Load()
		{
			Sounds.Clear();
			Keys.Clear();
			Names.Clear();
			string[] exts = { ".wav", ".ogg", ".mp3" };
			var files = GetFileNames()
				.Select(f => f.Replace('\\', '/'))
				.Where(f => f.StartsWith("Sounds/Interrupts/") && exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

			foreach (string f in files) {
				string noExt = f[..^Path.GetExtension(f).Length];
				Sounds.Add(new SoundStyle($"{Name}/{noExt}") { MaxInstances = 0 });
				Keys.Add(Path.GetFileName(noExt));
				Names.Add(Path.GetFileName(noExt).TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_', '-', ' ').Replace('_', ' '));
			}
		}

		// Server tells every client "play sound #i now", so everyone hears it at the same moment.
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			int i = reader.ReadInt32();
			if (Main.netMode == NetmodeID.MultiplayerClient && i >= 0 && i < Sounds.Count)
				Play(i);
		}

		// Each player's own volume slider (client-side config) applies here, on their machine only.
		public static void Play(int i)
		{
			float volume = ModContent.GetInstance<SilenceConfig>().Volume / 100f;
			if (volume > 0)
				SoundEngine.PlaySound(Sounds[i] with { Volume = volume });
		}
	}

	public class SilenceSystem : ModSystem
	{
		const int MinTicks = 15 * 60 * 60; // 15 minutes (60 ticks per second)
		const int MaxTicks = 45 * 60 * 60; // 45 minutes

		class Track
		{
			public int Sound; // index into TenHoursOfSilence.Sounds
			public int Timer = NewTimer(); // not saved, re-randomized on load
		}

		static int NewTimer() => Main.rand.Next(MinTicks, MaxTicks + 1);

		readonly List<Track> tracks = new(); // oldest first: +1 per player death, oldest removed per first boss kill
		readonly HashSet<string> bossesBeaten = new(); // saved with the world

		// Fires once per boss fight, exactly when the purple "X has been defeated!" message posts
		// (once for The Twins, once for Eater of Worlds). Server/singleplayer only. Same hook CalRemix uses.
		public override void Load() => On_NPC.DoDeathEvents_CelebrateBossDeath += (orig, self, typeName) => {
			orig(self, typeName);
			if (Main.netMode != NetmodeID.MultiplayerClient)
				BossDefeated(self);
		};

		// One key per boss fight, so multi-part bosses can't count twice.
		static string BossKey(NPC npc)
		{
			if (npc.realLife >= 0) // worm segments sharing one health bar (e.g. The Destroyer) -> the head
				npc = Main.npc[npc.realLife];
			return npc.type switch {
				NPCID.Retinazer or NPCID.Spazmatism => "TheTwins",
				NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail => "EaterOfWorlds",
				_ => npc.ModNPC?.FullName ?? npc.type.ToString(),
			};
		}

		void BossDefeated(NPC npc)
		{
			if (bossesBeaten.Add(BossKey(npc)))
				RemoveTrack();
			else
				Announce("Boss already beaten before, no track removed.");
		}

		public override void ClearWorld()
		{
			tracks.Clear();
			bossesBeaten.Clear();
		}

		public override void SaveWorldData(TagCompound tag)
		{
			tag["trackSounds"] = tracks.Select(t => TenHoursOfSilence.Keys[t.Sound]).ToList();
			tag["bossesBeaten"] = bossesBeaten.ToList();
		}

		public override void LoadWorldData(TagCompound tag)
		{
			int n = TenHoursOfSilence.Sounds.Count;
			foreach (string key in tag.GetList<string>("trackSounds")) {
				int i = TenHoursOfSilence.Keys.IndexOf(key);
				if (i < 0 && n > 0) // that sound file was removed from the mod: swap in a random one, keep the count
					i = Main.rand.Next(n);
				if (i >= 0)
					tracks.Add(new Track { Sound = i });
			}
			bossesBeaten.UnionWith(tag.GetList<string>("bossesBeaten"));
		}

		// New tracks pick a random sound that isn't already playing; once every sound is in use, any random one.
		public void AddDeath()
		{
			int n = TenHoursOfSilence.Sounds.Count;
			if (n == 0)
				return;
			var free = Enumerable.Range(0, n).Except(tracks.Select(t => t.Sound)).ToList();
			int sound = free.Count > 0 ? free[Main.rand.Next(free.Count)] : Main.rand.Next(n);
			tracks.Add(new Track { Sound = sound });
			Announce($"Death! Now also playing \"10 hours of silence interrupted by {TenHoursOfSilence.Names[sound]}\" ({tracks.Count} tracks)");
		}

		// First in, first out: the oldest track stops.
		void RemoveTrack()
		{
			if (tracks.Count == 0)
				return;
			int sound = tracks[0].Sound;
			tracks.RemoveAt(0);
			Announce($"Boss defeated! \"10 hours of silence interrupted by {TenHoursOfSilence.Names[sound]}\" stopped ({tracks.Count} tracks left)");
		}

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
			foreach (Track t in tracks) {
				if (--t.Timer > 0)
					continue;
				t.Timer = NewTimer();
				if (Main.netMode == NetmodeID.Server) {
					ModPacket p = Mod.GetPacket();
					p.Write(t.Sound);
					p.Send();
				}
				else {
					TenHoursOfSilence.Play(t.Sound);
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

	public class SilenceConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide; // each player sets their own

		[Range(0, 100)]
		[Increment(5)]
		[DefaultValue(100)]
		[Slider]
		public int Volume;
	}
}
