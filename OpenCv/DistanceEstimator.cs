using System;
using System.Globalization;
using System.IO;
using OpenCvSharp;

namespace OpenCv
{
    /// <summary>Ekrandaki bir nesne ve onun hakkında hesaplanan bilgiler.</summary>
    public sealed class TrackedObject
    {
        public Detection Detection;
        public ObjectClassInfo Info;

        /// <summary>Tahmini uzaklık (metre).</summary>
        public double DistanceM;

        /// <summary>Mesafenin neye göre hesaplandığı ("boyundan", "genişliğinden").</summary>
        public string Basis;

        /// <summary>Nesne kadrajın birden fazla kenarından taşıyorsa true; mesafe en fazla bu kadardır.</summary>
        public bool TooClose;

        public double WidthM;
        public double HeightM;

        /// <summary>Merkezin kare içindeki göreli konumu (0-1).</summary>
        public double CenterX;
        public double CenterY;

        public string Position
        {
            get
            {
                string horizontal = CenterX < 0.33 ? "solunda" : CenterX > 0.67 ? "sağında" : "ortasında";
                string vertical = CenterY < 0.33 ? "üst " : CenterY > 0.67 ? "alt " : "";
                return "Görüntünün " + vertical + horizontal;
            }
        }

        public string DistanceText
        {
            get
            {
                string value = DistanceM < 1
                    ? Math.Round(DistanceM * 100).ToString("0", CultureInfo.GetCultureInfo("tr-TR")) + " cm"
                    : DistanceM.ToString("0.0", CultureInfo.GetCultureInfo("tr-TR")) + " m";
                return (TooClose ? "< " : "~") + value;
            }
        }
    }

    /// <summary>
    /// Tek kamerayla mesafe tahmini (iğne deliği kamera modeli):
    /// mesafe = gerçek boyut × odak uzaklığı (piksel) / görüntüdeki boyut (piksel).
    /// Odak uzaklığı varsayılan olarak 60° yatay görüş açısına göre alınır;
    /// bilinen bir mesafeyle kalibre edilerek doğruluk artırılabilir.
    /// </summary>
    internal sealed class DistanceEstimator
    {
        // 60° yatay görüş açısı: f = (genişlik / 2) / tan(30°)
        private const double DefaultFocalRatio = 0.866;
        private const int EdgeMargin = 3;

        private readonly string calibrationPath;
        private double focalRatio = DefaultFocalRatio;

        public DistanceEstimator(string calibrationPath)
        {
            this.calibrationPath = calibrationPath;
            Load();
        }

        public bool IsCalibrated
        {
            get { return Math.Abs(focalRatio - DefaultFocalRatio) > 1e-6; }
        }

        public TrackedObject Describe(Detection detection, int frameWidth, int frameHeight)
        {
            ObjectClassInfo info = ObjectCatalog.Get(detection.ClassId);
            Rect box = detection.Box;
            double focal = focalRatio * frameWidth;

            bool clippedVertical = box.Top <= EdgeMargin || box.Bottom >= frameHeight - EdgeMargin;
            bool clippedHorizontal = box.Left <= EdgeMargin || box.Right >= frameWidth - EdgeMargin;

            bool useHeight = ChooseHeight(info, clippedVertical, clippedHorizontal);
            double realSize = useHeight ? info.HeightM : info.WidthM;
            double pixelSize = Math.Max(1, useHeight ? box.Height : box.Width);
            double distance = realSize * focal / pixelSize;

            return new TrackedObject
            {
                Detection = detection,
                Info = info,
                DistanceM = distance,
                Basis = useHeight ? "boyundan" : "genişliğinden",
                TooClose = clippedVertical && clippedHorizontal,
                WidthM = box.Width * distance / focal,
                HeightM = box.Height * distance / focal,
                CenterX = (box.X + box.Width / 2.0) / frameWidth,
                CenterY = (box.Y + box.Height / 2.0) / frameHeight
            };
        }

        /// <summary>
        /// Nesnenin bilinen uzaklığını kullanarak odak uzaklığını yeniden hesaplar ve kaydeder.
        /// </summary>
        public void Calibrate(TrackedObject obj, double realDistanceM)
        {
            if (realDistanceM <= 0 || obj.DistanceM <= 0) return;

            // Tahmin, odak uzaklığıyla doğru orantılıdır.
            focalRatio *= realDistanceM / obj.DistanceM;
            Save();
        }

        public void Reset()
        {
            focalRatio = DefaultFocalRatio;
            Save();
        }

        private static bool ChooseHeight(ObjectClassInfo info, bool clippedVertical, bool clippedHorizontal)
        {
            // Kadrajdan taşan kenar güvenilmez; diğerini kullan.
            if (clippedVertical && !clippedHorizontal) return false;
            if (clippedHorizontal && !clippedVertical) return true;

            // Yükseklik bakış açısından (önden/yandan) çok daha az etkilenir;
            // genişliği yalnızca pizza, klavye gibi yassı nesnelerde kullan.
            return info.HeightM >= info.WidthM * 0.4;
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(calibrationPath)) return;
                double value;
                if (double.TryParse(File.ReadAllText(calibrationPath).Trim(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out value) && value > 0.1 && value < 10)
                    focalRatio = value;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(calibrationPath));
                File.WriteAllText(calibrationPath, focalRatio.ToString("R", CultureInfo.InvariantCulture));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
