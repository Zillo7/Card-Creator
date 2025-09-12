using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CardCreator.Models {
  public class SelectedElementViewModel : INotifyPropertyChanged {
    public FrameworkElement? Element {get; private set;}
    public FrameworkElement? Container {get; private set;}
    public string? ElementType {get; private set;}
    public bool IsText => Element is TextBlock;
    public bool IsImage => Element is Image;
    public void SetElement(FrameworkElement? element){ Element=element; Container=element?.Parent as FrameworkElement; ElementType=element?.GetType().Name; OnPropertyChanged(string.Empty); }
    public string ControlName {
      get => Element?.Tag as string ?? "";
      set {
        if (Element != null) {
          Element.Tag = value;
          if (Container != null) Container.Tag = value;
          OnPropertyChanged();
        }
      }
    }
    double GetLeft()=> Container!=null? Canvas.GetLeft(Container):0;
    void SetLeft(double v){ if(Container!=null){ Canvas.SetLeft(Container,v); OnPropertyChanged(nameof(X)); } }
    double GetTop()=> Container!=null? Canvas.GetTop(Container):0;
    void SetTop(double v){ if(Container!=null){ Canvas.SetTop(Container,v); OnPropertyChanged(nameof(Y)); } }
    public void RefreshPosition(){ OnPropertyChanged(nameof(X)); OnPropertyChanged(nameof(Y)); }
    public double X { get=>GetLeft(); set=>SetLeft(value); }
    public double Y { get=>GetTop(); set=>SetTop(value); }
    public double Width {
      get => Element?.Width ?? double.NaN;
      set {
        if (Element != null) {
          Element.Width = value;
          if (Element is TextBlock tb) {
            if (tb.Parent is Grid g) g.Width = value;
            //if (double.IsNaN(value)) FitToText(tb);
          }
          OnPropertyChanged();
          OnPropertyChanged(nameof(WidthInput));
        }
      }
    }
    public double Height {
      get => Element?.Height ?? double.NaN;
      set {
        if (Element != null) {
          Element.Height = value;
          if (Element is TextBlock tb) {
            if (tb.Parent is Grid g) g.Height = value;
            //if (double.IsNaN(value)) FitToText(tb);
          }
          OnPropertyChanged();
          OnPropertyChanged(nameof(HeightInput));
        }
      }
    }
    public string WidthInput {
      get => double.IsNaN(Width) ? "" : Width.ToString();
      set {
        if (string.IsNullOrWhiteSpace(value))
          Width = double.NaN;
        else if (double.TryParse(value, out var v))
          Width = v;
      }
    }
    public string HeightInput {
      get => double.IsNaN(Height) ? "" : Height.ToString();
      set {
        if (string.IsNullOrWhiteSpace(value))
          Height = double.NaN;
        else if (double.TryParse(value, out var v))
          Height = v;
      }
    }
    public double MaxWidth {
      get => Element?.MaxWidth ?? double.PositiveInfinity;
      set {
        if (Element != null) {
          Element.MaxWidth = value;
          //if (Element is TextBlock tb) { FitToText(tb); }
          OnPropertyChanged();
        }
      }
    }
    public double MaxHeight {
      get => Element?.MaxHeight ?? double.PositiveInfinity;
      set {
        if (Element != null) {
          Element.MaxHeight = value;
          //if (Element is TextBlock tb) { FitToText(tb); }
          OnPropertyChanged();
        }
      }
    }
    public double Rotation {
      get{
        if(Element?.RenderTransform is TransformGroup tg)
          foreach(var t in tg.Children) if(t is RotateTransform r) return r.Angle;
        if(Element?.RenderTransform is RotateTransform r2) return r2.Angle;
        return 0;
      }
      set{
        if(Element==null) return;
        TransformGroup tg = Element.RenderTransform as TransformGroup ?? new TransformGroup();
        RotateTransform? r=null;
        foreach(var t in tg.Children) if(t is RotateTransform rr){ r=rr; break; }
        if(r==null){ tg.Children.Add(new RotateTransform(value)); Element.RenderTransform=tg; } else r.Angle=value;
        Element.RenderTransformOrigin=new Point(0.5,0.5);
        OnPropertyChanged();
      }
    }
    void FitToText(TextBlock tb){
            var ft = new FormattedText(tb.Text ?? "", CultureInfo.CurrentCulture, tb.FlowDirection, new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch), tb.FontSize, tb.Foreground, VisualTreeHelper.GetDpi(tb).PixelsPerDip);
            var w = ft.WidthIncludingTrailingWhitespace; var h = ft.Height;
            //tb.Width = w; tb.Height = h;
            if (tb.Parent is Grid g) { g.Width = w; g.Height = h; }
        }
    public string Text { get=> (Element as TextBlock)?.Text ?? ""; set{ if(Element is TextBlock tb){ tb.Text=value; OnPropertyChanged(); } } }
    public double FontSize { get=> (Element as TextBlock)?.FontSize ?? 16; set{ if(Element is TextBlock tb){ tb.FontSize=value; OnPropertyChanged(); } } }

    public IEnumerable<FontFamily> FontFamilies { get; } = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
    public FontFamily FontFamily {
      get => (Element as TextBlock)?.FontFamily ?? Fonts.SystemFontFamilies.First();
      set { if (Element is TextBlock tb) { tb.FontFamily = value; OnPropertyChanged(); } }
    }

    public string FontStyleOption {
      get {
        if (Element is TextBlock tb) {
          bool bold = tb.FontWeight == FontWeights.Bold;
          bool italic = tb.FontStyle == FontStyles.Italic;
          if (bold && italic) return "Bold Italic";
          if (bold) return "Bold";
          if (italic) return "Italic";
          return "None";
        }
        return "None";
      }
      set {
        if (Element is TextBlock tb) {
          switch (value) {
            case "Italic":
              tb.FontStyle = FontStyles.Italic;
              tb.FontWeight = FontWeights.Normal;
              break;
            case "Bold":
              tb.FontStyle = FontStyles.Normal;
              tb.FontWeight = FontWeights.Bold;
              break;
            case "Bold Italic":
              tb.FontStyle = FontStyles.Italic;
              tb.FontWeight = FontWeights.Bold;
              break;
            default:
              tb.FontStyle = FontStyles.Normal;
              tb.FontWeight = FontWeights.Normal;
              break;
          }
          //FitToText(tb);
          OnPropertyChanged();
        }
      }
    }
    public TextAlignment TextAlignment { get => (Element as TextBlock)?.TextAlignment ?? TextAlignment.Left; set { if (Element is TextBlock tb) { tb.TextAlignment = value; OnPropertyChanged(); } } }
    public Color ForegroundColor {
      get {
        if (Element is TextBlock tb && tb.Foreground is SolidColorBrush scb) return scb.Color;
        return Colors.Black;
      }
      set {
        if (Element is TextBlock tb) {
          tb.Foreground = new SolidColorBrush(value);
          OnPropertyChanged();
          OnPropertyChanged(nameof(ForegroundHex));
        }
      }
    }
    public string ForegroundHex {
      get => $"#{ForegroundColor.R:X2}{ForegroundColor.G:X2}{ForegroundColor.B:X2}";
      set {
        try { ForegroundColor = (Color)ColorConverter.ConvertFromString(value); } catch {}
      }
    }
    public bool IsHidden {
      get {
        if (Element == null) return false;
        return Element.Visibility != Visibility.Visible;
      }
      set {
        if (Element == null) return;
        Element.Visibility = value ? Visibility.Hidden : Visibility.Visible;
        if (Element.Parent is Grid g && Element is Image)
          g.Background = value ? Brushes.Transparent : Brushes.White;
        OnPropertyChanged();
      }
    }
    public string ImageSourcePath {
      get{
        if(Element is Image img && img.Source is System.Windows.Media.Imaging.BitmapImage bi)
          return bi.UriSource?.LocalPath ?? bi.UriSource?.ToString() ?? "";
        return "";
      }
      set{
        if(Element is Image img){
          try{
            var bi=new System.Windows.Media.Imaging.BitmapImage();
            bi.BeginInit(); bi.UriSource=new Uri(value,UriKind.RelativeOrAbsolute); bi.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bi.EndInit();
            img.Source=bi; OnPropertyChanged();
          }catch{}
        }
      }
    }
    public Stretch ImageStretch { get=> (Element as Image)?.Stretch ?? Stretch.Uniform; set{ if(Element is Image img){ img.Stretch=value; OnPropertyChanged(); } } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name=null)=> PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
  }
}
