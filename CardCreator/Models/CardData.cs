using System.Collections.Generic;

namespace CardCreator.Models {
  public class CardData {
    public string Name {get;set;}="Card";
    public int Quantity {get;set;}=1;
    public Dictionary<string, CardField> Fields {get;} = new();
  }
  public class CardField {
    public string? Text {get;set;}
    public double? FontSize {get;set;}
    public string? FontFamily {get;set;}
    public string? FontStyle {get;set;}
    public string? TextAlignment {get;set;}
    public string? Foreground {get;set;}
    public string? Source {get;set;}
    public string? Stretch {get;set;}
    public bool? Hidden {get;set;}
  }
}
