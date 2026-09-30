#!/usr/bin/env bash
# Menghapus Bertahan. Pengaturan dan Top Skor di ~/.config/Bertahan tetap disimpan.
set -euo pipefail

if [[ "${1:-}" == "--system" ]]; then
    PREFIX=/opt/bertahan
    BIN=/usr/local/bin
    APPS=/usr/share/applications
    ICONS=/usr/share/icons/hicolor/256x256/apps
else
    PREFIX="${XDG_DATA_HOME:-$HOME/.local/share}/bertahan"
    BIN="$HOME/.local/bin"
    APPS="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
    ICONS="${XDG_DATA_HOME:-$HOME/.local/share}/icons/hicolor/256x256/apps"
fi

rm -rf "$PREFIX"
rm -f "$BIN/bertahan" "$APPS/bertahan.desktop" "$ICONS/bertahan.png"
echo "Bertahan sudah dihapus. (Simpanan permainan ada di ~/.config/Bertahan)"
