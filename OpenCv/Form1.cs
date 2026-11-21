﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace OpenCv
{
    public partial class Form1 : Form
    {
        private const int MaxCameraIndex = 4;
        private const string AppFolderName = "OpenCV Kamera";
        private const int SidePanelWidth = 340;

        private static readonly string HelpText =
            "S  Başlat          Q  Durdur\n" +
            "O  Nesne tanıma    K  Mesafe kalibrasyonu\n" +
            "1-6  Filtre        M  Ayna\n" +
            "F  Yüz algılama    C  Kamera değiştir\n" +
            "P  Fotoğraf        R  Video kaydı\n" +
            "H  Yardımı gizle   Esc  Çıkış\n" +
            "Görüntüdeki nesneye tıklayarak seçin";

        private static readonly Color[] BoxColors =
        {
            Color.FromArgb(0, 200, 140), Color.FromArgb(70, 150, 255), Color.FromArgb(255, 170, 40),
            Color.FromArgb(230, 90, 160), Color.FromArgb(150, 110, 255), Color.FromArgb(240, 220, 60)
        };

        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

        private readonly PictureBox pictureBox;
        private readonly Label infoLabel;
        private readonly Label helpLabel;
        private readonly Panel sidePanel;
        private readonly ListBox objectList;
        private readonly TextBox detailsBox;
        private readonly System.Windows.Forms.Timer uiTimer;
        private readonly FrameProcessor processor;
        private readonly DistanceEstimator estimator;
        private readonly string modelDirectory;

        // UI iş parçacığına ait durum
        private bool isRunning;
        private int cameraIndex;
        private CancellationTokenSource cancellation;
        private Task captureTask;
        private FrameInfo lastInfo;
        private string message;
        private DateTime messageExpires;
        private TrackedObject[] shownObjects = new TrackedObject[0];
        private int shownVersion = -1;
        private TrackedObject selectedObject;
        private bool updatingList;
        private string wikiTitleShown;
        private string wikiText;

        // Kamera iş parçacığıyla paylaşılan ayarlar
        private volatile FilterMode filterMode = FilterMode.Normal;
        private volatile bool mirror = true;
        private volatile bool detectFaces;
        private volatile bool detectObjects;
        private volatile bool recordRequested;
        private volatile SelectionKey selectedKey;
        private int snapshotRequested;
        private int framePending;

        // Yalnızca kamera iş parçacığı kullanır (model ilk ihtiyaçta yüklenir)
        private ObjectDetector detector;

        public Form1()
        {
            Text = "OpenCV Kamera";
            Size = new System.Drawing.Size(1280, 720);
            MinimumSize = new System.Drawing.Size(800, 480);
            BackColor = Color.Black;
            KeyPreview = true;
            DoubleBuffered = true;

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom, // en-boy oranını koru
                BackColor = Color.Black,
                Cursor = Cursors.Hand
            };
            pictureBox.MouseClick += OnPictureClick;

            sidePanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = SidePanelWidth,
                BackColor = Color.FromArgb(24, 24, 30),
                Padding = new Padding(12),
                Visible = false
            };

            var listTitle = new Label
            {
                Text = "ALGILANAN NESNELER",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = Color.FromArgb(160, 160, 175),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            objectList = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 210,
                BackColor = Color.FromArgb(34, 34, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5f),
                IntegralHeight = false
            };
            objectList.SelectedIndexChanged += OnObjectListSelectionChanged;

            var detailsTitle = new Label
            {
                Text = "NESNE BİLGİSİ",
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(0, 10, 0, 0),
                ForeColor = Color.FromArgb(160, 160, 175),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            detailsBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                TabStop = false
            };

            // Dock sırası: sonra eklenen üstte görünür, önce eklenen önce yerleşir
            sidePanel.Controls.Add(detailsBox);
            sidePanel.Controls.Add(detailsTitle);
            sidePanel.Controls.Add(objectList);
            sidePanel.Controls.Add(listTitle);

            infoLabel = CreateOverlayLabel(new Font("Segoe UI", 12, FontStyle.Bold));
            infoLabel.Location = new System.Drawing.Point(15, 15);

            helpLabel = CreateOverlayLabel(new Font("Consolas", 10));
            helpLabel.Text = HelpText;

            Controls.Add(sidePanel);
            Controls.Add(pictureBox);
            Controls.Add(infoLabel);
            Controls.Add(helpLabel);
            pictureBox.BringToFront(); // kalan alanı doldursun
            infoLabel.BringToFront();
            helpLabel.BringToFront();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            processor = new FrameProcessor(Path.Combine(baseDir, "Data", "haarcascade_frontalface_default.xml"));
            modelDirectory = Path.Combine(baseDir, "Models");
            estimator = new DistanceEstimator(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName, "kalibrasyon.txt"));

            uiTimer = new System.Windows.Forms.Timer { Interval = 500 };
            uiTimer.Tick += (s, e) => UpdateStatus();
            uiTimer.Start();

            Resize += (s, e) => PositionHelpLabel();
            UpdateStatus();
            ShowDetails();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PositionHelpLabel();
        }

        private static Label CreateOverlayLabel(Font font)
        {
            return new Label
            {
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40),
                Font = font,
                AutoSize = true,
                Padding = new Padding(10, 8, 10, 8)
            };
        }

        private void PositionHelpLabel()
        {
            helpLabel.Location = new System.Drawing.Point(
                15, Math.Max(15, ClientSize.Height - helpLabel.Height - 15));
        }

        #region Klavye ve fare

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            bool handled = true;

            switch (e.KeyCode)
            {
                case Keys.S: StartCamera(); break;
                case Keys.Q: StopCamera(); break;
                case Keys.C: SwitchCamera(); break;
                case Keys.O: ToggleObjectDetection(); break;
                case Keys.K: CalibrateDistance(); break;
                case Keys.M:
                    mirror = !mirror;
                    ShowMessage("Ayna " + (mirror ? "açık" : "kapalı"));
                    break;
                case Keys.F: ToggleFaceDetection(); break;
                case Keys.P: TakeSnapshot(); break;
                case Keys.R: ToggleRecording(); break;
                case Keys.H: helpLabel.Visible = !helpLabel.Visible; break;
                case Keys.Escape: Close(); return;
                default:
                    int filter = GetFilterIndex(e.KeyCode);
                    if (filter >= 0)
                    {
                        filterMode = (FilterMode)filter;
                        ShowMessage("Filtre: " + FrameProcessor.GetFilterName(filterMode));
                    }
                    else
                    {
                        handled = false;
                    }
                    break;
            }

            // Liste kutusunun harfle arama yapmasını engelle
            if (handled) e.SuppressKeyPress = true;
            UpdateStatus();
        }

        private static int GetFilterIndex(Keys key)
        {
            int index = -1;
            if (key >= Keys.D1 && key <= Keys.D9) index = key - Keys.D1;
            else if (key >= Keys.NumPad1 && key <= Keys.NumPad9) index = key - Keys.NumPad1;
            return index < Enum.GetValues(typeof(FilterMode)).Length ? index : -1;
        }

        private void OnPictureClick(object sender, MouseEventArgs e)
        {
            if (lastInfo == null || shownObjects.Length == 0) return;

            // PictureBox "Zoom" modunda görüntünün ekrandaki yerini hesapla
            double scale = Math.Min((double)pictureBox.Width / lastInfo.Width,
                (double)pictureBox.Height / lastInfo.Height);
            double offsetX = (pictureBox.Width - lastInfo.Width * scale) / 2;
            double offsetY = (pictureBox.Height - lastInfo.Height * scale) / 2;
            int x = (int)((e.X - offsetX) / scale);
            int y = (int)((e.Y - offsetY) / scale);

            // Tıklanan noktayı içeren en küçük nesneyi seç
            int best = -1;
            int bestArea = int.MaxValue;
            for (int i = 0; i < shownObjects.Length; i++)
            {
                Rect box = shownObjects[i].Detection.Box;
                int area = box.Width * box.Height;
                if (box.Contains(x, y) && area < bestArea)
                {
                    best = i;
                    bestArea = area;
                }
            }

            if (best >= 0) objectList.SelectedIndex = best;
        }

        #endregion

        #region Komutlar

        private void StartCamera()
        {
            if (isRunning) return;

            isRunning = true;
            lastInfo = null;
            cancellation = new CancellationTokenSource();
            CancellationToken token = cancellation.Token;
            int index = cameraIndex;
            captureTask = Task.Run(() => CaptureLoop(index, token));
        }

        private void StopCamera()
        {
            if (!isRunning) return;

            isRunning = false;
            recordRequested = false;
            cancellation.Cancel();

            // Kamera iş parçacığı UI'yı yalnızca BeginInvoke ile güncellediği için
            // burada beklemek kilitlenmeye yol açmaz.
            try { captureTask.Wait(TimeSpan.FromSeconds(3)); }
            catch (AggregateException) { }

            cancellation = null;
            captureTask = null;
            lastInfo = null;
            ShowObjects(new TrackedObject[0], -1);

            Image old = pictureBox.Image;
            pictureBox.Image = null;
            if (old != null) old.Dispose();
        }

        private void SwitchCamera()
        {
            bool wasRunning = isRunning;
            StopCamera();
            cameraIndex = (cameraIndex + 1) % MaxCameraIndex;
            ShowMessage("Kamera " + cameraIndex + " seçildi");
            if (wasRunning) StartCamera();
        }

        private void ToggleObjectDetection()
        {
            detectObjects = !detectObjects;
            sidePanel.Visible = detectObjects;
            if (!detectObjects) ShowObjects(new TrackedObject[0], -1);
            ShowMessage("Nesne tanıma " + (detectObjects ? "açık" : "kapalı"));
            PositionHelpLabel();
        }

        private void ToggleFaceDetection()
        {
            if (!processor.FaceDetectionAvailable)
            {
                ShowMessage("Yüz algılama dosyası bulunamadı (Data klasörü)");
                return;
            }

            detectFaces = !detectFaces;
            ShowMessage("Yüz algılama " + (detectFaces ? "açık" : "kapalı"));
        }

        private void CalibrateDistance()
        {
            if (selectedObject == null)
            {
                ShowMessage("Kalibrasyon için önce bir nesne seçin (O ile nesne tanımayı açın)");
                return;
            }

            TrackedObject target = selectedObject;
            string answer = Microsoft.VisualBasic.Interaction.InputBox(
                "Seçili nesnenin (" + target.Info.Name + ") kameraya gerçek uzaklığı kaç metre?\n\n" +
                "Örnek: 1,5\nVarsayılana dönmek için 0 yazın.",
                "Mesafe kalibrasyonu", "1");
            if (string.IsNullOrWhiteSpace(answer)) return;

            double meters;
            string normalized = answer.Trim().Replace(',', '.');
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out meters) ||
                meters < 0 || meters > 100)
            {
                ShowMessage("Geçersiz değer: " + answer);
                return;
            }

            if (meters == 0)
            {
                estimator.Reset();
                ShowMessage("Kalibrasyon sıfırlandı");
            }
            else
            {
                estimator.Calibrate(target, meters);
                ShowMessage("Kalibre edildi: " + target.Info.Name + " = " +
                            meters.ToString("0.##", Turkish) + " m");
            }
        }

        private void TakeSnapshot()
        {
            if (!isRunning)
            {
                ShowMessage("Fotoğraf için önce kamerayı başlatın (S)");
                return;
            }

            Interlocked.Exchange(ref snapshotRequested, 1);
        }

        private void ToggleRecording()
        {
            if (!isRunning)
            {
                ShowMessage("Kayıt için önce kamerayı başlatın (S)");
                return;
            }

            recordRequested = !recordRequested;
        }

        #endregion

        #region Kamera iş parçacığı

        private void CaptureLoop(int index, CancellationToken token)
        {
            VideoCapture capture = OpenCamera(index);
            if (capture == null)
            {
                PostToUi(() => OnCameraFailed("Kamera " + index + " açılamadı"));
                return;
            }

            VideoWriter writer = null;
            string videoPath = null;
            var frameTimer = Stopwatch.StartNew();
            double fps = 0;
            int emptyFrames = 0;

            Task<List<Detection>> detectionTask = null;
            TrackedObject[] objects = new TrackedObject[0];
            int objectsVersion = 0;

            try
            {
                using (var frame = new Mat())
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (!capture.Read(frame) || frame.Empty())
                        {
                            if (++emptyFrames > 100)
                            {
                                PostToUi(() => OnCameraFailed("Kamera bağlantısı kesildi"));
                                break;
                            }
                            Thread.Sleep(20);
                            continue;
                        }
                        emptyFrames = 0;

                        double elapsed = frameTimer.Elapsed.TotalSeconds;
                        frameTimer.Restart();
                        if (elapsed > 0)
                            fps = fps <= 0 ? 1.0 / elapsed : fps * 0.9 + (1.0 / elapsed) * 0.1;

                        if (mirror)
                            Cv2.Flip(frame, frame, FlipMode.Y);

                        // Nesne tanıma arka planda çalışır; görüntü akışı onu beklemez.
                        if (detectObjects)
                        {
                            if (detectionTask != null && detectionTask.IsCompleted)
                            {
                                if (detectionTask.Status == TaskStatus.RanToCompletion)
                                {
                                    objects = Describe(detectionTask.Result, frame.Width, frame.Height);
                                    objectsVersion++;
                                }
                                else if (detectionTask.Exception != null)
                                {
                                    string error = detectionTask.Exception.GetBaseException().Message;
                                    PostToUi(() => ShowMessage("Nesne tanıma hatası: " + error));
                                }
                                detectionTask = null;
                            }

                            if (detectionTask == null && EnsureDetector())
                            {
                                Mat copy = frame.Clone();
                                ObjectDetector activeDetector = detector;
                                detectionTask = Task.Run(() =>
                                {
                                    using (copy) return activeDetector.Detect(copy);
                                });
                            }
                        }
                        else if (objects.Length > 0)
                        {
                            objects = new TrackedObject[0];
                            objectsVersion++;
                        }

                        int faceCount;
                        using (Mat output = processor.Process(frame, filterMode, detectFaces, out faceCount))
                        {
                            Bitmap bitmap = BitmapConverter.ToBitmap(output);
                            DrawObjects(bitmap, objects, FindMatch(objects, selectedKey));

                            // Video kaydı (etiketler dahil)
                            if (recordRequested && writer == null)
                            {
                                videoPath = CreateOutputPath(Environment.SpecialFolder.MyVideos, "video", ".avi");
                                writer = new VideoWriter(videoPath, FourCC.MJPG, fps > 1 ? fps : 20, output.Size());
                                if (writer.IsOpened())
                                {
                                    PostToUi(() => ShowMessage("● Kayıt başladı"));
                                }
                                else
                                {
                                    writer.Dispose();
                                    writer = null;
                                    recordRequested = false;
                                    PostToUi(() => ShowMessage("Video kaydı başlatılamadı"));
                                }
                            }
                            else if (!recordRequested && writer != null)
                            {
                                CloseWriter(ref writer, videoPath);
                            }

                            if (writer != null)
                            {
                                using (Mat labeled = BitmapConverter.ToMat(bitmap))
                                    writer.Write(labeled);
                            }

                            // Fotoğraf (etiketler dahil)
                            if (Interlocked.Exchange(ref snapshotRequested, 0) == 1)
                            {
                                string photoPath = CreateOutputPath(Environment.SpecialFolder.MyPictures, "foto", ".png");
                                string result;
                                try
                                {
                                    bitmap.Save(photoPath, ImageFormat.Png);
                                    result = "Fotoğraf kaydedildi: " + photoPath;
                                }
                                catch (Exception ex)
                                {
                                    result = "Fotoğraf kaydedilemedi: " + ex.Message;
                                }
                                PostToUi(() => ShowMessage(result));
                            }

                            // Ekranda göster (UI geride kalırsa bu kareyi atla)
                            if (Interlocked.CompareExchange(ref framePending, 1, 0) == 0)
                            {
                                var info = new FrameInfo
                                {
                                    CameraIndex = index,
                                    Width = output.Width,
                                    Height = output.Height,
                                    Fps = fps,
                                    FaceCount = faceCount,
                                    Recording = writer != null
                                };
                                TrackedObject[] snapshot = objects;
                                int version = objectsVersion;

                                if (!PostToUi(() => ShowFrame(bitmap, info, snapshot, version)))
                                {
                                    bitmap.Dispose();
                                    Interlocked.Exchange(ref framePending, 0);
                                }
                            }
                            else
                            {
                                bitmap.Dispose();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Kamera hatası: " + ex);
                PostToUi(() => OnCameraFailed("Hata: " + ex.Message));
            }
            finally
            {
                if (detectionTask != null)
                {
                    try { detectionTask.Wait(TimeSpan.FromSeconds(2)); }
                    catch (AggregateException) { }
                }

                CloseWriter(ref writer, videoPath);
                capture.Release();
                capture.Dispose();
            }
        }

        private bool EnsureDetector()
        {
            if (detector != null) return true;

            try
            {
                detector = ObjectDetector.TryLoad(modelDirectory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Model yüklenemedi: " + ex);
            }

            if (detector == null)
            {
                detectObjects = false;
                PostToUi(() =>
                {
                    sidePanel.Visible = false;
                    ShowMessage("Nesne tanıma modeli bulunamadı (Models klasörü)");
                });
                return false;
            }

            return true;
        }

        private TrackedObject[] Describe(List<Detection> detections, int width, int height)
        {
            var result = new TrackedObject[detections.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = estimator.Describe(detections[i], width, height);
            return result;
        }

        /// <summary>Kutuları ve Türkçe etiketleri görüntünün üzerine çizer.</summary>
        private static void DrawObjects(Bitmap bitmap, TrackedObject[] objects, int selectedIndex)
        {
            if (objects.Length == 0) return;

            using (Graphics g = Graphics.FromImage(bitmap))
            using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                for (int i = 0; i < objects.Length; i++)
                {
                    TrackedObject obj = objects[i];
                    Rect box = obj.Detection.Box;
                    Color color = BoxColors[obj.Detection.ClassId % BoxColors.Length];
                    bool selected = i == selectedIndex;

                    using (var pen = new Pen(color, selected ? 5 : 2.5f))
                        g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);

                    string label = obj.Info.Name + "  " + obj.DistanceText;
                    SizeF size = g.MeasureString(label, font);
                    float labelY = box.Y - size.Height - 2 >= 0 ? box.Y - size.Height - 2 : box.Y + 2;

                    using (var background = new SolidBrush(color))
                        g.FillRectangle(background, box.X, labelY, size.Width + 6, size.Height);
                    g.DrawString(label, font, textBrush, box.X + 3, labelY);
                }
            }
        }

        private static int FindMatch(TrackedObject[] objects, SelectionKey key)
        {
            if (key == null) return -1;

            int best = -1;
            double bestDistance = 0.25; // görüntü boyutuna göre en fazla bu kadar kaymış olabilir
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].Detection.ClassId != key.ClassId) continue;
                double dx = objects[i].CenterX - key.CenterX;
                double dy = objects[i].CenterY - key.CenterY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static VideoCapture OpenCamera(int index)
        {
            // DirectShow Windows'ta genellikle daha hızlı açılır; olmazsa varsayılana dön.
            foreach (VideoCaptureAPIs api in new[] { VideoCaptureAPIs.DSHOW, VideoCaptureAPIs.ANY })
            {
                var capture = new VideoCapture(index, api);
                if (capture.IsOpened()) return capture;
                capture.Dispose();
            }
            return null;
        }

        private void CloseWriter(ref VideoWriter writer, string path)
        {
            if (writer == null) return;

            writer.Release();
            writer.Dispose();
            writer = null;
            PostToUi(() => ShowMessage("Video kaydedildi: " + path));
        }

        private static string CreateOutputPath(Environment.SpecialFolder folder, string prefix, string extension)
        {
            string directory = Path.Combine(Environment.GetFolderPath(folder), AppFolderName);
            Directory.CreateDirectory(directory);
            string name = prefix + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + extension;
            return Path.Combine(directory, name);
        }

        private bool PostToUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return false;
            try
            {
                BeginInvoke(action);
                return true;
            }
            catch (InvalidOperationException)
            {
                // Form kapanırken handle yok edilmiş olabilir.
                return false;
            }
        }

        #endregion

        #region UI güncellemeleri

        private void ShowFrame(Bitmap bitmap, FrameInfo info, TrackedObject[] objects, int version)
        {
            Interlocked.Exchange(ref framePending, 0);

            if (!isRunning)
            {
                bitmap.Dispose();
                return;
            }

            Image old = pictureBox.Image;
            pictureBox.Image = bitmap;
            if (old != null) old.Dispose();

            lastInfo = info;
            if (version != shownVersion) ShowObjects(objects, version);
            UpdateStatus();
        }

        private void ShowObjects(TrackedObject[] objects, int version)
        {
            shownObjects = objects;
            shownVersion = version;

            // Seçili nesneyi yeni sonuçlar arasında bul (aynı sınıf, en yakın konum)
            int selectedIndex = FindMatch(objects, selectedKey);
            if (selectedIndex < 0 && selectedKey == null && objects.Length > 0)
                selectedIndex = 0; // hiçbir şey seçili değilse en büyük nesneyi seç

            updatingList = true;
            try
            {
                objectList.BeginUpdate();
                objectList.Items.Clear();
                foreach (TrackedObject obj in objects)
                {
                    objectList.Items.Add(string.Format(Turkish, "{0}   {1}   %{2:0}",
                        obj.Info.Name, obj.DistanceText, obj.Detection.Confidence * 100));
                }
                objectList.SelectedIndex = selectedIndex;
                objectList.EndUpdate();
            }
            finally
            {
                updatingList = false;
            }

            if (selectedIndex >= 0)
                Select(objects[selectedIndex]);
            else if (objects.Length == 0)
                Select(null);
            else
                selectedObject = null; // seçili nesne şu an görünmüyor; anahtarı koru

            ShowDetails();
            if (selectedObject == null && selectedKey != null && objects.Length > 0)
                detailsBox.Text = "Seçili nesne şu an görünmüyor. Listeden başka bir nesne seçebilirsiniz.";
        }

        private void OnObjectListSelectionChanged(object sender, EventArgs e)
        {
            if (updatingList) return;

            int index = objectList.SelectedIndex;
            Select(index >= 0 && index < shownObjects.Length ? shownObjects[index] : null);
            ShowDetails();
        }

        private void Select(TrackedObject obj)
        {
            selectedObject = obj;
            selectedKey = obj == null
                ? null
                : new SelectionKey(obj.Detection.ClassId, obj.CenterX, obj.CenterY);

            if (obj != null && obj.Info.WikiTitle != wikiTitleShown)
                LoadWikipedia(obj.Info.WikiTitle);
        }

        private async void LoadWikipedia(string title)
        {
            wikiTitleShown = title;
            wikiText = "Yükleniyor...";

            string text;
            try
            {
                string summary = await WikipediaClient.GetSummaryAsync(title);
                text = string.IsNullOrEmpty(summary) ? "Bu nesne için Vikipedi özeti bulunamadı." : summary;
            }
            catch (Exception)
            {
                text = "Vikipedi'ye ulaşılamadı (internet bağlantısı gerekir).";
            }

            // Bu arada başka bir nesne seçildiyse eski sonucu gösterme
            if (title != wikiTitleShown) return;
            wikiText = text;
            ShowDetails();
        }

        private void ShowDetails()
        {
            TrackedObject obj = selectedObject;
            if (obj == null)
            {
                detailsBox.Text = !detectObjects
                    ? "Nesne tanımayı açmak için O tuşuna basın."
                    : shownObjects.Length == 0
                        ? "Henüz nesne algılanmadı. Kameraya bir nesne gösterin."
                        : "Bilgi görmek için listeden ya da görüntüden bir nesne seçin.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(obj.Info.Name.ToUpper(Turkish));
            sb.AppendLine();
            sb.AppendLine("Kategori: " + obj.Info.Category);
            sb.AppendLine(string.Format(Turkish, "Güven: %{0:0}", obj.Detection.Confidence * 100));
            sb.AppendLine("Tahmini mesafe: " + obj.DistanceText + " (" + obj.Basis + ")");
            if (obj.TooClose)
                sb.AppendLine("  Nesne kadraja sığmıyor; gerçek mesafe bundan da az.");
            sb.AppendLine(string.Format(Turkish, "Tahmini boyut: {0:0.00} m (G) × {1:0.00} m (Y)",
                obj.WidthM, obj.HeightM));
            sb.AppendLine("Konum: " + obj.Position);
            sb.AppendLine();
            sb.AppendLine(obj.Info.Description);
            sb.AppendLine();
            sb.AppendLine("Vikipedi:");
            sb.AppendLine(wikiText ?? "");
            sb.AppendLine();
            sb.Append(estimator.IsCalibrated
                ? "Mesafe kalibre edilmiş kamera ayarıyla hesaplandı."
                : "Mesafe yaklaşıktır. Daha doğru sonuç için nesneyi bilinen bir uzaklığa koyup K tuşuyla kalibre edin.");

            detailsBox.Text = sb.ToString();
        }

        private void OnCameraFailed(string text)
        {
            StopCamera();
            ShowMessage(text);
        }

        private void ShowMessage(string text)
        {
            message = text;
            messageExpires = DateTime.Now.AddSeconds(4);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            string line1;
            if (!isRunning)
                line1 = "⏹ Kamera " + cameraIndex + " durdu (S = Başlat)";
            else if (lastInfo == null)
                line1 = "… Kamera " + cameraIndex + " açılıyor";
            else
                line1 = string.Format("▶ Kamera {0} · {1}×{2} · {3:0.0} FPS",
                    lastInfo.CameraIndex, lastInfo.Width, lastInfo.Height, lastInfo.Fps);

            string faces = !detectFaces ? "Kapalı" : lastInfo != null ? lastInfo.FaceCount.ToString() : "—";
            string line2 = "Filtre: " + FrameProcessor.GetFilterName(filterMode) +
                           " · Ayna: " + (mirror ? "Açık" : "Kapalı") +
                           " · Yüz: " + faces +
                           " · Nesne: " + (detectObjects ? shownObjects.Length.ToString() : "Kapalı");

            bool recording = lastInfo != null && lastInfo.Recording;
            if (recording)
                line2 += " · ● KAYIT";

            string text = line1 + "\n" + line2;
            if (message != null && DateTime.Now < messageExpires)
                text += "\n" + message;

            infoLabel.Text = text;
            infoLabel.ForeColor = recording ? Color.FromArgb(255, 110, 110) : Color.White;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            uiTimer.Stop();
            StopCamera();
            processor.Dispose();
            if (detector != null) detector.Dispose();
            base.OnFormClosing(e);
        }

        #endregion

        private sealed class FrameInfo
        {
            public int CameraIndex;
            public int Width;
            public int Height;
            public double Fps;
            public int FaceCount;
            public bool Recording;
        }

        /// <summary>Seçili nesneyi kareler arasında takip etmek için sınıf + konum.</summary>
        private sealed class SelectionKey
        {
            public SelectionKey(int classId, double centerX, double centerY)
            {
                ClassId = classId;
                CenterX = centerX;
                CenterY = centerY;
            }

            public readonly int ClassId;
            public readonly double CenterX;
            public readonly double CenterY;
        }
    }
}
