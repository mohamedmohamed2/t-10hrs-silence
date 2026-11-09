# 10 Hours of Silence (tModLoader mod)

Every death in a world adds another "10 hours of silence interrupted by ___" track. Each track plays its sound
at a random moment every 15 to 45 minutes. Tracks stack forever. In multiplayer the server decides when each sound
plays and tells every player at the same moment, so everyone hears the same thing.

- Tracks are saved per world and shared by all players.
- **Each death adds a random sound** that isn't already playing. Once every sound is in use, duplicates can stack.
- **The first kill of each boss removes the oldest track** (first in, first out). Re-kills don't count, so summon
  items can't be farmed. "Boss killed" means Terraria posted its purple "X has been defeated!" message, so The Twins
  and Eater of Worlds count once per fight, and modded bosses count too.
- **Volume slider:** Settings > Mod Configuration > 10 Hours of Silence. Each player sets their own (0 = muted).
- A gray chat line announces every added or removed track.

## Install / build (once)

1. Install tModLoader from Steam and launch it once.
2. Put this folder at `Documents/My Games/Terraria/tModLoader/ModSources/TenHoursOfSilence`
   (Linux: `~/.local/share/Terraria/tModLoader/ModSources/`). **The folder must be named exactly `TenHoursOfSilence`.**
3. In tModLoader: Workshop > Develop Mods > TenHoursOfSilence > **Build + Reload**.
4. If the build fails, copy the error text and send it to me.

For multiplayer, every player and the server need the same build of the mod (tModLoader enforces this).
After you add sounds, rebuild and send the new mod to your friends.

## Adding sounds (with loudness normalizing)

1. Put clips in the `RawSounds` folder (any of mp3, wav, ogg, m4a, webm...). Any names are fine.
2. Double-click `normalize_sounds.bat`. It makes every clip the same loudness and writes clean `.wav` copies into
   `Sounds/Interrupts/`. Your originals in `RawSounds` are never changed.
   - Needs Python once: install from python.org (tick "Add python.exe to PATH") or the Microsoft Store.
   - First run moves anything already in `Sounds/Interrupts/` into `RawSounds` so it gets normalized too.
   - Everything too loud or too quiet? Change `TARGET_DB` at the top of `normalize_sounds.py` (-14 louder, -22 quieter).
3. Build + Reload in tModLoader.

Order and numbering don't matter anymore. You can add or remove sounds mid-playthrough: worlds save tracks by file
name, so existing tracks keep their sound. If a sound file was removed, its track switches to a random sound.
Chat shows the file name (`goku_scream` shows as "goku scream").
**Delete `999_test_beep.wav` from `RawSounds`** once you have real sounds.

Tips: keep clips short (under ~10 s) and trim silence at the start.

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
  server / in singleplayer counts each death exactly once. Boss Checklist (JavidPack/BossChecklist) counts deaths
  the same way, and the mod also copies its guard against player slot 255 (the game's placeholder player).
- Server-to-player messages only reach fully joined players (`ModPacket.Send` checks `broadcast`), and a failing
  packet handler is caught by tModLoader, so a bad packet can't crash anyone.
- `.wav`, `.mp3` and `.ogg` all have registered sound readers, so all three formats work.
- `SoundStyle.Volume` is capped at 1.0, so the mod can't boost quiet clips in-game. That's why normalizing happens
  on the files before building.
- `SoundStyle.MaxInstances = 0` means no limit, so stacked copies of the same clip don't cut each other off.
- `Mod.GetFileNames`, `ModSystem.PostUpdateWorld/ClearWorld/SaveWorldData/LoadWorldData`, `Mod.HandlePacket` and
  `ChatHelper.BroadcastChatMessage` exist with the signatures used.

## Still untested

- Not compiled yet: no tModLoader binaries were reachable from the build environment. The first in-game build is the real test.
- Targets tModLoader 1.4.4 (current stable). tModLoader 1.4.5 is in preview and will need a port (.NET 10), but
  1.4.4 mods stay available on the Workshop.
- Interval timers are not saved, so they re-randomize each time the world loads. The tracks *are* saved.
- `normalize_sounds.py` was tested on Linux with mono/stereo, mp3/ogg/wav and very quiet/loud clips (all came out at
  -18.0 dB). The `.bat` wrapper hasn't been run on Windows yet.
