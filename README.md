# OpenCV Kamera

OpenCvSharp ile bilgisayar kamerasından canlı görüntü alan bir Windows Forms uygulaması. Görüntüye gerçek zamanlı filtreler uygular, yüz algılar, fotoğraf çeker ve video kaydeder.

## Özellikler
- Canlı kamera görüntüsü (en-boy oranı korunur), FPS ve çözünürlük göstergesi
- 6 filtre: Normal, Gri, Kenar (Canny), Bulanık, Karakalem, Negatif
- Haar Cascade ile yüz algılama ve yüz sayısı
- Ayna görüntüsü aç/kapat
- Fotoğraf çekme (PNG) ve video kaydı (AVI, MJPG)
- Birden fazla kamera arasında geçiş
- Kamera açılamazsa ya da bağlantı kesilirse ekranda uyarı

## Kısayollar

| Tuş | İşlev |
|-----|-------|
| `S` | Kamerayı başlat |
| `Q` | Kamerayı durdur |
| `1`–`6` | Filtre seç (Normal, Gri, Kenar, Bulanık, Karakalem, Negatif) |
| `F` | Yüz algılamayı aç/kapat |
| `M` | Ayna görüntüsünü aç/kapat |
| `C` | Sonraki kameraya geç |
| `P` | Fotoğraf çek |
| `R` | Video kaydını başlat/durdur |
| `H` | Yardım panelini göster/gizle |
| `Esc` | Çıkış |

Fotoğraflar `Resimler\OpenCV Kamera`, videolar `Videolar\OpenCV Kamera` klasörüne kaydedilir.

## Kurulum
1. Projeyi klonlayın:
   ```
   git clone https://github.com/BeyzanurKarademirli/OpenCv.git
   ```
2. `OpenCv.sln` dosyasını Visual Studio ile açın. NuGet paketleri ilk derlemede otomatik geri yüklenir:
   - `OpenCvSharp4`
   - `OpenCvSharp4.Extensions`
   - `OpenCvSharp4.runtime.win`
3. Projeyi çalıştırın (`F5`).

## Proje yapısı
- `OpenCv/Form1.cs`: Arayüz, klavye kısayolları ve kamera iş parçacığı
- `OpenCv/FrameProcessor.cs`: Filtreler ve yüz algılama
- `OpenCv/Data/haarcascade_frontalface_default.xml`: OpenCV'nin yüz algılama modeli (derlemede çıktı klasörüne kopyalanır)

## Gereksinimler
- Windows işletim sistemi
- .NET Framework 4.7.2
- Bir web kamerası
