using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace OpenCv
{
    public partial class Form1 : Form
    {
        private VideoCapture capture;
        private Mat frame;
        private bool isRunning = false;
        private PictureBox pictureBox1;
        private Label infoLabel;

        public Form1()
        {
            this.Text = "OpenCV Kamera Akışı";
            this.Size = new System.Drawing.Size(800, 600);

            pictureBox1 = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            this.Controls.Add(pictureBox1);

            infoLabel = new Label
            {
                Text = "Kamera durdu (S=Başlat, Q=Durdur)",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(180, 0, 0, 0),
                Font = new Font("Arial", 14, FontStyle.Bold),
                AutoSize = false,
                Width = 400,
                Height = 50,
                Top = 15,
                Left = 15,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10)
            };
            this.Controls.Add(infoLabel);
            infoLabel.BringToFront();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.S && !isRunning) StartCamera();
            if (e.KeyCode == Keys.Q && isRunning) StopCamera();
        }

        private void StartCamera()
        {
            capture = new VideoCapture(0);
            frame = new Mat();
            isRunning = true;
            infoLabel.Text = "▶ Kamera çalışıyor... (Q=Durdur)";

            Task.Run(() => ProcessCamera());
        }

        private void StopCamera()
        {
            isRunning = false;
            if (capture != null)
            {
                capture.Release();
                capture.Dispose();
            }
            infoLabel.Text = "⏹ Kamera durdu (S=Başlat)";
        }

        private void ProcessCamera()
        {
            while (isRunning)
            {
                try
                {
                    capture.Read(frame);
                    if (frame.Empty()) continue;

                    Invoke(new Action(() =>
                    {
                        if (pictureBox1.Image != null)
                            pictureBox1.Image.Dispose();
                        pictureBox1.Image = BitmapConverter.ToBitmap(frame);
                        infoLabel.BringToFront();
                    }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Hata: " + ex.Message);
                }
            }
        }
    }
}