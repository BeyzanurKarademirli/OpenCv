# OpenCV Kamera

OpenCvSharp ile bilgisayar kamerasından canlı görüntü alan bir Windows Forms uygulaması. Görüntüdeki nesneleri tanır, her birinin kameraya uzaklığını tahmin eder ve nesne hakkında bilgi verir. Ayrıca gerçek zamanlı filtreler uygular, yüz algılar, fotoğraf çeker ve video kaydeder. Tamamen ücretsizdir; nesne tanıma internet olmadan çalışır.

## Özellikler
- **Nesne tanıma:** 80 farklı nesne türü (kişi, sandalye, telefon, kitap, araba, kedi...) YOLOv4-tiny modeliyle, internet gerektirmeden
- **Mesafe tahmini:** Her nesnenin kameraya yaklaşık uzaklığı ve tahmini gerçek boyutu
- **Nesne bilgisi:** Türkçe ad, kategori, güven oranı, konum, kısa açıklama ve (internet varsa) Türkçe Vikipedi özeti
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
| `O` | Nesne tanımayı aç/kapat (sağda nesne paneli açılır) |
| `K` | Seçili nesneyle mesafe kalibrasyonu |
| Fare tıklaması | Görüntüdeki nesneyi seç ve bilgilerini göster |
| `1`–`6` | Filtre seç (Normal, Gri, Kenar, Bulanık, Karakalem, Negatif) |
| `F` | Yüz algılamayı aç/kapat |
| `M` | Ayna görüntüsünü aç/kapat |
| `C` | Sonraki kameraya geç |
| `P` | Fotoğraf çek |
| `R` | Video kaydını başlat/durdur |
| `H` | Yardım panelini göster/gizle |
| `Esc` | Çıkış |

Fotoğraflar `Resimler\OpenCV Kamera`, videolar `Videolar\OpenCV Kamera` klasörüne kaydedilir. Nesne etiketleri fotoğraf ve videolara da işlenir.

## Mesafe nasıl hesaplanıyor?
Tek kamerayla derinlik doğrudan ölçülemez, bu yüzden her nesne türünün ortalama gerçek boyutu kullanılır (örneğin kişi ≈ 1,70 m, sandalye ≈ 0,90 m):

```
mesafe = gerçek boyut × odak uzaklığı (piksel) / görüntüdeki boyut (piksel)
```

- Varsayılan olarak tipik bir web kamerası (≈ 60° görüş açısı) kabul edilir.
- Nesne kadrajdan taşıyorsa (örneğin kameraya yakın oturan birinin sadece üst gövdesi görünüyorsa) taşmayan kenar kullanılır.
- **Kalibrasyon:** Bir nesneyi (örneğin kendinizi) bilinen bir uzaklığa koyun, listeden seçin, `K` tuşuna basıp gerçek mesafeyi metre olarak yazın. Ayar kaydedilir ve sonraki açılışlarda da kullanılır. `0` yazmak varsayılana döndürür.
- Sonuçlar yaklaşıktır; nesnenin gerçek boyutu ortalamadan farklıysa (çocuk, büyük köpek vb.) tahmin de sapar.

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
- `OpenCv/ObjectDetector.cs`: YOLOv4-tiny ile nesne algılama (OpenCV DNN)
- `OpenCv/DistanceEstimator.cs`: Mesafe ve boyut tahmini, kalibrasyon
- `OpenCv/ObjectCatalog.cs`: 80 nesne türünün Türkçe adı, kategorisi, ortalama boyutları ve açıklaması
- `OpenCv/WikipediaClient.cs`: Türkçe Vikipedi'den ücretsiz özet (API anahtarı gerekmez)
- `OpenCv/Models/`: YOLOv4-tiny model dosyaları ([YOLO lisansı](OpenCv/Models/LICENSE-YOLO.txt), ücretsiz kullanım)
- `OpenCv/Data/haarcascade_frontalface_default.xml`: OpenCV'nin yüz algılama modeli (derlemede çıktı klasörüne kopyalanır)

## Gereksinimler
- Windows işletim sistemi
- .NET Framework 4.7.2
- Bir web kamerası
