using System.ComponentModel;
using Lab1_Var8.Core;

namespace Lab1_Var8.App
{
 
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ColorModel _model;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            _model = new ColorModel();

            _model.ColorChanged += OnModelColorChanged;
        }

        public byte R => _model.R;
        public byte G => _model.G;
        public byte B => _model.B;

        public double X => _model.X;
        public double Y => _model.Y;
        public double Z => _model.Z;

        public double H => _model.H;
        public double S => _model.S;
        public double V => _model.V;

        public bool GamutWarning => _model.LastGamutWarning;
        public Illuminant Illuminant => _model.Illuminant;

        public void ApplyRgb(byte r, byte g, byte b) => _model.SetFromRgb(r, g, b);
        public void ApplyXyz(double x, double y, double z) => _model.SetFromXyz(x, y, z);
        public void ApplyHsv(double h, double s, double v) => _model.SetFromHsv(h, s, v);
        public void ApplyIlluminant(Illuminant illuminant) => _model.SetIlluminant(illuminant);

        private void OnModelColorChanged(object sender, ColorChangedEventArgs e)
        {
            RaiseAll();
        }

        private void RaiseAll()
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(nameof(R)));
            handler(this, new PropertyChangedEventArgs(nameof(G)));
            handler(this, new PropertyChangedEventArgs(nameof(B)));
            handler(this, new PropertyChangedEventArgs(nameof(X)));
            handler(this, new PropertyChangedEventArgs(nameof(Y)));
            handler(this, new PropertyChangedEventArgs(nameof(Z)));
            handler(this, new PropertyChangedEventArgs(nameof(H)));
            handler(this, new PropertyChangedEventArgs(nameof(S)));
            handler(this, new PropertyChangedEventArgs(nameof(V)));
            handler(this, new PropertyChangedEventArgs(nameof(GamutWarning)));
        }
    }
}
