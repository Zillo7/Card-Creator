using System.Collections.Generic;
namespace CardCreator.Models {
  public class TemplateModel {
    public double CardWidth {get;set;}=750;
    public double CardHeight{get;set;}=1050;
    public int SheetColumns {get;set;}=3;
    public int SheetRows {get;set;}=3;
    public List<TemplateItem> Items {get;set;}=new();
  }
  public class TemplateItem {
    public string Type {get;set;}="Text";
    public double X {get;set;}
    public double Y {get;set;}
    public double Width {get;set;}
    public double Height {get;set;}
    public double Rotation {get;set;}
    public int Z {get;set;}
    public string? ControlName {get;set;}
    public string? Text {get;set;}
    public double? FontSize {get;set;}
    public bool? Bold {get;set;}
    public bool? Italic {get;set;}
    public string? FontFamily {get;set;}
    public string? TextAlignment {get;set;}
    public string? ForegroundHex {get;set;}
    public string? Source {get;set;}
    public string? Stretch {get;set;}
    public bool? Hidden {get;set;}
  }
}
