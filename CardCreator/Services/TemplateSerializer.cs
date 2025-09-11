using CardCreator.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CardCreator.Services {
  public static class TemplateSerializer {
    static readonly JsonSerializerOptions Options = new(){ WriteIndented=true, DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull };

    public static void SaveToJson(Canvas canvas, string path, double cardW, double cardH){
      var model=new TemplateModel{ CardWidth=cardW, CardHeight=cardH };
      int z=0;
      foreach(var obj in canvas.Children){
        if(obj is not Grid g || g.Children.Count==0) continue;
        var inner=(FrameworkElement)g.Children[0];
        var item=new TemplateItem{
          X=Canvas.GetLeft(g), Y=Canvas.GetTop(g), Width=g.Width, Height=g.Height, Rotation=GetRotation(inner), Z=z++
        };
        if(inner is TextBlock tb){
          item.Type="Text"; item.Text=tb.Text; item.FontSize=tb.FontSize; item.Bold=tb.FontWeight==FontWeights.Bold;
          if(tb.Foreground is SolidColorBrush scb) item.ForegroundHex=$"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
        } else if(inner is Image img){
          item.Type="Image"; item.Source=TryGetImageSource(img); item.Stretch=img.Stretch.ToString();
        }
        model.Items.Add(item);
      }
      File.WriteAllText(path, JsonSerializer.Serialize(model, Options));
    }

    public static void LoadFromJson(Canvas canvas, string path){
      var json=File.ReadAllText(path);
      var model=JsonSerializer.Deserialize<TemplateModel>(json,Options) ?? new TemplateModel();
      canvas.Children.Clear();
      foreach(var item in model.Items.OrderBy(i=>i.Z)){
        FrameworkElement inner;
        if(item.Type=="Image"){
          var img=new Image{ Stretch= Enum.TryParse<Stretch>(item.Stretch??"Uniform", out var s) ? s : Stretch.Uniform };
          if(!string.IsNullOrWhiteSpace(item.Source)){
            try{
              var bi=new BitmapImage();
              bi.BeginInit(); bi.UriSource=new Uri(item.Source,UriKind.RelativeOrAbsolute); bi.CacheOption=BitmapCacheOption.OnLoad; bi.EndInit();
              img.Source=bi;
            }catch{}
          }
          inner=img;
        } else {
          var tb=new TextBlock{ Text=item.Text??"Text", FontSize=item.FontSize??28 };
          if(item.Bold==true) tb.FontWeight=FontWeights.Bold;
          if(!string.IsNullOrWhiteSpace(item.ForegroundHex)){ try{ tb.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.ForegroundHex!)); }catch{} }
          inner=tb;
        }
        inner.RenderTransformOrigin=new Point(0.5,0.5);
        ApplyRotation(inner, item.Rotation);
        var container=new Grid{ Background=Brushes.Transparent, Width=item.Width, Height=item.Height };
        container.Children.Add(inner);
        Canvas.SetLeft(container,item.X); Canvas.SetTop(container,item.Y);
        canvas.Children.Add(container);
      }
    }

    public static void ExportToXaml(Canvas canvas, string path, double cardW, double cardH){
      var sb=new StringBuilder();
      sb.AppendLine("<Canvas xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\""+cardW+"\" Height=\""+cardH+"\">"); 
      foreach(var obj in canvas.Children){
        if(obj is not Grid g || g.Children.Count==0) continue;
        var inner=(FrameworkElement)g.Children[0];
        double x=Canvas.GetLeft(g), y=Canvas.GetTop(g);
        if(inner is TextBlock tb){
          string color="#000000";
          if(tb.Foreground is SolidColorBrush scb) color=$"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
          sb.AppendLine("  <TextBlock Text=\""+XmlEscape(tb.Text)+"\" FontSize=\""+tb.FontSize+"\" FontWeight=\""+(tb.FontWeight==FontWeights.Bold?"Bold":"Normal")+"\" Foreground=\""+color+"\" Width=\""+g.Width+"\" Height=\""+g.Height+"\" Canvas.Left=\""+x+"\" Canvas.Top=\""+y+"\">"); 
          var angle=GetRotation(inner);
          if(Math.Abs(angle)>0.001) sb.AppendLine("    <TextBlock.RenderTransform><RotateTransform Angle=\""+angle+"\"/></TextBlock.RenderTransform>");
          sb.AppendLine("  </TextBlock>");
        } else if(inner is Image img){
          string srcAttr="";
          if(img.Source is BitmapImage bi && bi.UriSource!=null) srcAttr=" Source=\""+XmlEscape(bi.UriSource.ToString())+"\"";
          sb.AppendLine("  <Image"+srcAttr+" Stretch=\""+img.Stretch+"\" Width=\""+g.Width+"\" Height=\""+g.Height+"\" Canvas.Left=\""+x+"\" Canvas.Top=\""+y+"\">"); 
          var angle=GetRotation(inner);
          if(Math.Abs(angle)>0.001) sb.AppendLine("    <Image.RenderTransform><RotateTransform Angle=\""+angle+"\"/></Image.RenderTransform>");
          sb.AppendLine("  </Image>");
        }
      }
      sb.AppendLine("</Canvas>");
      File.WriteAllText(path, sb.ToString());
    }

    private static string XmlEscape(string s){ return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;"); }
    private static double GetRotation(FrameworkElement el){
      if(el.RenderTransform is TransformGroup tg) foreach(var t in tg.Children) if(t is RotateTransform r) return r.Angle;
      if(el.RenderTransform is RotateTransform r2) return r2.Angle;
      return 0;
    }
    private static void ApplyRotation(FrameworkElement el,double angle){
      if(Math.Abs(angle)<0.001) return;
      if(el.RenderTransform is not TransformGroup tg){ tg=new TransformGroup(); el.RenderTransform=tg; }
      tg.Children.Add(new RotateTransform(angle));
    }
    private static string? TryGetImageSource(Image img){
      if(img.Source is BitmapImage bi) return bi.UriSource?.LocalPath ?? bi.UriSource?.ToString();
      return null;
    }
  }
}
