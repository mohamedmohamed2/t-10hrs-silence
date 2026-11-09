"""Makes every clip the same loudness.

Put raw clips (mp3, wav, ogg, m4a, webm, ... anything ffmpeg reads) in RawSounds/, run this,
and it rebuilds Sounds/Interrupts/ with loudness-matched 44.1 kHz 16-bit mono .wav copies.
Originals in RawSounds/ are never touched. Then Build + Reload in tModLoader.

Needs: pip install imageio-ffmpeg  (normalize_sounds.bat does this for you)
"""
import math
import re
import shutil
import subprocess
import sys
from array import array
from pathlib import Path

import imageio_ffmpeg

TARGET_DB = -18.0  # loudness of the audible parts, in dBFS RMS. Raise toward -14 for louder, lower for quieter.
PEAK_LIMIT = 0.89  # about -1 dBFS, keeps boosted clips from clipping
SILENCE_DB = -60.0  # 50 ms windows quieter than this don't count, so leading/trailing silence doesn't skew it

ROOT = Path(__file__).resolve().parent
RAW = ROOT / "RawSounds"
OUT = ROOT / "Sounds" / "Interrupts"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()


def loudness_db(path):
    pcm = subprocess.run([FFMPEG, "-v", "error", "-i", str(path), "-ac", "1", "-ar", "44100", "-f", "s16le", "-"],
                         capture_output=True, check=True).stdout
    samples = array("h", pcm[: len(pcm) // 2 * 2])
    win = 2205  # 50 ms
    powers = []
    for i in range(0, len(samples) - win + 1, win):
        p = sum(s * s for s in samples[i:i + win]) / win / 32768 ** 2
        if p > 0 and 10 * math.log10(p) > SILENCE_DB:
            powers.append(p)
    return 10 * math.log10(sum(powers) / len(powers)) if powers else None


def safe_name(stem):
    return re.sub(r"[^a-z0-9]+", "_", stem.lower()).strip("_") or "sound"


def main():
    if not RAW.exists():  # first run: adopt whatever is already in the mod
        RAW.mkdir()
        for f in OUT.glob("*.*"):
            shutil.move(str(f), RAW / f.name)
    OUT.mkdir(parents=True, exist_ok=True)
    for f in OUT.glob("*.wav"):
        f.unlink()

    done = 0
    for src in sorted(p for p in RAW.iterdir() if p.is_file()):
        try:
            db = loudness_db(src)
        except subprocess.CalledProcessError:
            print(f"SKIP  {src.name}: not an audio file ffmpeg can read")
            continue
        if db is None:
            print(f"SKIP  {src.name}: silent")
            continue
        gain = TARGET_DB - db
        dst = OUT / (safe_name(src.stem) + ".wav")
        subprocess.run([FFMPEG, "-v", "error", "-y", "-i", str(src),
                        "-af", f"volume={gain:.2f}dB,alimiter=limit={PEAK_LIMIT}:level=0",
                        "-ar", "44100", "-ac", "1", "-c:a", "pcm_s16le", str(dst)], check=True)
        print(f"OK    {src.name} -> {dst.name}  ({gain:+.1f} dB)")
        done += 1
    print(f"\n{done} sounds written to {OUT}. Now Build + Reload in tModLoader.")


if __name__ == "__main__":
    sys.exit(main())
