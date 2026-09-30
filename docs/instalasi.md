# Instalasi dan Rilis

[Kembali ke indeks](README.md)

Paket Bertahan sudah membawa runtime .NET sendiri (*self-contained*), jadi pemain **tidak perlu memasang .NET**. Unduh paket untuk sistem operasimu dari halaman **[Releases](https://github.com/SyubadubinStudios/bertahan/releases)**.

| Platform | File | Isi |
|---|---|---|
| Windows 10/11 64-bit | `Bertahan-<versi>-windows-x64-setup.exe` | Installer: Start Menu, ikon Desktop (opsional), uninstaller |
| | `Bertahan-<versi>-windows-x64.zip` | Versi portabel, tanpa instalasi |
| Linux x64 | `Bertahan-<versi>-linux-x64.tar.gz` | Aplikasi + `install.sh` / `uninstall.sh` + entri menu aplikasi |
| macOS Apple Silicon (M1 ke atas) | `Bertahan-<versi>-macos-arm64.dmg` | `Bertahan.app` |
| macOS Intel | `Bertahan-<versi>-macos-x64.dmg` | `Bertahan.app` |
| | `SHA256SUMS.txt` | Checksum untuk memeriksa unduhan |

## Kebutuhan sistem

- **GPU** yang mendukung DirectX 12 atau Vulkan (Windows), Vulkan (Linux), atau Metal (macOS). Hampir semua kartu grafis dan GPU terintegrasi sejak sekitar 2015 bisa.
- RAM 4 GB, ruang disk sekitar 400 MB.
- Windows 10 atau 11 (64-bit), Linux x64 dengan X11 atau XWayland, macOS 13 atau lebih baru.

## Windows

**Installer:**
1. Jalankan `Bertahan-<versi>-windows-x64-setup.exe`.
2. Jika muncul **"Windows melindungi PC Anda"** (SmartScreen), klik **Info selengkapnya**, lalu **Tetap jalankan**. Peringatan ini muncul karena installer belum ditandatangani sertifikat berbayar.
3. Pilih apakah dipasang untuk kamu saja (tanpa admin) atau untuk semua pengguna, lalu ikuti langkahnya.
4. Mainkan dari Start Menu atau ikon Desktop.

Untuk menghapus: **Pengaturan > Aplikasi > Bertahan > Uninstall**.

**Portabel:** ekstrak `Bertahan-<versi>-windows-x64.zip` ke folder mana saja, lalu jalankan `Bertahan\Bertahan.exe`.

## Linux

```bash
tar -xzf Bertahan-<versi>-linux-x64.tar.gz
cd bertahan-<versi>
./install.sh            # untuk pengguna ini: ~/.local/share/bertahan, tanpa sudo
# atau
sudo ./install.sh --system   # untuk semua pengguna: /opt/bertahan
```

Setelah itu Bertahan muncul di menu aplikasi, dan bisa juga dijalankan dengan perintah `bertahan` (pastikan `~/.local/bin` ada di `PATH`). Tanpa instalasi pun bisa: jalankan `./app/Bertahan` langsung.

`install.sh` memeriksa pustaka yang dibutuhkan (Vulkan, ALSA, fontconfig, X11) dan memberi tahu perintah pemasangannya bila ada yang belum terpasang:

```bash
# Debian / Ubuntu
sudo apt install libvulkan1 mesa-vulkan-drivers libasound2 libfontconfig1 libx11-6
# Fedora
sudo dnf install vulkan-loader mesa-vulkan-drivers alsa-lib fontconfig libX11
```

Kartu NVIDIA memakai driver Vulkan dari driver proprietary NVIDIA.

Untuk menghapus: `./uninstall.sh` (atau `sudo ./uninstall.sh --system`). Bisa juga menjalankan `~/.local/share/bertahan/uninstall.sh`.

## macOS

1. Buka file `.dmg` yang sesuai dengan Mac-mu (**arm64** untuk M1/M2/M3/M4, **x64** untuk Intel).
2. Seret **Bertahan** ke folder **Applications**.
3. Saat pertama kali dibuka, macOS akan menolak karena aplikasi belum dinotarisasi Apple. Buka **System Settings > Privacy & Security**, lalu klik **Open Anyway** di bagian bawah. Cara lain lewat Terminal:

   ```bash
   xattr -dr com.apple.quarantine /Applications/Bertahan.app
   ```

## Data permainan

Pengaturan, Top Skor, dan petualangan tersimpan di satu file `settings.json`. File ini **tidak ikut terhapus** saat game di-uninstall.

| Platform | Lokasi |
|---|---|
| Windows | `%APPDATA%\Bertahan\settings.json` |
| Linux | `~/.config/Bertahan/settings.json` |
| macOS | `~/.config/Bertahan/settings.json` |

Bila layar game hitam, pilih GPU lain di menu **Pilihan > Grafis**, atau jalankan dengan variabel lingkungan `WGPU_BACKEND=vulkan` (atau `dx12` di Windows).

---

## Membuat rilis (untuk pengembang)

### Otomatis lewat GitHub Actions

Workflow [`.github/workflows/release.yml`](../.github/workflows/release.yml) membangun paket di OS aslinya masing-masing: Windows (zip + installer Inno Setup), Linux (tar.gz), dan macOS (dmg Intel dan Apple Silicon, ditandatangani ad-hoc).

```bash
# 1. naikkan <Version> di src/Bertahan/Bertahan.csproj, commit
# 2. buat tag dan push
git tag v1.0.0
git push origin v1.0.0
```

Tag `v*` memicu build di ketiga OS, lalu semua paket dan `SHA256SUMS.txt` diterbitkan di GitHub Releases. Versi paket diambil dari nama tag. Workflow juga bisa dijalankan manual (**Actions > Rilis > Run workflow**); hasilnya tersedia sebagai *artifact*, tanpa membuat rilis.

### Manual dari komputer sendiri

Semua paket bisa dibuat dari satu komputer (publish lintas platform), hasilnya di folder `dist/`:

```powershell
# Windows (PowerShell)
powershell -ExecutionPolicy Bypass -File packaging\release.ps1
powershell -ExecutionPolicy Bypass -File packaging\release.ps1 -Rids win-x64 -Version 1.2.0
```

```bash
# Linux / macOS
packaging/release.sh
packaging/release.sh --rid linux-x64 --version 1.2.0
```

| Paket | Dibuat di Windows | Dibuat di Linux | Dibuat di macOS |
|---|---|---|---|
| Windows `.zip` | ya | ya | ya |
| Windows installer `-setup.exe` | ya, bila [Inno Setup 6](https://jrsoftware.org/isinfo.php) terpasang (`winget install JRSoftware.InnoSetup`) | tidak | tidak |
| Linux `.tar.gz` | ya (`release.ps1`) | ya | ya |
| macOS `.dmg` | tidak (dapat `.tar.gz` berisi `Bertahan.app`) | tidak (`.tar.gz`) | ya |

`release.ps1` menandai file executable di arsip tar dengan manifest mtree, jadi paket Linux/macOS buatan Windows tetap bisa langsung dijalankan. `release.sh` di Git Bash tidak bisa melakukannya, jadi di Windows pakailah `release.ps1`.

### Isi folder `packaging/`

| File | Fungsi |
|---|---|
| `release.ps1`, `release.sh` | Publish self-contained per platform (`win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`), kemas, dan tulis `SHA256SUMS.txt` |
| `windows/bertahan.iss` | Skrip installer Inno Setup (per-pengguna atau semua pengguna, Start Menu, Desktop, uninstaller) |
| `linux/install.sh`, `uninstall.sh`, `bertahan.desktop` | Pemasang untuk Linux dan entri menu aplikasi |
| `macos/Info.plist` | Templat bundel `Bertahan.app` (`@VERSION@` diganti saat build) |
| `icons/make_icons.py` | Membuat ikon aplikasi (`.ico`, `.icns`, `.png`) dari render Blender di `Assets/UI`. Hasilnya di-commit; jalankan ulang bila ikon zombi berubah |

Versi aplikasi ada di `<Version>` pada `Bertahan.csproj`. Nilainya tampil di pojok layar judul dan di nama paket.

Paket belum ditandatangani sertifikat (Authenticode di Windows, Developer ID di macOS). Karena itu pemain melihat peringatan SmartScreen atau Gatekeeper seperti dijelaskan di atas. Bila kelak punya sertifikat, tambahkan langkah `signtool` di `release.ps1` dan `codesign --sign "Developer ID ..."` plus `notarytool` di `release.sh`.
