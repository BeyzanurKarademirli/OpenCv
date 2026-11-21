using System;
using System.Collections.Generic;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace OpenCv
{
    /// <summary>Tek bir algılama sonucu (kare koordinatlarında).</summary>
    public sealed class Detection
    {
        public int ClassId;
        public float Confidence;
        public Rect Box;
    }

    /// <summary>
    /// YOLOv4-tiny (COCO, 80 sınıf) ile nesne algılama. Tamamen yerel ve ücretsiz çalışır;
    /// internet bağlantısı gerektirmez. Aynı anda tek iş parçacığından çağrılmalıdır.
    /// </summary>
    internal sealed class ObjectDetector : IDisposable
    {
        public const string ConfigFile = "yolov4-tiny.cfg";
        public const string WeightsFile = "yolov4-tiny.weights";

        private const int InputSize = 416;
        private const float ScoreThreshold = 0.4f;
        private const float NmsThreshold = 0.45f;

        private readonly Net net;
        private readonly string[] outputNames;

        private ObjectDetector(Net net)
        {
            this.net = net;
            outputNames = net.GetUnconnectedOutLayersNames();
        }

        /// <summary>Model dosyaları yoksa null döner.</summary>
        public static ObjectDetector TryLoad(string modelDirectory)
        {
            string cfg = Path.Combine(modelDirectory, ConfigFile);
            string weights = Path.Combine(modelDirectory, WeightsFile);
            if (!File.Exists(cfg) || !File.Exists(weights)) return null;

            Net net = CvDnn.ReadNetFromDarknet(cfg, weights);
            if (net == null || net.Empty()) return null;

            net.SetPreferableBackend(Backend.OPENCV);
            net.SetPreferableTarget(Target.CPU);
            return new ObjectDetector(net);
        }

        public List<Detection> Detect(Mat frame)
        {
            int width = frame.Width;
            int height = frame.Height;

            var boxes = new List<Rect>();
            var scores = new List<float>();
            var classIds = new List<int>();

            using (Mat blob = CvDnn.BlobFromImage(frame, 1 / 255.0, new Size(InputSize, InputSize),
                       new Scalar(), true, false))
            {
                net.SetInput(blob);
                var outputs = new Mat[outputNames.Length];
                for (int i = 0; i < outputs.Length; i++) outputs[i] = new Mat();

                try
                {
                    net.Forward(outputs, outputNames);

                    foreach (Mat output in outputs)
                    {
                        // Her satır: cx, cy, w, h, nesnellik, 80 sınıf skoru (0-1 arası, göreli)
                        int rows = output.Rows;
                        int cols = output.Cols;
                        float[] data;
                        output.GetArray(out data);

                        for (int r = 0; r < rows; r++)
                        {
                            int offset = r * cols;
                            int bestClass = -1;
                            float bestScore = 0;
                            for (int c = 5; c < cols; c++)
                            {
                                if (data[offset + c] > bestScore)
                                {
                                    bestScore = data[offset + c];
                                    bestClass = c - 5;
                                }
                            }

                            if (bestScore < ScoreThreshold) continue;

                            float cx = data[offset] * width;
                            float cy = data[offset + 1] * height;
                            float w = data[offset + 2] * width;
                            float h = data[offset + 3] * height;

                            int left = Math.Max(0, (int)(cx - w / 2));
                            int top = Math.Max(0, (int)(cy - h / 2));
                            int right = Math.Min(width - 1, (int)(cx + w / 2));
                            int bottom = Math.Min(height - 1, (int)(cy + h / 2));
                            if (right <= left || bottom <= top) continue;

                            boxes.Add(new Rect(left, top, right - left, bottom - top));
                            scores.Add(bestScore);
                            classIds.Add(bestClass);
                        }
                    }
                }
                finally
                {
                    foreach (Mat output in outputs) output.Dispose();
                }
            }

            int[] keep;
            CvDnn.NMSBoxes(boxes, scores, ScoreThreshold, NmsThreshold, out keep);

            var result = new List<Detection>(keep.Length);
            foreach (int i in keep)
                result.Add(new Detection { ClassId = classIds[i], Confidence = scores[i], Box = boxes[i] });

            // En büyük nesne başta olsun
            result.Sort((a, b) => (b.Box.Width * b.Box.Height).CompareTo(a.Box.Width * a.Box.Height));
            return result;
        }

        public void Dispose()
        {
            net.Dispose();
        }
    }
}
