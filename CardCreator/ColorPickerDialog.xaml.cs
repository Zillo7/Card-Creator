using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace CardCreator
{
    public partial class ColorPickerDialog : Window, INotifyPropertyChanged
    {
        private double _red;
        private double _green;
        private double _blue;
        private double _alpha;
        private Brush _previewBrush = Brushes.Transparent;

        public ColorPickerDialog()
        {
            InitializeComponent();
            DataContext = this;
            UpdatePreview();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public double Red
        {
            get => _red;
            set
            {
                if (SetField(ref _red, Clamp(value)))
                    UpdatePreview();
            }
        }

        public double Green
        {
            get => _green;
            set
            {
                if (SetField(ref _green, Clamp(value)))
                    UpdatePreview();
            }
        }

        public double Blue
        {
            get => _blue;
            set
            {
                if (SetField(ref _blue, Clamp(value)))
                    UpdatePreview();
            }
        }

        public double Alpha
        {
            get => _alpha;
            set
            {
                if (SetField(ref _alpha, Clamp(value)))
                    UpdatePreview();
            }
        }

        public Brush PreviewBrush
        {
            get => _previewBrush;
            private set => SetField(ref _previewBrush, value);
        }

        public Color SelectedColor
        {
            get => Color.FromArgb((byte)Math.Round(Alpha), (byte)Math.Round(Red), (byte)Math.Round(Green), (byte)Math.Round(Blue));
            set
            {
                Alpha = value.A;
                Red = value.R;
                Green = value.G;
                Blue = value.B;
            }
        }

        private static double Clamp(double value) => Math.Min(255, Math.Max(0, value));

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        private void UpdatePreview()
        {
            var color = SelectedColor;
            PreviewBrush = new SolidColorBrush(color);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
