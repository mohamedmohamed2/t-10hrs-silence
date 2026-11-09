# 10 Hours of Silence (tModLoader mod)

Every death in a world adds another "10 hours of silence interrupted by ___" track. Each track plays its sound
at a random moment every 5 to 30 minutes. Tracks stack forever. In multiplayer the server decides when each sound
plays and tells every player at the same moment, so everyone hears the same thing.

- Death counter: one per world, shared by all players, saved with the world.
- Death 1 = first sound file (by filename), death 2 = second file, and so on. After the last file it loops back to
  the first, so a 300-death run with 100 clips has every clip stacked 3 times.
- A gray chat line announces each new track.

## Install / build (once)

1. Install tModLoader from Steam and launch it once.
2. Put this folder at `Documents/My Games/Terraria/tModLoader/ModSources/TenHoursOfSilence`
   (Linux: `~/.local/share/Terraria/tModLoader/ModSources/`). **The folder must be named exactly `TenHoursOfSilence`.**
3. In tModLoader: Workshop > Develop Mods > TenHoursOfSilence > **Build + Reload**.
4. If the build fails, copy the error text and send it to me.

For multiplayer, every player and the server need the same build of the mod (tModLoader enforces this).
After you add sounds, rebuild and send the new mod to your friends.

## Adding sounds

Drop `.wav` or `.ogg` files (mp3 may also work) into `Sounds/Interrupts/`. Name them with a number in front to control the order:

```
Sounds/Interrupts/001_metal_pipe.wav
Sounds/Interrupts/002_goku_scream.ogg
Sounds/Interrupts/003_vine_boom.wav
```

The chat name comes from the filename (`002_goku_scream` shows as "goku scream").
**Delete `999_test_beep.wav`** once you have real sounds. It's only there so you can confirm the mod works.

Tips: keep clips short (under ~10 s) and trim silence at the start. 44.1 kHz 16-bit wav is the safest format.

## Where to get clips

- **Freesound.org**: Creative Commons licensed, safe if you ever publish to the Workshop (check each clip's license).
- **Meme soundboard sites (e.g. myinstants.com)**: have nearly every meme, but the clips are mostly copyrighted
  (Goku = Toei/Funimation, game sounds = their studios). OK for private play, risky for a public Workshop upload.

## Starter list (popular "interrupted by" sounds, from memory, not a verified catalog)

metal pipe falling, Goku scream, vine boom, bruh, Taco Bell bong, Windows XP error, Windows XP shutdown,
Minecraft villager "hmm", Minecraft hurt "oof", Roblox oof, bonk, MLG air horn, Among Us emergency meeting,
Among Us kill, Discord notification, Discord call join, Skype ringtone, iPhone alarm, Nokia ringtone,
Mac startup chime, Wilhelm scream, fart with reverb, Owen Wilson "wow", SpongeBob "a few moments later",
"emotional damage", "bababooey", cat "huh", sad violin, "augh", anime "wow", Mario "mamma mia",
Sonic ring, Zelda item get, Pac-Man death, Mario coin, Mario 1-up, Metal Gear alert "!",
Undertale Sans "megalovania" sting, Halo "oh no", Duolingo, dial-up modem, "yeet", hamster "what",
"Ohio" sting, rizz sound, Terraria guide death, Terraria Moon Lord roar, lego step scream,
"It's free real estate", John Cena intro, "Nope" (Engineer), "Directed by Robert B. Weide",
Curb Your Enthusiasm theme sting, "Coffin dance" sting, "To be continued" sting.

## Verified against tModLoader source (stable branch, Nov 2026)

- `ModPlayer.Kill` is documented as "Called on local, server, and remote clients", so counting only on the
  server / in singleplayer counts each death exactly once.
- `.wav`, `.mp3` and `.ogg` all have registered sound readers, so all three formats work.
- `SoundStyle.MaxInstances = 0` means no limit, so stacked copies of the same clip don't cut each other off.
- `Mod.GetFileNames`, `ModSystem.PostUpdateWorld/ClearWorld/SaveWorldData/LoadWorldData`, `Mod.HandlePacket` and
  `ChatHelper.BroadcastChatMessage` exist with the signatures used.

## Still untested

- Not compiled yet: no tModLoader binaries were reachable from the build environment. The first in-game build is the real test.
- Targets tModLoader 1.4.4 (current stable). tModLoader 1.4.5 is in preview and will need a port (.NET 10), but
  1.4.4 mods stay available on the Workshop.
- Interval timers are not saved, so they re-randomize each time the world loads. The death count *is* saved.
