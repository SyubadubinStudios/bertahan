# Panduan Pengembangan

[Kembali ke indeks](README.md)

## Build dan jalankan

```bash
dotnet build src/Bertahan/Bertahan.csproj
dotnet run --project src/Bertahan
```

Paket utama: `Avalonia` 12, `ThreeNet`, `ThreeNet.Avalonia`, dan `ThreeNet.Native` 0.6.0. Aset di `src/Bertahan/Assets/**` disalin ke folder output. Proyek ini tidak punya test project. Verifikasi dilakukan lewat **mode screenshot** dan **autoplay** di bawah.

## Mode screenshot dan autoplay

Game bisa dijalankan tanpa interaksi untuk mengambil gambar satu layar lalu menutup diri. Mode inilah yang dipakai untuk membuat semua screenshot dokumentasi.

```bash
Bertahan.exe --shot out.png --scene <scene> [opsi]
```

Selain `out.png`, dibuat juga `out.png.txt` berisi ringkasan: gelombang, fase, skor, kills, darah, nyawa, state, jumlah tanaman berayun, FPS, draw call, dan backend.

| Opsi | Arti |
|---|---|
| `--scene` | `title`, `name`, `difficulty`, `chars`, `map`, `loading`, `scores`, `options`, `controls`, `about`, `ending`, `opening`, `level` |
| `--frames N` | Jumlah frame dirender sebelum capture (default 30) |
| `--warp S` | Maju cepat S detik simulasi: waktu desa di menu, atau lama pertarungan di level |
| `--time S` | Posisi waktu untuk `opening` dan `about` |
| `--level N` | Level (untuk `level`, `map`, `loading`) |
| `--char id`, `--name "..."`, `--difficulty Bayi\|Pemberani\|MimpiBuruk`, `--retries N` | Data petualangan |
| `--autoplay` | Bot `Autopilot` yang bermain: mendekati zombi, menyerang, melempar molotov, memungut barang |
| `--overlay pause\|result\|preview` | Buka pause, tampilkan layar hasil (bila level selesai dalam `--warp`), atau ambil gambar sinematik tanpa HUD |
| `--quality 0-2` | Kualitas grafik (default 2 di mode ini) |

Contoh:

```bash
# desa di menu setelah 12 detik
Bertahan.exe --shot menu.png --scene title --warp 12
# bot bermain Level 1 sampai selesai, lalu layar hasil
Bertahan.exe --shot win.png --scene level --level 1 --warp 480 --autoplay --difficulty Bayi --overlay result --frames 60
# adegan ke-2 cerita pembuka
Bertahan.exe --shot opening.png --scene opening --time 12
```

Variabel lingkungan:
- `BERTAHAN_SETTINGS=<path>` memakai file pengaturan lain, sehingga pengaturan dan Top Skor asli tidak tersentuh.
- `BERTAHAN_LOG=<path>` menulis log (backend, pembuatan renderer).
- `WGPU_BACKEND=dx12|vulkan` memaksa backend GPU.

Tekan **F12** saat bermain untuk menyimpan screenshot ke `Pictures/Bertahan`.

## Catatan teknis Three.Net

Beberapa hal penting yang ditemukan saat pengembangan:

1. **Satu renderer untuk satu scene.** Renderer Three.Net menyimpan cache sumber daya GPU berdasarkan handle. Jika renderer yang sama menggambar scene lain, mesh dan material bisa tertukar (misalnya bagian tubuh karakter memakai warna model lain). Karena itu `MainWindow.SetScene` **membuat ulang `ThreeNetView`** setiap kali scene berganti (cerita, menu, atau level).
2. **DX12 + MSAA pada sebagian GPU AMD** (misalnya RX 640) menghasilkan frame hitam pada target offscreen berukuran 256 px ke atas. `GpuBackend` menguji render 256 px dengan MSAA 4x lalu beralih ke Vulkan bila hasilnya hitam. Hasil uji disimpan (`ProbedBackend`, `ProbeVersion`).
3. **Kabut linear bawaan.** Selain `FogDensity` (eksponensial kuadrat), `SceneEnvironment.FogStart`/`FogEnd` (bawaan 10-100 m) juga berlaku dan membuat semua yang jauh, termasuk langit, menjadi putih. Semua scene mengatur `FogStart = 1000` dan `FogEnd = 5000`.
4. **Hindari `Geometry.ComputeTangents()` tanpa normal map.** Hasilnya bidang tanah tidak tergambar.
5. **Hindari material PBR untuk bidang datar yang sangat jauh.** Pada sudut landai ia memantulkan langit dan tampak seperti pita pucat. Gunakan Lambert.
6. **Tampilan hanya mulai merender setelah punya `Scene`.** Karena itu shell memasang scene kosong saat boot.
7. **Shader hook:** `Scene.CreateShader(source, ShaderLanguage.Glsl, name)`. Fungsi yang tersedia adalah `vec3 user_vertex(VertexContext)` dan `Surface user_surface(SurfaceContext, Surface)`, dengan akses ke `time`, `uv`, `custom0`, dan `custom1`. Dipakai untuk hujan.
8. `Node.Visible` tidak punya getter, jadi simpan status sendiri. `Node.Light` bertipe `Light?` (struct), jadi ubah dengan `light.Value with { ... }`.
9. Material hasil impor GLB tidak bisa diambil dari node. Untuk animasi angin, yang digoyang adalah **node model**-nya (anak dari pivot `PropLibrary.Place`), bukan vertex-nya.

## Konvensi kode

- Semua teks di dalam game berbahasa Indonesia. Id aset dan data juga berbahasa Indonesia (`gerbang`, `sawah`, `pasar`, `kuburan`, `Siang`/`Sore`/`Malam`).
- Tabel keseimbangan game ada di `Game/Defs.cs` dan `Game/Difficulty.cs`.
- UI dibangun dengan C# (bukan XAML) di atas `Kit` (warna, panel, teks) dan `MenuButton`. Layar baru diturunkan dari `Screen`.
