# Cara Bermain

[Kembali ke indeks](README.md)

Wabah zombi datang dari kuburan tua dan menyerang kampung demi kampung. Tugasmu adalah bertahan melewati setiap **gelombang** zombi di tiap level, mengalahkan **bos** di gelombang terakhir, lalu membawa keluargamu ke level berikutnya.

![Pertarungan di Gerbang Kampung](images/level-1.jpg)

## Kontrol

| Tombol | Aksi |
|---|---|
| W A S D / Panah | Bergerak (relatif terhadap kamera) |
| Mouse | Membidik; pemain menghadap ke kursor |
| Klik kiri (tahan) / J | Menyerang; menembak saat memegang senapan |
| Klik kanan / G / L | Lempar bom molotov ke arah bidikan |
| Spasi / K | Berguling: kebal sekitar 0,45 detik, menghabiskan 20 tenaga |
| Shift (tahan) | Berlari: lebih cepat 45%, menghabiskan tenaga |
| R | Isi ulang senapan (otomatis juga saat peluru habis) |
| 1 - 9 / roda mouse | Ganti senjata di tas |
| Q / E | Putar kamera |
| + / - | Dekatkan / jauhkan kamera |
| Esc / P | Istirahat (pause) |

Game juga otomatis masuk mode istirahat ketika jendela kehilangan fokus.

## Keluarga pahlawan

![Keluarga](images/cast-keluarga.png)

| Karakter | Peran | Darah | Kecepatan | Pukulan | Kegesitan | Senjata awal | Keistimewaan |
|---|---|---|---|---|---|---|---|
| **Bapak** | Kepala keluarga | 130 | 5,0 | x1,2 | x1,0 | Bambu Runcing | Darah paling tebal, tusukan jarak jauh |
| **Ibu** | Jagoan dapur | 115 | 5,2 | x1,1 | x1,05 | Wajan | Wajan membuat zombi pusing |
| **Kakak** | Anak SD pemberani | 100 | 5,7 | x1,0 | x1,15 | Pentungan | Lincah, serangan cepat |
| **Ade** | Anggota Pramuka | 85 | 6,3 | x0,9 | x1,3 | Pentungan | Paling gesit, gulingan lebih jauh |
| **Kake** | Mantan jawara kampung | 100 | 4,6 | x1,4 | x0,9 | Pentungan | Pukulan jurus silat paling sakit |
| **Nene** | Ratu sapu lidi | 95 | 4,7 | x1,0 | x1,0 | Sapu Lidi | Jamu rahasia: darah pulih 2 per detik |

## Senjata

![Senjata](images/senjata.png)

Senjata tambahan tergeletak di peta dan bisa dipungut dengan berjalan di atasnya. Semua senjata yang dipungut masuk ke tas, dan kamu bisa berganti senjata dengan tombol 1-9.

| Senjata | Jenis | Damage | Jangkauan | Jeda | Catatan |
|---|---|---|---|---|---|
| Pentungan | Ayun | 22 | 1,7 m | 0,42 s | Kayu keras andalan ronda malam |
| Sapu Lidi | Ayun | 14 | 2,1 m | 0,34 s | Busur ayunan paling lebar (150°), dorongan kuat |
| Wajan | Ayun | 28 | 1,6 m | 0,55 s | Membuat zombi pusing (stun 0,9 s) |
| Bambu Runcing | Tusuk | 32 | 2,8 m | 0,58 s | Menembus hingga 3 zombi dalam satu garis |
| Linggis | Ayun | 36 | 1,9 m | 0,60 s | Berat dan tangguh |
| Kampak | Ayun | 48 | 1,8 m | 0,75 s | Tebasan keras |
| Pacul | Ayun | 60 | 2,1 m | 0,95 s | Paling mematikan, dorongan terbesar |
| Senapan | Tembak | 65 | 30 m | 0,55 s | Magasin 6, menembus 2 zombi, isi ulang 1,6 s |
| Bom Molotov | Lempar | 35 + api | area ~3,4 m | - | Api membakar 6 detik (9 damage tiap 1/3 detik) |

Damage akhir dikalikan dengan **Pukulan** karakter. Kecepatan serangan dipengaruhi **Kegesitan**. Serangan berat memberi efek *hit-stop* dan guncangan kamera.

## Barang pungutan

| Barang | Efek |
|---|---|
| Nasi bungkus | +35 darah |
| Jamu | +70 darah |
| Peti peluru | +12 peluru senapan |
| Botol molotov | +2 bom molotov |
| Senjata | Masuk ke tas; peluru +12 bila senapan sudah dimiliki |

Setiap kali gelombang selesai, perbekalan baru muncul di peta. Zombi yang kalah juga kadang menjatuhkan nasi, peluru, atau molotov. Pemain memulai level dengan 2 molotov dan 12 peluru cadangan.

## Para zombi

![Zombi](images/cast-zombi.png)

