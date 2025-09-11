using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CardCreator.Models {
  public class SelectedElementViewModel : INotifyPropertyChanged {
    public FrameworkElement? Element {get; private set;}
    public string? ElementType {get; private set;}
    public bool IsText => Element is TextBlock;
    public bool IsImage => Element is Image;
    public void SetElement(FrameworkElement? element){ Element=element; ElementType=element?.GetType().Name; OnPropertyChanged(string.Empty); }
    double GetLeft()=> Element!=null? Canvas.GetLeft(Element):0;
    void SetLeft(double v){ if(Element!=null){ Canvas.SetLeft(Element,v); OnPropertyChanged(nameof(X)); } }
    double GetTop()=> Element!=null? Canvas.GetTop(Element):0;
    void SetTop(double v){ if(Element!=null){ Canvas.SetTop(Element,v); OnPropertyChanged(nameof(Y)); } }
    public double X { get=>GetLeft(); set=>SetLeft(value); }
    public double Y { get=>GetTop(); set=>SetTop(value); }
    public double Width { get=> Element?.Width is double d && !double.IsNaN(d)? d : (Element?.ActualWidth??0); set{ if(Element!=null){ Element.Width=value; OnPropertyChanged(); } } }
    public double Height{ get=> Element?.Height is double d && !double.IsNaN(d)? d : (Element?.ActualHeight??0); set{ if(Element!=null){ Element.Height=value; OnPropertyChanged(); } } }
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
    public string Text { get=> (Element as TextBlock)?.Text ?? ""; set{ if(Element is TextBlock tb){ tb.Text=value; OnPropertyChanged(); } } }
    public double FontSize { get=> (Element as TextBlock)?.FontSize ?? 16; set{ if(Element is TextBlock tb){ tb.FontSize=value; OnPropertyChanged(); } } }
    public bool Bold { get=> (Element as TextBlock)?.FontWeight==FontWeights.Bold; set{ if(Element is TextBlock tb){ tb.FontWeight=value? FontWeights.Bold:FontWeights.Normal; OnPropertyChanged(); } } }
    public string ForegroundHex {
      get{
        if(Element is TextBlock tb && tb.Foreground is SolidColorBrush scb) return $"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
        return "#000000";
      }
      set{
        if(Element is TextBlock tb){
          try{ tb.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)); }catch{}
          OnPropertyChanged();
        }
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
