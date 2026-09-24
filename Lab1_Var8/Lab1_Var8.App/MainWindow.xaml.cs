using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Lab1_Var8.Core;
using WinForms = System.Windows.Forms;

namespace Lab1_Var8.App
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel = new MainWindowViewModel();

      
        private bool _updating = false;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel.PropertyChanged += (s, e) => RefreshDisplay();

            RefreshDisplay();
        }


        private void RefreshDisplay()
        {
            _updating = true;

            RSlider.Value = _viewModel.R; GSlider.Value = _viewModel.G; BSlider.Value = _viewModel.B;
            RBox.Text = _viewModel.R.ToString(); GBox.Text = _viewModel.G.ToString(); BBox.Text = _viewModel.B.ToString();

            XSlider.Value = Clamp(_viewModel.X, XSlider.Minimum, XSlider.Maximum);
            YSlider.Value = Clamp(_viewModel.Y, YSlider.Minimum, YSlider.Maximum);
            ZSlider.Value = Clamp(_viewModel.Z, ZSlider.Minimum, ZSlider.Maximum);
            XBox.Text = _viewModel.X.ToString("F0", CultureInfo.InvariantCulture);
            YBox.Text = _viewModel.Y.ToString("F0", CultureInfo.InvariantCulture);
            ZBox.Text = _viewModel.Z.ToString("F0", CultureInfo.InvariantCulture);

            HSlider.Value = _viewModel.H; SSlider.Value = _viewModel.S; VSlider.Value = _viewModel.V;
            HBox.Text = _viewModel.H.ToString("F0", CultureInfo.InvariantCulture);
            SBox.Text = _viewModel.S.ToString("F0", CultureInfo.InvariantCulture);
            VBox.Text = _viewModel.V.ToString("F0", CultureInfo.InvariantCulture);

            PreviewRect.Fill = new SolidColorBrush(Color.FromRgb(_viewModel.R, _viewModel.G, _viewModel.B));

            WarningText.Text = _viewModel.GamutWarning
                ? "Внимание: введённый цвет вне диапазона, соответствующего видимым RGB-цветам. Значения обрезаны до [0,255]."
                : "";

            _updating = false;
        }

        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);

        // ================== RGB ==================

        private void RgbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updating) return;
            _viewModel.ApplyRgb((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value);
        }

        private void RgbBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyRgbBoxes();
        }

        private void RgbBox_LostFocus(object sender, RoutedEventArgs e) => ApplyRgbBoxes();

        private void ApplyRgbBoxes()
        {
            if (_updating) return;
            byte r = ParseByteClamped(RBox.Text, _viewModel.R);
            byte g = ParseByteClamped(GBox.Text, _viewModel.G);
            byte b = ParseByteClamped(BBox.Text, _viewModel.B);
            _viewModel.ApplyRgb(r, g, b);
        }

        // ================== XYZ ==================

        private void XyzSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updating) return;
            _viewModel.ApplyXyz(XSlider.Value, YSlider.Value, ZSlider.Value);
        }

        private void XyzBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyXyzBoxes();
        }

        private void XyzBox_LostFocus(object sender, RoutedEventArgs e) => ApplyXyzBoxes();

        private void ApplyXyzBoxes()
        {
            if (_updating) return;
            double x = ParseDouble(XBox.Text, _viewModel.X);
            double y = ParseDouble(YBox.Text, _viewModel.Y);
            double z = ParseDouble(ZBox.Text, _viewModel.Z);
            _viewModel.ApplyXyz(x, y, z);
        }

        // ================== HSV ==================

        private void HsvSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updating) return;
            _viewModel.ApplyHsv(HSlider.Value, SSlider.Value, VSlider.Value);
        }

        private void HsvBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyHsvBoxes();
        }

        private void HsvBox_LostFocus(object sender, RoutedEventArgs e) => ApplyHsvBoxes();

        private void ApplyHsvBoxes()
        {
            if (_updating) return;
            double h = ParseDouble(HBox.Text, _viewModel.H);
            double s = ParseDouble(SBox.Text, _viewModel.S);
            double v = ParseDouble(VBox.Text, _viewModel.V);
            _viewModel.ApplyHsv(h, s, v);
        }

        // ================== Настройки ==================

        private void IlluminantCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updating) return;
            var item = IlluminantCombo.SelectedItem as ComboBoxItem;
            if (item == null) return;

            var illuminant = (string)item.Tag switch
            {
                "D65" => Illuminant.D65,
                "D50" => Illuminant.D50,
                "E" => Illuminant.E,
                _ => Illuminant.D65
            };
            _viewModel.ApplyIlluminant(illuminant);
        }

        // ================== Палитра ==================

        private void PaletteButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.ColorDialog { FullOpen = true })
            {
                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    var c = dialog.Color;
                    _viewModel.ApplyRgb(c.R, c.G, c.B);
                }
            }
        }

        // ================== Утилиты разбора ввода ==================

        private static byte ParseByteClamped(string text, byte fallback)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                return fallback;
            if (val < 0) val = 0;
            if (val > 255) val = 255;
            return (byte)Math.Round(val);
        }

        private static double ParseDouble(string text, double fallback)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val)
                ? val
                : fallback;
        }
    }
}
