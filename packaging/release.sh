#!/usr/bin/env bash
# Membuat paket rilis Bertahan (Linux, macOS, atau Git Bash di Windows).
#
#   packaging/release.sh                       # semua platform, versi dari Bertahan.csproj
#   packaging/release.sh --rid linux-x64       # satu platform (boleh diulang)
#   packaging/release.sh --version 1.2.0
#
# Hasil di dist/:
#   Bertahan-<v>-windows-x64.zip          (+ -setup.exe bila Inno Setup `iscc` tersedia)
#   Bertahan-<v>-linux-x64.tar.gz         (berisi install.sh / uninstall.sh)
#   Bertahan-<v>-macos-<arch>.dmg         (di macOS; di OS lain .tar.gz berisi Bertahan.app)
#   SHA256SUMS.txt
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/src/Bertahan/Bertahan.csproj"
PKG="$ROOT/packaging"
DIST="$ROOT/dist"
STAGE="$DIST/stage"

VERSION=""
RIDS=()
while (( $# )); do
    case "$1" in
        --version) VERSION="$2"; shift 2 ;;
        --rid) RIDS+=("$2"); shift 2 ;;
        -h|--help) sed -n '2,13p' "$0"; exit 0 ;;
        *) echo "Argumen tidak dikenal: $1" >&2; exit 1 ;;
    esac
done
[[ -n "$VERSION" ]] || VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROJECT")"
(( ${#RIDS[@]} )) || RIDS=(win-x64 linux-x64 osx-x64 osx-arm64)

echo "== Bertahan $VERSION: ${RIDS[*]}"
if [[ "$(uname -s)" == MINGW* || "$(uname -s)" == MSYS* ]] && [[ " ${RIDS[*]} " == *" linux-"* || " ${RIDS[*]} " == *" osx-"* ]]; then
    echo "Peringatan: di Git Bash, tar tidak bisa menandai file sebagai executable. Untuk paket Linux/macOS pakai packaging/release.ps1." >&2
fi
mkdir -p "$DIST"

publish() {
    local rid="$1" out="$STAGE/$1"
    rm -rf "$out"
    dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained true \
        -p:Version="$VERSION" -p:DebugType=None -p:DebugSymbols=false -o "$out"
}

zip_dir() { # zip_dir <archive> <parent dir> <folder name>
    local archive="$1" parent="$2" name="$3"
    rm -f "$archive"
    if command -v zip >/dev/null; then
        (cd "$parent" && zip -qr9 "$archive" "$name")
    elif [[ -x /c/Windows/System32/tar.exe ]]; then
        # the bsdtar bundled with Windows writes zip files too
        /c/Windows/System32/tar.exe -a -cf "$(cygpath -w "$archive")" -C "$(cygpath -w "$parent")" "$name"
    else
        (cd "$parent" && python3 -m zipfile -c "$archive" "$name")
    fi
}

find_iscc() {
    command -v iscc 2>/dev/null && return
    for p in "/c/Program Files (x86)/Inno Setup 6/ISCC.exe" "/c/Program Files/Inno Setup 6/ISCC.exe"; do
        [[ -x "$p" ]] && { echo "$p"; return; }
    done
}

package_windows() {
    local rid="$1" dir="$DIST/pkg/Bertahan"
    rm -rf "$DIST/pkg" && mkdir -p "$dir"
    cp -r "$STAGE/$rid/." "$dir/"
    zip_dir "$DIST/Bertahan-$VERSION-windows-x64.zip" "$DIST/pkg" Bertahan
    local iscc; iscc="$(find_iscc || true)"
    if [[ -n "$iscc" ]]; then
        local w=echo; command -v cygpath >/dev/null && w="cygpath -w"
        "$iscc" /Q "/DAppVersion=$VERSION" "/DSourceDir=$($w "$STAGE/$rid")" "/DOutputDir=$($w "$DIST")" "$($w "$PKG/windows/bertahan.iss")"
    else
        echo "   (Inno Setup tidak ditemukan: installer .exe dilewati, hanya .zip)"
    fi
}

package_linux() {
    local rid="$1" dir="$DIST/pkg/bertahan-$VERSION"
    rm -rf "$DIST/pkg" && mkdir -p "$dir/app"
    cp -r "$STAGE/$rid/." "$dir/app/"
    cp "$PKG/linux/install.sh" "$PKG/linux/uninstall.sh" "$PKG/linux/bertahan.desktop" "$dir/"
    cp "$PKG/icons/bertahan-256.png" "$dir/bertahan.png"
    cp "$ROOT/LICENSE" "$dir/"
    chmod +x "$dir/install.sh" "$dir/uninstall.sh" "$dir/app/Bertahan"
    tar -C "$DIST/pkg" -czf "$DIST/Bertahan-$VERSION-linux-x64.tar.gz" "bertahan-$VERSION"
}

package_macos() {
    local rid="$1" arch="${1#osx-}" app="$DIST/pkg/Bertahan.app"
    rm -rf "$DIST/pkg" && mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
    cp -r "$STAGE/$rid/." "$app/Contents/MacOS/"
    sed "s/@VERSION@/$VERSION/g" "$PKG/macos/Info.plist" > "$app/Contents/Info.plist"
    cp "$PKG/icons/bertahan.icns" "$app/Contents/Resources/"
    chmod +x "$app/Contents/MacOS/Bertahan"
    local base="$DIST/Bertahan-$VERSION-macos-$arch"
    if command -v hdiutil >/dev/null; then
        # tanpa Apple Developer ID: tanda tangan ad-hoc agar bisa jalan di Apple Silicon
        # (the SDK already signs the apphost, so a failure here is not fatal)
        codesign --force --deep --sign - "$app" || echo "   (codesign ad-hoc gagal; lanjut tanpa tanda tangan ulang)"
        ln -s /Applications "$DIST/pkg/Applications"
        rm -f "$base.dmg"
        hdiutil create -quiet -volname "Bertahan $VERSION" -srcfolder "$DIST/pkg" -ov -format UDZO "$base.dmg"
    else
        tar -C "$DIST/pkg" -czf "$base.tar.gz" Bertahan.app
    fi
}

for rid in "${RIDS[@]}"; do
    echo "== $rid: publish"
    publish "$rid"
    echo "== $rid: paket"
    case "$rid" in
        win-*) package_windows "$rid" ;;
        linux-*) package_linux "$rid" ;;
        osx-*) package_macos "$rid" ;;
        *) echo "RID tidak didukung: $rid" >&2; exit 1 ;;
    esac
done
rm -rf "$DIST/pkg"

(cd "$DIST" && for f in Bertahan-*; do
    if command -v sha256sum >/dev/null; then sha256sum "$f"; else shasum -a 256 "$f"; fi
done > SHA256SUMS.txt)
echo "== Selesai:"
ls -lh "$DIST" | grep -v stage
