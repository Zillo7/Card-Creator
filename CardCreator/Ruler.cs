using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CardCreator
{
    public class Ruler : FrameworkElement
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(Ruler),
            new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitsProperty = DependencyProperty.Register(
            nameof(Units), typeof(MeasurementUnit), typeof(Ruler),
            new FrameworkPropertyMetadata(MeasurementUnit.Inches, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MarkerProperty = DependencyProperty.Register(
            nameof(Marker), typeof(double), typeof(Ruler),
            new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OriginProperty = DependencyProperty.Register(
            nameof(Origin), typeof(double), typeof(Ruler),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public MeasurementUnit Units
        {
            get => (MeasurementUnit)GetValue(UnitsProperty);
            set => SetValue(UnitsProperty, value);
        }

        public double Marker
        {
            get => (double)GetValue(MarkerProperty);
            set => SetValue(MarkerProperty, value);
        }

        public double Origin
        {
            get => (double)GetValue(OriginProperty);
            set => SetValue(OriginProperty, value);
        }

        private double UnitsToDiu()
        {
            return Units switch
            {
                MeasurementUnit.Inches => 96.0,
                MeasurementUnit.Millimeters => 96.0 / 25.4,
                _ => 1.0,
            };
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double scale = UnitsToDiu();
            double length = Orientation == Orientation.Horizontal ? ActualWidth : ActualHeight;
            var pen = new Pen(Brushes.Gray, 1);
            var textBrush = Brushes.Black;
            var typeface = new Typeface("Segoe UI");
            double tickLengthMajor = 10;
            double tickLengthMinor = 5;

            double step = Units == MeasurementUnit.DeviceIndependent ? 10 : 1;
            double start = Math.Floor((-Origin) / scale / step) * step;
            double end = Math.Ceiling((length - Origin) / scale / step) * step;

            for (double u = start; u <= end; u += step)
            {
                double pos = Origin + u * scale + 0.5; // crisp lines
                if (pos < 0 || pos > length) continue;

                Point p1, p2;
                if (Orientation == Orientation.Horizontal)
                {
                    p1 = new Point(pos, ActualHeight);
                    p2 = new Point(pos, ActualHeight - tickLengthMajor);
                }
                else
                {
                    p1 = new Point(ActualWidth, pos);
                    p2 = new Point(ActualWidth - tickLengthMajor, pos);
                }
                dc.DrawLine(pen, p1, p2);

                if (Units == MeasurementUnit.Inches)
                {
                    var ft = new FormattedText(u.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, typeface, 8, textBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    if (Orientation == Orientation.Horizontal)
                        dc.DrawText(ft, new Point(pos + 2, 0));
                    else
                        dc.DrawText(ft, new Point(0, pos + 2));
                }

                // minor ticks for inches
                if (Units == MeasurementUnit.Inches && scale >= 10)
                {
                    for (int m = 1; m < 10; m++)
                    {
                        double mpos = pos + m * scale / 10;
                        if (mpos < 0 || mpos > length) continue;
                        Point mp1, mp2;
                        if (Orientation == Orientation.Horizontal)
                        {
                            mp1 = new Point(mpos, ActualHeight);
                            mp2 = new Point(mpos, ActualHeight - tickLengthMinor);
                        }
                        else
                        {
                            mp1 = new Point(ActualWidth, mpos);
                            mp2 = new Point(ActualWidth - tickLengthMinor, mpos);
                        }
                        dc.DrawLine(pen, mp1, mp2);
                    }
                }
            }

            if (!double.IsNaN(Marker))
            {
                var markerPen = new Pen(Brushes.Red, 1);
                if (Orientation == Orientation.Horizontal)
                {
                    double x = Origin + Marker + 0.5;
                    dc.DrawLine(markerPen, new Point(x, 0), new Point(x, ActualHeight));
                }
                else
                {
                    double y = Origin + Marker + 0.5;
                    dc.DrawLine(markerPen, new Point(0, y), new Point(ActualWidth, y));
                }
            }
        }
    }
}
