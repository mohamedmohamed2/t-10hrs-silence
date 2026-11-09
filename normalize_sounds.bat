@echo off
rem Double-click me after putting clips in RawSounds. Needs Python (python.org or Microsoft Store).
cd /d "%~dp0"
set PY=py
where py >nul 2>nul || set PY=python
%PY% -m pip install --quiet --disable-pip-version-check imageio-ffmpeg
%PY% normalize_sounds.py
pause
