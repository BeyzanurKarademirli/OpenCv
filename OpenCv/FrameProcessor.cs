using System;
using System.IO;
using OpenCvSharp;

namespace OpenCv
{
    /// <summary>Kameradan gelen kareye uygulanabilecek görüntü filtreleri.</summary>
    public enum FilterMode
    {
        Normal,
        Gri,
        Kenar,
        Bulanik,
        Karakalem,
        Negatif
    }

    /// <summary>
    /// Kareleri filtreler ve (isteğe bağlı) Haar Cascade ile yüz algılar.
    /// Yalnızca kamera iş parçacığından kullanılır.
    /// </summary>
    internal sealed class FrameProcessor : IDisposable
    {
        private const double DetectionWidth = 640.0;

        private readonly CascadeClassifier faceCascade;

        public FrameProcessor(string cascadePath)
        {
            if (!File.Exists(cascadePath)) return;

            var cascade = new CascadeClassifier(cascadePath);
            if (cascade.Empty())
                cascade.Dispose();
            else
                faceCascade = cascade;
        }

        public bool FaceDetectionAvailable
        {
            get { return faceCascade != null; }
        }

        public static string GetFilterName(FilterMode mode)
        {
            switch (mode)
            {
                case FilterMode.Gri: return "Gri";
                case FilterMode.Kenar: return "Kenar";
                case FilterMode.Bulanik: return "Bulanık";
                case FilterMode.Karakalem: return "Karakalem";
                case FilterMode.Negatif: return "Negatif";
                default: return "Normal";
            }
        }

        /// <summary>
        /// Filtrelenmiş yeni bir kare döndürür. Dönen <see cref="Mat"/> çağıranındır, dispose edilmelidir.
        /// </summary>
        public Mat Process(Mat source, FilterMode mode, bool detectFaces, out int faceCount)
        {
            Mat output = ApplyFilter(source, mode);
            faceCount = 0;

            if (detectFaces && faceCascade != null)
            {
                Rect[] faces = DetectFaces(source);
                faceCount = faces.Length;
                foreach (Rect face in faces)
                    Cv2.Rectangle(output, face, new Scalar(0, 220, 0), 2);
            }

            return output;
        }

        private static Mat ApplyFilter(Mat source, FilterMode mode)
        {
            var result = new Mat();

            switch (mode)
            {
                case FilterMode.Gri:
                    using (var gray = ToGray(source))
                        Cv2.CvtColor(gray, result, ColorConversionCodes.GRAY2BGR);
                    break;

                case FilterMode.Kenar:
                    using (var gray = ToGray(source))
                    using (var edges = new Mat())
                    {
                        Cv2.GaussianBlur(gray, gray, new Size(5, 5), 0);
                        Cv2.Canny(gray, edges, 50, 150);
                        Cv2.CvtColor(edges, result, ColorConversionCodes.GRAY2BGR);
                    }
                    break;

                case FilterMode.Bulanik:
                    Cv2.GaussianBlur(source, result, new Size(21, 21), 0);
                    break;

                case FilterMode.Karakalem:
                    using (var gray = ToGray(source))
                    using (var blurred = new Mat())
                    using (var sketch = new Mat())
                    {
                        // "Renk soldurma" (dodge) yöntemi: gri / bulanık gri
                        Cv2.GaussianBlur(gray, blurred, new Size(21, 21), 0);
                        Cv2.Divide(gray, blurred, sketch, 256.0);
                        Cv2.CvtColor(sketch, result, ColorConversionCodes.GRAY2BGR);
                    }
                    break;

                case FilterMode.Negatif:
                    Cv2.BitwiseNot(source, result);
                    break;

                default:
                    source.CopyTo(result);
                    break;
            }

            return result;
        }

        private Rect[] DetectFaces(Mat source)
        {
            using (var gray = ToGray(source))
            using (var small = new Mat())
            {
                // Büyük karelerde algılamayı hızlandırmak için küçült
                double scale = source.Width > DetectionWidth ? DetectionWidth / source.Width : 1.0;
                Mat target = gray;
                if (scale < 1.0)
                {
                    Cv2.Resize(gray, small, new Size(0, 0), scale, scale, InterpolationFlags.Area);
                    target = small;
                }

                Cv2.EqualizeHist(target, target);
                Rect[] faces = faceCascade.DetectMultiScale(
                    target, 1.1, 5, HaarDetectionTypes.ScaleImage, new Size(40, 40));

                if (scale < 1.0)
                {
                    for (int i = 0; i < faces.Length; i++)
                    {
                        Rect r = faces[i];
                        faces[i] = new Rect(
                            (int)(r.X / scale), (int)(r.Y / scale),
                            (int)(r.Width / scale), (int)(r.Height / scale));
                    }
                }

                return faces;
            }
        }

        private static Mat ToGray(Mat source)
        {
            var gray = new Mat();
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }

        public void Dispose()
        {
            if (faceCascade != null)
                faceCascade.Dispose();
        }
    }
}
