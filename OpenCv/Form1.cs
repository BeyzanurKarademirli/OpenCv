using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

        private static readonly string HelpText =
            "S  Başlat          Q  Durdur\n" +
            "1-6  Filtre        M  Ayna\n" +
            "F  Yüz algılama    C  Kamera değiştir\n" +
            "P  Fotoğraf        R  Video kaydı\n" +
            "H  Yardımı gizle   Esc  Çıkış";

        private readonly PictureBox pictureBox;
        private readonly Label infoLabel;
        private readonly Label helpLabel;
        private readonly System.Windows.Forms.Timer uiTimer;
        private readonly FrameProcessor processor;

        // UI iş parçacığına ait durum
        private bool isRunning;
        private int cameraIndex;
        private CancellationTokenSource cancellation;
        private Task captureTask;
        private FrameInfo lastInfo;
        private string message;
        private DateTime messageExpires;

        // Kamera iş parçacığıyla paylaşılan ayarlar
        private volatile FilterMode filterMode = FilterMode.Normal;
        private volatile bool mirror = true;
        private volatile bool detectFaces;
        private volatile bool recordRequested;
        private int snapshotRequested;
        private int framePending;

        public Form1()
        {
            Text = "OpenCV Kamera";
            Size = new System.Drawing.Size(960, 640);
            MinimumSize = new System.Drawing.Size(480, 360);
            BackColor = Color.Black;
            KeyPreview = true;
            DoubleBuffered = true;

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom, // en-boy oranını koru
                BackColor = Color.Black
            };

            infoLabel = CreateOverlayLabel(new Font("Segoe UI", 12, FontStyle.Bold));
            infoLabel.Location = new System.Drawing.Point(15, 15);

            helpLabel = CreateOverlayLabel(new Font("Consolas", 10));
            helpLabel.Text = HelpText;

            Controls.Add(pictureBox);
            Controls.Add(infoLabel);
            Controls.Add(helpLabel);
            infoLabel.BringToFront();
            helpLabel.BringToFront();

            string cascadePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Data", "haarcascade_frontalface_default.xml");
            processor = new FrameProcessor(cascadePath);

            uiTimer = new System.Windows.Forms.Timer { Interval = 500 };
            uiTimer.Tick += (s, e) => UpdateStatus();
            uiTimer.Start();

            Resize += (s, e) => PositionHelpLabel();
            UpdateStatus();
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

        #region Klavye

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.S: StartCamera(); break;
                case Keys.Q: StopCamera(); break;
                case Keys.C: SwitchCamera(); break;
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
                    break;
            }

            UpdateStatus();
        }

        private static int GetFilterIndex(Keys key)
        {
            int index = -1;
            if (key >= Keys.D1 && key <= Keys.D9) index = key - Keys.D1;
            else if (key >= Keys.NumPad1 && key <= Keys.NumPad9) index = key - Keys.NumPad1;
            return index < Enum.GetValues(typeof(FilterMode)).Length ? index : -1;
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

                        int faceCount;
                        using (Mat output = processor.Process(frame, filterMode, detectFaces, out faceCount))
                        {
                            // Video kaydı
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
                                writer.Write(output);

                            // Fotoğraf
                            if (Interlocked.Exchange(ref snapshotRequested, 0) == 1)
                            {
                                string photoPath = CreateOutputPath(Environment.SpecialFolder.MyPictures, "foto", ".png");
                                bool saved = Cv2.ImWrite(photoPath, output);
                                PostToUi(() => ShowMessage(saved
                                    ? "Fotoğraf kaydedildi: " + photoPath
                                    : "Fotoğraf kaydedilemedi"));
                            }

                            // Ekranda göster (UI geride kalırsa bu kareyi atla)
                            if (Interlocked.CompareExchange(ref framePending, 1, 0) == 0)
                            {
                                Bitmap bitmap = BitmapConverter.ToBitmap(output);
                                var info = new FrameInfo
                                {
                                    CameraIndex = index,
                                    Width = output.Width,
                                    Height = output.Height,
                                    Fps = fps,
                                    FaceCount = faceCount,
                                    Recording = writer != null
                                };

                                if (!PostToUi(() => ShowFrame(bitmap, info)))
                                {
                                    bitmap.Dispose();
                                    Interlocked.Exchange(ref framePending, 0);
                                }
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
                CloseWriter(ref writer, videoPath);
                capture.Release();
                capture.Dispose();
            }
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

        private void ShowFrame(Bitmap bitmap, FrameInfo info)
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
            UpdateStatus();
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
                           " · Yüz: " + faces;

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
    }
}
