#!/usr/bin/env bash
# Memasang Bertahan untuk pengguna ini (tanpa sudo):
#   ./install.sh            -> ~/.local/share/bertahan + menu aplikasi + perintah `bertahan`
#   ./install.sh --system   -> /opt/bertahan (butuh sudo)
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

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

echo "Memasang Bertahan ke $PREFIX"
rm -rf "$PREFIX"
mkdir -p "$PREFIX" "$BIN" "$APPS" "$ICONS"
cp -r "$HERE/app/." "$PREFIX/"
chmod +x "$PREFIX/Bertahan"
cp "$HERE/uninstall.sh" "$PREFIX/uninstall.sh"
chmod +x "$PREFIX/uninstall.sh"
cp "$HERE/bertahan.png" "$ICONS/bertahan.png"
ln -sf "$PREFIX/Bertahan" "$BIN/bertahan"
sed "s|@EXEC@|$PREFIX/Bertahan|; s|@PATH@|$PREFIX|" "$HERE/bertahan.desktop" > "$APPS/bertahan.desktop"
command -v update-desktop-database >/dev/null && update-desktop-database "$APPS" >/dev/null 2>&1 || true

# Game ini butuh Vulkan (wgpu) dan ALSA untuk suara
missing=()
for lib in libvulkan.so.1 libasound.so.2 libfontconfig.so.1 libX11.so.6; do
    ldconfig -p 2>/dev/null | grep -q "$lib" || missing+=("$lib")
done
if (( ${#missing[@]} )); then
    echo
    echo "Peringatan: pustaka berikut belum ditemukan: ${missing[*]}"
    echo "  Debian/Ubuntu: sudo apt install libvulkan1 mesa-vulkan-drivers libasound2 libfontconfig1 libx11-6"
    echo "  Fedora:        sudo dnf install vulkan-loader mesa-vulkan-drivers alsa-lib fontconfig libX11"
    echo "  Arch:          sudo pacman -S vulkan-icd-loader vulkan-radeon|vulkan-intel|nvidia-utils alsa-lib fontconfig libx11"
fi

echo
echo "Selesai! Jalankan dari menu aplikasi atau ketik: bertahan"
[[ ":$PATH:" == *":$BIN:"* ]] || echo "(Tambahkan $BIN ke PATH agar perintah 'bertahan' dikenali.)"
