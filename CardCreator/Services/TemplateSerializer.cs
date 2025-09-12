using CardCreator.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CardCreator.Services {
  public static class TemplateSerializer {
    static readonly JsonSerializerOptions Options = new(){ WriteIndented=true, DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull };

    public static void SaveToJson(Canvas canvas, string path, double cardW, double cardH, int sheetCols, int sheetRows){
      var model=new TemplateModel{ CardWidth=cardW, CardHeight=cardH, SheetColumns=sheetCols, SheetRows=sheetRows };
      int z=0;
      foreach(var obj in canvas.Children){
        if(obj is not Grid g || g.Children.Count==0) continue;
        var inner=(FrameworkElement)g.Children[0];
        var item=new TemplateItem{
          X=Canvas.GetLeft(g), Y=Canvas.GetTop(g), Width=g.Width, Height=g.Height, Rotation=GetRotation(inner), Z=z++,
          Hidden=inner.Visibility!=Visibility.Visible,
          ControlName=inner.Tag is string t && !string.IsNullOrWhiteSpace(t) ? t : null
        };
        if(inner is RichTextBox rtb){
          item.Type="Text";
          try{ item.Text=XamlWriter.Save(rtb.Document); }catch{ item.Text=""; }
          item.FontSize=rtb.FontSize; item.Bold=rtb.FontWeight==FontWeights.Bold;
          item.Italic=rtb.FontStyle==FontStyles.Italic; item.TextAlignment=rtb.Document.TextAlignment.ToString();
          item.FontFamily=rtb.FontFamily.Source;
          if(rtb.Foreground is SolidColorBrush scb) item.ForegroundHex=$"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
        } else if(inner is TextBlock tb){
          item.Type="Text"; item.Text=XamlWriter.Save(new FlowDocument(new Paragraph(new Run(tb.Text??"")))); item.FontSize=tb.FontSize; item.Bold=tb.FontWeight==FontWeights.Bold;
          item.Italic=tb.FontStyle==FontStyles.Italic; item.TextAlignment=tb.TextAlignment.ToString();
          item.FontFamily=tb.FontFamily.Source;
          if(tb.Foreground is SolidColorBrush scb) item.ForegroundHex=$"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
        } else if(inner is Image img){
          item.Type="Image"; item.Source=TryGetImageSource(img); item.Stretch=img.Stretch.ToString();
        }
        model.Items.Add(item);
      }
      File.WriteAllText(path, JsonSerializer.Serialize(model, Options));
    }

    public static TemplateModel LoadFromJson(Canvas canvas, string path){
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
          if(item.Hidden==true) img.Visibility=Visibility.Hidden;
          inner=img;
        } else {
          var rtb=new RichTextBox{ FontSize=item.FontSize??28 };
          if(!string.IsNullOrWhiteSpace(item.Text)){
            try{ rtb.Document=(FlowDocument)XamlReader.Parse(item.Text); }
            catch{ rtb.Document=new FlowDocument(new Paragraph(new Run(item.Text))); }
          }
          if(item.Bold==true) rtb.FontWeight=FontWeights.Bold;
          if(item.Italic==true) rtb.FontStyle=FontStyles.Italic;
          if(!string.IsNullOrWhiteSpace(item.FontFamily)) try{ rtb.FontFamily=new FontFamily(item.FontFamily); }catch{}
          if(!string.IsNullOrWhiteSpace(item.TextAlignment) && Enum.TryParse<TextAlignment>(item.TextAlignment, out var ta)) rtb.Document.TextAlignment=ta;
          if(!string.IsNullOrWhiteSpace(item.ForegroundHex)){ try{ rtb.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.ForegroundHex!)); }catch{} }
          if(item.Hidden==true) rtb.Visibility=Visibility.Hidden;
          inner=rtb;
        }
        if(!string.IsNullOrWhiteSpace(item.ControlName)) inner.Name=item.ControlName;
        inner.RenderTransformOrigin=new Point(0.5,0.5);
        ApplyRotation(inner, item.Rotation);
        var container=new Grid{ Background=item.Type=="Image"? (item.Hidden==true?Brushes.Transparent:Brushes.White) : Brushes.Transparent, Width=item.Width, Height=item.Height };
        if(!string.IsNullOrWhiteSpace(item.ControlName)) { inner.Tag=item.ControlName; container.Tag=item.ControlName; }
        container.Children.Add(inner);
        Canvas.SetLeft(container,item.X); Canvas.SetTop(container,item.Y);
        canvas.Children.Add(container);
      }
      return model;
    }

    public static TemplateModel LoadFromXaml(Canvas canvas, string path){
      var xaml=File.ReadAllText(path);
      var obj=XamlReader.Parse(xaml) as Canvas;
      var model=new TemplateModel();
      if(obj!=null){
        canvas.Children.Clear();
        foreach(UIElement child in obj.Children){
          if(child is RichTextBox rtb) rtb.IsHitTestVisible=true;
          canvas.Children.Add(child);
        }
        model.CardWidth=obj.Width;
        model.CardHeight=obj.Height;
        if(obj.Tag is string tag){
          var parts=tag.Split(',');
          if(parts.Length>=2){
            if(int.TryParse(parts[0], out var c)) model.SheetColumns=c;
            if(int.TryParse(parts[1], out var r)) model.SheetRows=r;
          }
        }
      }
      return model;
    }

    public static void ExportToXaml(Canvas canvas, string path, double cardW, double cardH, int sheetCols, int sheetRows){
      var sb=new StringBuilder();
      sb.AppendLine("<Canvas xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\""+cardW+"\" Height=\""+cardH+"\" Tag=\""+sheetCols+","+sheetRows+"\">");
      foreach(var obj in canvas.Children){
        if(obj is not Grid g || g.Children.Count==0) continue;
        var inner=(FrameworkElement)g.Children[0];
        double x=Canvas.GetLeft(g), y=Canvas.GetTop(g);
        string tagAttr="";
        if(inner.Tag is string tag && !string.IsNullOrWhiteSpace(tag)) tagAttr=" Tag=\""+XmlEscape(tag)+"\"";
        if(inner is RichTextBox rtb){
          string color="#000000";
          if(rtb.Foreground is SolidColorBrush scb) color=$"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
          var attrs=" FontSize=\""+rtb.FontSize+"\""+
            " FontFamily=\""+XmlEscape(rtb.FontFamily.Source)+"\""+
            " FontWeight=\""+(rtb.FontWeight==FontWeights.Bold?"Bold":"Normal")+"\""+
            " FontStyle=\""+(rtb.FontStyle==FontStyles.Italic?"Italic":"Normal")+"\""+
            " Foreground=\""+color+"\""+
            " Width=\""+g.Width+"\" Height=\""+g.Height+"\" Canvas.Left=\""+x+"\" Canvas.Top=\""+y+"\""+tagAttr;
          if(rtb.Visibility!=Visibility.Visible) attrs+=" Visibility=\""+rtb.Visibility+"\"";
          sb.AppendLine("  <RichTextBox"+attrs+">");
          var angle=GetRotation(inner);
          if(Math.Abs(angle)>0.001) sb.AppendLine("    <RichTextBox.RenderTransform><RotateTransform Angle=\""+angle+"\"/></RichTextBox.RenderTransform>");
          try{ sb.AppendLine("    "+XamlWriter.Save(rtb.Document)); }catch{}
          sb.AppendLine("  </RichTextBox>");
        } else if(inner is TextBlock tb){
          string color="#000000";
          if(tb.Foreground is SolidColorBrush scb) color=$"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
          var attrs=" Text=\""+XmlEscape(tb.Text)+"\""+
            " FontSize=\""+tb.FontSize+"\""+
            " FontFamily=\""+XmlEscape(tb.FontFamily.Source)+"\""+
            " FontWeight=\""+(tb.FontWeight==FontWeights.Bold?"Bold":"Normal")+"\""+
            " FontStyle=\""+(tb.FontStyle==FontStyles.Italic?"Italic":"Normal")+"\""+
            " TextAlignment=\""+tb.TextAlignment+"\""+
            " Foreground=\""+color+"\""+
            " Width=\""+g.Width+"\" Height=\""+g.Height+"\" Canvas.Left=\""+x+"\" Canvas.Top=\""+y+"\""+tagAttr;
          if(tb.Visibility!=Visibility.Visible) attrs+=" Visibility=\""+tb.Visibility+"\"";
          sb.AppendLine("  <TextBlock"+attrs+">");
          var angle=GetRotation(inner);
          if(Math.Abs(angle)>0.001) sb.AppendLine("    <TextBlock.RenderTransform><RotateTransform Angle=\""+angle+"\"/></TextBlock.RenderTransform>");
          sb.AppendLine("  </TextBlock>");
        } else if(inner is Image img){
          string srcAttr="";
          if(img.Source is BitmapImage bi && bi.UriSource!=null) srcAttr=" Source=\""+XmlEscape(bi.UriSource.ToString())+"\"";
          var attrs=srcAttr+" Stretch=\""+img.Stretch+"\" Width=\""+g.Width+"\" Height=\""+g.Height+"\" Canvas.Left=\""+x+"\" Canvas.Top=\""+y+"\""+tagAttr;
          if(img.Visibility!=Visibility.Visible) attrs+=" Visibility=\""+img.Visibility+"\"";
          sb.AppendLine("  <Image"+attrs+">");
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
