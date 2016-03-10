using Jeffreye.Alipay.BarcodePayment.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.System.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using ZXing;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AlipayBarcodePayment
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        OtpManager otp;
        string payChannel = null;

        DisplayRequest display;

        public MainPage()
        {
            this.InitializeComponent();

            display = new DisplayRequest();

            otp = new OtpManager(Configurations.Tid, Configurations.Index, Configurations.UserId);
            //decrypt seed
            OtpShareStore.putString(Application.Current, Configurations.Tid, Configurations.EncryptedSeed, OtpShareStore.SETTING_INFOS_NEW);
            OtpShareStore.putString(Application.Current, "interval", Configurations.Interval, OtpShareStore.SETTING_INFOS_NEW);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            //bug:这里设置会出错，移到SplashScreen.Dismissed可能就可以
            //ApplicationView.GetForCurrentView().IsScreenCaptureEnabled = false;
            Application.Current.Resuming += Application_Resuming;

            display.RequestActive();

            UpdateEveryMinutes();

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            display.RequestRelease();
            Application.Current.Resuming += Application_Resuming;
        }

        private void Application_Resuming(object sender, object e)
        {
            Refresh();
        }

        private async void UpdateEveryMinutes()
        {
            do
            {
                Refresh();
                await Task.Delay(1000*60);
            } while (true);
        }

        private async void Refresh()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                var content = otp.getDynamicOtp(payChannel);
                code.Text = content.Insert(4, "  ").Insert(10, "  ").Insert(16, "  ");
                GenerateImage(content, barcode, BarcodeFormat.CODE_128);
                GenerateImage(content, qrcode, BarcodeFormat.QR_CODE);
            });
        }

        private void GenerateImage(string content, Image image, BarcodeFormat format)
        {
            var width = (int)image.Width;
            var height = (int)image.Height;

            BarcodeWriter writer = new BarcodeWriter();
            writer.Format = format;

            writer.Renderer = new ZXing.Rendering.PixelDataRenderer()
            {
                Background = new Windows.UI.Color() { R = 246, G = 246, B = 248 }
            };

            writer.Options = new ZXing.Common.EncodingOptions()
            {
                Height = height,
                Width = width,
                Margin = 0,
                PureBarcode = true
            };
            writer.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.H);

            var pixels = writer.Write(content);
            image.Source = pixels.ToBitmap() as WriteableBitmap;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
