using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Velleman8090;

namespace WebcamPhotosensor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //this.Loaded += MainWindow_Loaded;
            this._relay = V8090.Instance;
            this._relay.TranmissionFailed += TransmissionFailed;
        }

        private void TransmissionFailed()
        {
            this.failedCount.Text = (int.Parse(this.failedCount.Text) + 1).ToString();
        }

        private VideoCaptureDevice _vs;

        private V8090 _relay;

        bool _isOn = false;

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // enumerate video devices
            var videoDevices = new FilterInfoCollection(
                    FilterCategory.VideoInputDevice);
            // create video source
            VideoCaptureDevice videoSource = new VideoCaptureDevice(
                    videoDevices[0].MonikerString);
            // set NewFrame event handler
            videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);

            this._vs = videoSource;
            // start the video source
            videoSource.Start();
            this.Dispatcher.Invoke(() => onetime.IsEnabled = false);
            
        }

        private async void video_NewFrame(object sender,
        NewFrameEventArgs eventArgs)
        {
            this._vs.SignalToStop();
            // get new frame
            Bitmap bitmap = eventArgs.Frame;
            //bitmap.Save("test.jpg", ImageFormat.Jpeg);

            if (CalculateAverageLightness(bitmap) < (upDown.Value * 0.01) && !this._isOn)
            {
                this._relay.On(6);
                this._isOn = true;
            }
            else if (CalculateAverageLightness(bitmap) > (upDown.Value * 0.01) && this._isOn)
            {
                this._relay.Off(6);
                this._isOn = false;
            }

            await Task.Delay(2000);
            MainWindow_Loaded(this, null);
            // process the frame
        }

        public static double CalculateAverageLightness(Bitmap bm)
        {
            double lum = 0;
            var tmpBmp = new Bitmap(bm);
            var width = bm.Width;
            var height = bm.Height;
            var bppModifier = bm.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? 3 : 4;

            var srcData = tmpBmp.LockBits(new System.Drawing.Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
            var stride = srcData.Stride;
            var scan0 = srcData.Scan0;

            //Luminance (standard, objective): (0.2126*R) + (0.7152*G) + (0.0722*B)
            //Luminance (perceived option 1): (0.299*R + 0.587*G + 0.114*B)
            //Luminance (perceived option 2, slower to calculate): sqrt( 0.241*R^2 + 0.691*G^2 + 0.068*B^2 )

            unsafe
            {
                byte* p = (byte*)(void*)scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = (y * stride) + x * bppModifier;
                        lum += (0.299 * p[idx + 2] + 0.587 * p[idx + 1] + 0.114 * p[idx]);
                    }
                }
            }

            tmpBmp.UnlockBits(srcData);
            tmpBmp.Dispose();
            var avgLum = lum / (width * height);


            return avgLum / 255.0;
        }
    }
}
