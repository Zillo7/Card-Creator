using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CardCreator.Models {
  public class SelectedElementViewModel : INotifyPropertyChanged {
    public FrameworkElement? Element {get; private set;}
    public FrameworkElement? Container {get; private set;}
    public string? ElementType {get; private set;}
    public bool IsText => Element is RichTextBox;
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
          if (Element is RichTextBox tb) {
            if (tb.Parent is Grid g) g.Width = value;
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
          if (Element is RichTextBox tb) {
            if (tb.Parent is Grid g) g.Height = value;
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
          OnPropertyChanged();
        }
      }
    }
    public double MaxHeight {
      get => Element?.MaxHeight ?? double.PositiveInfinity;
      set {
        if (Element != null) {
          Element.MaxHeight = value;
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
    public FlowDocument Document {
      get => (Element as RichTextBox)?.Document ?? new FlowDocument();
      set { if(Element is RichTextBox rtb){ rtb.Document = value; OnPropertyChanged(); } }
    }
    public string Text {
      get{
        if(Element is RichTextBox rtb){
          var range=new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
          return range.Text;
        }
        return "";
      }
      set{
        if(Element is RichTextBox rtb){
          rtb.Document = new FlowDocument(new Paragraph(new Run(value)));
          OnPropertyChanged();
        }
      }
    }
    public void NotifyTextChanged() => OnPropertyChanged(nameof(Text));
    public IEnumerable<FontFamily> FontFamilies { get; } = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
    public IEnumerable<double> FontSizes { get; } = new double[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };
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