| Zombi | Darah | Jalan / lari | Damage | Skor | Perilaku |
|---|---|---|---|---|---|
| Zombi Warga | 55 | 1,3 / 2,4 | 10 | 10 | Zombi dasar yang mengejar lewat jalan kampung |
| Tuyul Zombi | 32 | 3,2 / 4,6 | 6 | 15 | Sangat cepat dan bergerak zig-zag |
| Pocong Zombi | 75 | 1,9 / 2,9 | 12 | 20 | Hanya bergerak saat melompat; tidak terhambat air sawah |
| Satpam Zombi | 160 | 1,4 / 2,3 | 18 | 40 | Tebal dan memukul keras |
| Kuntilanak Zombi | 110 | 2,0 / 3,2 | 12 | 50 | Jeritannya memperlambat pemain 2,5 detik |
| Genderuwo Zombi (bos) | 480 | 1,7 / 3,4 | 28 | 150 | Menyeruduk (30 damage) dan hentakan tanah; pusing bila menabrak tembok |
| Dukun Zombi (bos akhir) | 1500 | 1,4 / 2,0 | 20 | 600 | Menjaga jarak, menembakkan bola api (5 bola saat darah < 50%), memanggil anak buah tiap 14 detik |

Zombi makin kuat di level yang lebih tinggi: darah +10%, kecepatan +6%, dan damage +12% per level.

## Level

![Peta petualangan](images/peta.jpg)

| Level | Nama | Zona | Waktu dan cuaca | Bos |
|---|---|---|---|---|
| 1 | Gerbang Kampung | Zona Aman | Siang cerah, berangin | Satpam Zombi (penutup gelombang terakhir) |
| 2 | Sawah Berhantu | Zona Bahaya | Malam berkabut, kunang-kunang | Genderuwo Zombi |
| 3 | Pasar Lama | Zona Menengah | Sore, gerimis | 2 Genderuwo Zombi |
| 4 | Kuburan Terbengkalai | Zona Berisiko | Malam, hujan, petir, kabut hijau | Dukun Zombi dan Genderuwo |
| 5 - 10 | Hutan Bambu, Pantai Nelayan, Pabrik Gula, Stasiun Tua, Kota Kabupaten, Puncak Gunung | - | - | Segera hadir |

Setiap level terdiri dari **4 gelombang**. Sebelum tiap gelombang ada hitungan mundur ("Kentongan berbunyi, zombi datang!"), dan setelah gelombang selesai ada jeda singkat untuk memungut perbekalan. Gelombang ke-4 adalah gelombang terakhir ("GELOMBANG BOS") dengan musik khusus. Bos besar (Genderuwo dan Dukun) muncul dengan bilah darah sendiri di atas layar.

## Tingkat kesulitan

![Tingkat kesulitan](images/kesulitan.jpg)

| | Bayi | Pemberani | Mimpi Buruk |
|---|---|---|---|
| Nyawa per level | 5 | 3 | 2 |
| Darah zombi | x0,7 | x1,0 | x1,35 |
| Damage zombi | x0,55 | x1,0 | x1,5 |
| Kecepatan zombi | x0,88 | x1,0 | x1,12 |
| Pengali skor | x0,75 | x1,0 | x1,5 |
| Peluang barang jatuh | x1,5 | x1,0 | x0,7 |

## Nyawa dan kesempatan ulang

- Setiap level dimulai dengan jumlah **nyawa** sesuai tingkat kesulitan (hati di HUD).
- Saat darah habis, pahlawan **pingsan**. Jika masih ada nyawa, ia bangkit di tempat yang sama dengan darah penuh, kebal selama 3 detik, dan zombi di sekitarnya terlempar menjauh.
- Jika **nyawa habis**, level gagal dan harus **diulang**. Setiap petualangan punya **3 kesempatan mengulang** per level ("Ulang 3" di HUD).
- Jika kesempatan mengulang juga habis, muncul **GAME OVER** dan petualangan **dimulai lagi dari Level 1** dengan skor total kembali nol.
- Menamatkan sebuah level mengembalikan kesempatan mengulang menjadi 3.
- Kemajuan petualangan tersimpan otomatis. Menu **LANJUTKAN** membuka Peta Petualangan di level terakhir.

## Skor

- **Mengalahkan zombi:** skor zombi x pengali kombo x pengali kesulitan.
- **Kombo:** setiap zombi yang kalah dalam 3 detik sejak yang sebelumnya menambah kombo. Pengalinya adalah 1 + (kombo / 5), maksimal x5.
- **Gelombang selesai:** bonus 100 x nomor gelombang.
- **Level selesai:** semua bonus berikut dikalikan pengali kesulitan.
  - Bonus waktu: 3000 - 5 per detik bermain.
  - Bonus darah: 5 x sisa darah.
  - Bonus nyawa: 250 per nyawa cadangan.
- **Bintang (1-3):** 1 bintang untuk menang, +1 bila tidak kehilangan nyawa, dan +1 bila darah di atas 50%.
- Skor level masuk ke **Top Skor** level tersebut (10 terbaik), termasuk percobaan yang gagal (ditandai "gugur").

## Tips

- Berguling membuatmu kebal sesaat. Gunakan untuk lolos dari seruduk Genderuwo.
- Lempar molotov ke kerumunan. Api terus membakar zombi yang melintas.
- Pocong hanya bergerak di udara. Pukul saat ia mendarat.
- Jauhi Kuntilanak saat ia mulai menjerit.
- Air sawah memperlambat semua orang, kecuali pocong dan kuntilanak.
- Dukun Zombi menjaga jarak. Kejar dia atau pakai senapan.
