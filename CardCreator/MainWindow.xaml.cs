using CardCreator.Models;
using CardCreator.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;
using System.Windows.Markup;
using WinForms = System.Windows.Forms;
using DrawingColor = System.Drawing.Color;
using IOPath = System.IO.Path;

namespace CardCreator
{
public partial class MainWindow : Window
{
    public MainViewModel VM => (MainViewModel)DataContext;
    public MainWindow()
    {
        Resources["NullToBoolInverse"] = new NullToBoolInverseConverter();
        InitializeComponent();
        CardCanvas.AddHandler(UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler(CardCanvas_MouseLeftButtonDown), true);
        CardCanvas.AddHandler(UIElement.MouseMoveEvent,
            new MouseEventHandler(CardCanvas_MouseMove), true);
        CardCanvas.AddHandler(UIElement.MouseLeftButtonUpEvent,
            new MouseButtonEventHandler(CardCanvas_MouseLeftButtonUp), true);
        CardCanvas.AddHandler(UIElement.MouseLeaveEvent,
            new MouseEventHandler(CardCanvas_MouseLeave), true);
        VM.AttachCanvas(CardCanvas, GuideH, GuideV, Marquee);
        Loaded += (_, __) => UpdateRulerOrigins();
        CardCanvas.SizeChanged += (_, __) => UpdateRulerOrigins();
        VM.Inspector.PropertyChanged += Inspector_PropertyChanged;
    }
    private void CardCanvas_MouseLeftButtonDown(object s, MouseButtonEventArgs e) => VM.OnCanvasMouseLeftDown(e);
    private void CardCanvas_MouseMove(object s, MouseEventArgs e)
    {
        VM.OnCanvasMouseMove(e);
        var p = e.GetPosition(CardCanvas);
        RulerH.Marker = p.X;
        RulerV.Marker = p.Y;
        UpdateMarkerReadout(p);
    }
    private void CardCanvas_MouseLeftButtonUp(object s, MouseButtonEventArgs e) => VM.OnCanvasMouseLeftUp(e);
    private void CardCanvas_MouseLeave(object s, MouseEventArgs e)
    {
        RulerH.Marker = double.NaN;
        RulerV.Marker = double.NaN;
        MarkerReadout.Text = string.Empty;
    }

    private void Inspector_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
            UpdateFontToolbarFormatting();
    }

    private void UpdateRulerOrigins()
    {
        var originInH = CardCanvas.TranslatePoint(new Point(0, 0), RulerH);
        var originInV = CardCanvas.TranslatePoint(new Point(0, 0), RulerV);
        RulerH.Origin = originInH.X;
        RulerV.Origin = originInV.Y;
    }
    private void UpdateMarkerReadout(Point p)
    {
        var unit = UnitSuffix();
        MarkerReadout.Text = string.Format(CultureInfo.InvariantCulture,
            "X: {0:0.##}{2} Y: {1:0.##}{2}",
            DiuToUnits(p.X), DiuToUnits(p.Y), unit);
    }
    private double DiuToUnits(double value) => VM.Units switch
    {
        MeasurementUnit.Inches => value / 96.0,
        MeasurementUnit.Millimeters => value * 25.4 / 96.0,
        _ => value,
    };
    private string UnitSuffix() => VM.Units switch
    {
        MeasurementUnit.Inches => " in",
        MeasurementUnit.Millimeters => " mm",
        _ => " px",
    };
    private void BrowseImage_Click(object s, RoutedEventArgs e)
    {
        if (VM.Inspector.Element is not Image)
            return;
        var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*" };
        if (dlg.ShowDialog() == true)
            VM.Inspector.ImageSourcePath = dlg.FileName;
    }
    private void PickColor_Click(object s, RoutedEventArgs e)
    {
        if (VM.Inspector.Element is not RichTextBox rtb)
            return;
        var dlg = new WinForms.ColorDialog();
        var c = GetSelectionColor(rtb);
        dlg.Color = DrawingColor.FromArgb(c.A, c.R, c.G, c.B);
        if (dlg.ShowDialog() == WinForms.DialogResult.OK)
        {
            var color = Color.FromArgb(dlg.Color.A, dlg.Color.R, dlg.Color.G, dlg.Color.B);
            var sel = new TextRange(rtb.Selection.Start, rtb.Selection.End);
            sel.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            UpdateFontToolbarFormatting();
            VM.Inspector.NotifyTextChanged();
        }
    }
    private void ToggleStrikethrough_Click(object s, RoutedEventArgs e)
    {
        if (VM.Inspector.Element is not RichTextBox rtb)
            return;
        var sel = new TextRange(rtb.Selection.Start, rtb.Selection.End);
        var current = sel.GetPropertyValue(Inline.TextDecorationsProperty);
        bool has = current is TextDecorationCollection tdc &&
                   tdc.Any(td => td.Location == TextDecorationLocation.Strikethrough);
        sel.ApplyPropertyValue(Inline.TextDecorationsProperty,
            has ? null : TextDecorations.Strikethrough);
        rtb.Focus();
    }
    private void InsertImage_Click(object s, RoutedEventArgs e)
    {
        if (VM.Inspector.Element is not RichTextBox rtb)
            return;
        var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png" };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(dlg.FileName);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            var container = VM.CreateRtbImageContainer(bi);
            _ = new InlineUIContainer(container, rtb.CaretPosition);
            rtb.Focus();
        }
        catch { }
    }
    private void FontFamilyCombo_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (VM.Inspector.Element is not RichTextBox rtb || FontFamilyCombo.SelectedItem is not FontFamily ff)
            return;
        var sel = new TextRange(rtb.Selection.Start, rtb.Selection.End);
        sel.ApplyPropertyValue(TextElement.FontFamilyProperty, ff);
        rtb.Focus();
        UpdateFontToolbarFormatting();
        VM.Inspector.NotifyTextChanged();
    }
    private void FontSizeCombo_SelectionChanged(object s, SelectionChangedEventArgs e) => ApplyFontSizeFromCombo();
    private void FontSizeCombo_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            ApplyFontSizeFromText();
    }
    private void FontSizeCombo_LostFocus(object s, RoutedEventArgs e) => ApplyFontSizeFromText();
    private void ApplyFontSizeFromCombo()
    {
        if (VM.Inspector.Element is not RichTextBox rtb)
            return;
        if (FontSizeCombo.SelectedItem is double size)
        {
            var sel = new TextRange(rtb.Selection.Start, rtb.Selection.End);
            sel.ApplyPropertyValue(TextElement.FontSizeProperty, size);
            rtb.Focus();
            UpdateFontToolbarFormatting();
            VM.Inspector.NotifyTextChanged();
        }
    }
    private void ApplyFontSizeFromText()
    {
        if (VM.Inspector.Element is not RichTextBox rtb)
            return;
        if (double.TryParse(FontSizeCombo.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var size))
        {
            var sel = new TextRange(rtb.Selection.Start, rtb.Selection.End);
            sel.ApplyPropertyValue(TextElement.FontSizeProperty, size);
            rtb.Focus();
            UpdateFontToolbarFormatting();
            VM.Inspector.NotifyTextChanged();
        }
    }
    internal void UpdateFontToolbarFormatting()
    {
        if (VM.Inspector.Element is not RichTextBox rtb)
        {
            FontFamilyCombo.SelectedItem = null;
            FontSizeCombo.Text = string.Empty;
            ForegroundPreview.Background = Brushes.Black;
            return;
        }
        var sel = rtb.Selection;
        var ffObj = sel.GetPropertyValue(TextElement.FontFamilyProperty);
        if (ffObj is FontFamily fam)
            FontFamilyCombo.SelectedItem = fam;
        else
            FontFamilyCombo.SelectedItem = null;
        var fsObj = sel.GetPropertyValue(TextElement.FontSizeProperty);
        if (fsObj is double fs)
            FontSizeCombo.Text = fs.ToString(CultureInfo.InvariantCulture);
        else
            FontSizeCombo.Text = rtb.FontSize.ToString(CultureInfo.InvariantCulture);
        var colObj = sel.GetPropertyValue(TextElement.ForegroundProperty);
        if (colObj is SolidColorBrush scb)
            ForegroundPreview.Background = scb;
        else
            ForegroundPreview.Background = Brushes.Black;
    }
    private Color GetSelectionColor(RichTextBox rtb)
    {
        var obj = rtb.Selection.GetPropertyValue(TextElement.ForegroundProperty);
        return obj is SolidColorBrush scb ? scb.Color : Colors.Black;
    }
    private void Workspace_MouseLeftButtonDown(object s, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        if (MainViewModel.FindAncestor<Canvas>(source) != CardCanvas)
            VM.ClearSelection();
    }
    private void Window_KeyDown(object s, KeyEventArgs e) => VM.OnKeyDown(e);
}

public class MainViewModel : INotifyPropertyChanged
{
    private Canvas? _canvas;
    private Line? _guideH, _guideV;
    private Rectangle? _marquee;
    private Point? _dragStart;
    private Vector _dragOffset;
    private Grid? _draggingContainer;
    private bool _draggingMarquee;
    private Point _marqueeStart;

    public RelayCommand AddTextCommand { get; }
    public RelayCommand AddImageCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand LoadCommand { get; }
    public RelayCommand ExportXamlCommand { get; }
    public RelayCommand AlignLeftCommand { get; }
    public RelayCommand AlignCenterCommand { get; }
    public RelayCommand AlignRightCommand { get; }
    public RelayCommand AlignTopCommand { get; }
    public RelayCommand AlignMiddleCommand { get; }
    public RelayCommand AlignBottomCommand { get; }
    public RelayCommand DistributeHCommand { get; }
    public RelayCommand DistributeVCommand { get; }
    public RelayCommand BringForwardCommand { get; }
    public RelayCommand SendBackwardCommand { get; }
    public RelayCommand SettingsCommand { get; }
    public RelayCommand AddCardCommand { get; }
    public RelayCommand RemoveCardCommand { get; }
    public RelayCommand SaveCsvCommand { get; }
    public RelayCommand LoadCsvCommand { get; }
    public RelayCommand SaveImagesCommand { get; }
    public RelayCommand SaveSheetsCommand { get; }

    public SelectedElementViewModel Inspector { get; } = new();
    private readonly List<Grid> _selected = new();
    private Grid? _selectedRtbImage;
    private double _cardWidth = 240;
    public double CardWidth
    {
        get => _cardWidth;
        set {
            _cardWidth = value;
            OnPropertyChanged();
        }
    }
    private double _cardHeight = 336;
    public double CardHeight
    {
        get => _cardHeight;
        set {
            _cardHeight = value;
            OnPropertyChanged();
        }
    }
    public IEnumerable<Grid> SelectedItems => _selected;
    public FrameworkElement? SingleSelectedInner => _selected.Count == 1
        ? (FrameworkElement ?) _selected[0].Children[0]: null;
    public bool HasSelection => _selected.Count > 0;

    public ObservableCollection<CardData> Cards { get; } = new();
    private CardData? _selectedCard;
    public CardData? SelectedCard
    {
        get => _selectedCard;
        set {
            _selectedCard = value;
            OnPropertyChanged();
            RemoveCardCommand.RaiseCanExecuteChanged();
            ApplyCardData();
        }
    }
    private int _cardCounter = 1;
    private string _lastControlName = "";

    private bool _snapEnabled = true;
    public bool SnapEnabled
    {
        get => _snapEnabled;
        set {
            _snapEnabled = value;
            OnPropertyChanged();
        }
    }
    private int _gridSize = 10;
    public int GridSize
    {
        get => _gridSize;
        set {
            _gridSize = value;
            OnPropertyChanged();
        }
    }
    private bool _guidelinesEnabled = true;
    public bool GuidelinesEnabled
    {
        get => _guidelinesEnabled;
        set {
            _guidelinesEnabled = value;
            OnPropertyChanged();
        }
    }

    private MeasurementUnit _units = MeasurementUnit.Inches;
    public MeasurementUnit Units
    {
        get => _units;
        set
        {
            _units = value;
            OnPropertyChanged();
        }
    }

    private int _sheetColumns = 3, _sheetRows = 3;
    private bool _useJpeg;

    public MainViewModel()
    {
        AddTextCommand = new RelayCommand(
            _ => AddText());
        AddImageCommand = new RelayCommand(
            _ => AddImage());
        SaveCommand = new RelayCommand(
            _ => Save());
        LoadCommand = new RelayCommand(
            _ => Load());
        ExportXamlCommand = new RelayCommand(
            _ => ExportXaml());
        AlignLeftCommand = new RelayCommand(
            _ => AlignLeft(),
            _ => _selected.Count >= 2);
        AlignCenterCommand = new RelayCommand(
            _ => AlignCenter(),
            _ => _selected.Count >= 2);
        AlignRightCommand = new RelayCommand(
            _ => AlignRight(),
            _ => _selected.Count >= 2);
        AlignTopCommand = new RelayCommand(
            _ => AlignTop(),
            _ => _selected.Count >= 2);
        AlignMiddleCommand = new RelayCommand(
            _ => AlignMiddle(),
            _ => _selected.Count >= 2);
        AlignBottomCommand = new RelayCommand(
            _ => AlignBottom(),
            _ => _selected.Count >= 2);
        DistributeHCommand = new RelayCommand(
            _ => DistributeH(),
            _ => _selected.Count >= 3);
        DistributeVCommand = new RelayCommand(
            _ => DistributeV(),
            _ => _selected.Count >= 3);
        BringForwardCommand = new RelayCommand(
            _ => ChangeZ(1),
            _ => _selected.Count >= 1);
        SendBackwardCommand = new RelayCommand(
            _ => ChangeZ(-1),
            _ => _selected.Count >= 1);
        SettingsCommand = new RelayCommand(
            _ => ChangeSettings());
        AddCardCommand = new RelayCommand(
            _ => AddCard());
        RemoveCardCommand = new RelayCommand(
            _ => RemoveCard(),
            _ => SelectedCard != null);
        SaveCsvCommand = new RelayCommand(
            _ => SaveCsv());
        LoadCsvCommand = new RelayCommand(
            _ => LoadCsv());
        SaveImagesCommand = new RelayCommand(
            _ => SaveImages(),
            _ => Cards.Count > 0);
        SaveSheetsCommand = new RelayCommand(
            _ => SaveSheets(),
            _ => Cards.Count > 0);
        Cards.CollectionChanged += (_, __) =>
        {
            SaveImagesCommand.RaiseCanExecuteChanged();
            SaveSheetsCommand.RaiseCanExecuteChanged();
        };
        Inspector.PropertyChanged += OnInspectorPropertyChanged;
    }

    private void ChangeSettings()
    {
        var dlg = new SettingsDialog(CardWidth, CardHeight, _sheetColumns, _sheetRows, _useJpeg)
        {
            Owner = Application.Current.MainWindow
        };
        if (dlg.ShowDialog() == true)
        {
            CardWidth = dlg.VM.WidthDip;
            CardHeight = dlg.VM.HeightDip;
            _sheetColumns = dlg.Columns;
            _sheetRows = dlg.Rows;
            _useJpeg = dlg.UseJpeg;
        }
    }

    public void AttachCanvas(Canvas canvas, Line guideH, Line guideV, Rectangle marquee)
    {
        _canvas = canvas;
        _guideH = guideH;
        _guideV = guideV;
        _marquee = marquee;
    }

    public void OnCanvasMouseLeftDown(MouseButtonEventArgs e)
    {
        if (_canvas == null)
            return;
        var pos = e.GetPosition(_canvas);
        var source = e.OriginalSource as DependencyObject;
        var iuic = FindAncestor<InlineUIContainer>(source);
        if (iuic?.Child is Grid g)
        {
            ClearSelection();
            _selectedRtbImage = g;
            if (g.Children.Count > 1 && g.Children[1] is Border b)
                b.Visibility = Visibility.Visible;
            Inspector.SetElement((FrameworkElement)g.Children[0]);
            return;
        }
        if (e.OriginalSource == _canvas)
        {
            _draggingMarquee = true;
            _marqueeStart = pos;
            ShowMarquee(pos, pos);
            if (!IsAdditiveSelect())
                ClearSelection();
            return;
        }
        var rtb = FindAncestor<RichTextBox>(source);
        if (rtb != null)
            source = rtb;
        var container = FindAncestor<Grid>(source);
        if (container != null && _canvas.Children.Contains(container))
        {
            if (e.OriginalSource is Thumb)
            {
                if (IsAdditiveSelect())
                    ToggleSelection(container);
                else if (!_selected.Contains(container))
                {
                    ClearSelection();
                    _selected.Add(container);
                    UpdateSelectionVisuals();
                }
                return;
            }
            if (IsAdditiveSelect())
                ToggleSelection(container);
            else
            {
                if (!_selected.Contains(container))
                {
                    ClearSelection();
                    _selected.Add(container);
                    UpdateSelectionVisuals();
                }
            }
            _draggingContainer = container;
            _dragStart = pos;
            _dragOffset = new Vector(Canvas.GetLeft(container), Canvas.GetTop(container));
            container.CaptureMouse();
            e.Handled = true;
        }
    }

    public void OnCanvasMouseMove(MouseEventArgs e)
    {
        if (_canvas == null)
            return;
        var pos = e.GetPosition(_canvas);
        if (_draggingMarquee && _marquee != null)
        {
            ShowMarquee(_marqueeStart, pos);
            UpdateMarqueeSelection();
            return;
        }
        if (_dragStart.HasValue && _draggingContainer != null && _draggingContainer.IsMouseCaptured)
        {
            var delta = pos - _dragStart.Value;
            var nx = _dragOffset.X + delta.X;
            var ny = _dragOffset.Y + delta.Y;
            if (SnapEnabled)
            {
                nx = Snap(nx);
                ny = Snap(ny);
            }
            Canvas.SetLeft(_draggingContainer, Math.Max(0, nx));
            Canvas.SetTop(_draggingContainer, Math.Max(0, ny));
            if (GuidelinesEnabled)
                UpdateGuidelines(_draggingContainer);
            if (_selected.Count == 1 && _selected[0] == _draggingContainer)
                Inspector.RefreshPosition();
        }
    }

    public void OnCanvasMouseLeftUp(MouseButtonEventArgs e)
    {
        if (_draggingContainer != null)
        {
            _draggingContainer.ReleaseMouseCapture();
            _draggingContainer = null;
        }
        _dragStart = null;
        HideGuides();
        if (_draggingMarquee)
        {
            _draggingMarquee = false;
            if (_marquee != null)
                _marquee.Visibility = Visibility.Collapsed;
        }
        UpdateInspector();
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        if (_canvas == null)
            return;
        if (e.Key == Key.Escape)
        {
            if (_selected.Count > 0)
            {
                ClearSelection();
                e.Handled = true;
            }
            return;
        }
        if (_selected.Count == 0)
            return;
        if (e.Key == Key.Delete)
        {
            DeleteSelected();
            e.Handled = true;
            return;
        }
        int step = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? 10 : 1;
        bool handled = false;
        foreach (var c in _selected)
        {
            double x = Canvas.GetLeft(c), y = Canvas.GetTop(c);
            switch (e.Key)
            {
            case Key.Left:
                x -= step;
                handled = true;
                break;
            case Key.Right:
                x += step;
                handled = true;
                break;
            case Key.Up:
                y -= step;
                handled = true;
                break;
            case Key.Down:
                y += step;
                handled = true;
                break;
            }
            if (handled)
            {
                if (SnapEnabled)
                {
                    x = Snap(x);
                    y = Snap(y);
                }
                Canvas.SetLeft(c, Math.Max(0, x));
                Canvas.SetTop(c, Math.Max(0, y));
            }
        }
        if (handled)
        {
            e.Handled = true;
            UpdateInspector();
        }
    }

    public void AddText()
    {
        if (_canvas == null)
            return;
        var tb = new RichTextBox
        {
            FontSize = 28,
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            BorderBrush = null,
            BorderThickness = new Thickness(0),
            FocusVisualStyle = null,
            RenderTransformOrigin = new Point(0.5, 0.5)
        };
        tb.Document = new FlowDocument(new Paragraph(new Run("Text")));
        var container = CreateContainer(tb, 60, 60, 180, 60);
        tb.Width = 180;
        tb.Height = 60;
        tb.HorizontalAlignment = HorizontalAlignment.Stretch;
        tb.VerticalAlignment = VerticalAlignment.Stretch;
        _canvas.Children.Add(container);
        SelectSingle(container);
    }
    public void AddImage()
    {
        if (_canvas == null)
            return;
        var img = new Image { Stretch = Stretch.Uniform, RenderTransformOrigin = new Point(0.5, 0.5) };
        var container = CreateContainer(img, 100, 100, 200, 140);
        img.Width = 200;
        img.Height = 140;
        _canvas.Children.Add(container);
        SelectSingle(container);
    }

    private void AttachContainerChrome(Grid container)
    {
        if (container.Children.Count > 0 && container.Children[0] is RichTextBox rtb)
        {
            rtb.TextChanged += CanvasRichTextBox_TextChanged;
            rtb.SelectionChanged += CanvasRichTextBox_SelectionChanged;
        }
        var selBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(200, 0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(-2),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        container.Children.Add(selBorder);

        Thumb MakeThumb(Cursor cursor, HorizontalAlignment hAlign, VerticalAlignment vAlign, int xDir, int yDir)
        {
            var t = new Thumb
            {
                Width = 10,
                Height = 10,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(200, 0, 120, 215)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = hAlign,
                VerticalAlignment = vAlign,
                Margin = new Thickness(-5),
                Cursor = cursor,
                Visibility = Visibility.Collapsed
            };
            t.DragDelta += (s, e) => ResizeFromCorner(container, e, xDir, yDir);
            return t;
        }

        container.Children.Add(MakeThumb(Cursors.SizeNWSE, HorizontalAlignment.Left, VerticalAlignment.Top, -1, -1));
        container.Children.Add(MakeThumb(Cursors.SizeNESW, HorizontalAlignment.Right, VerticalAlignment.Top, 1, -1));
        container.Children.Add(MakeThumb(Cursors.SizeNESW, HorizontalAlignment.Left, VerticalAlignment.Bottom, -1, 1));
        container.Children.Add(MakeThumb(Cursors.SizeNWSE, HorizontalAlignment.Right, VerticalAlignment.Bottom, 1, 1));
    }

      private Grid CreateContainer(FrameworkElement inner, double x, double y, double w, double h, bool useSnap = true)
      {
          var container = new Grid { Background = Brushes.Transparent, Width = w, Height = h };
        container.Children.Add(inner);
        if (inner is RichTextBox rtb)
        {
            rtb.TextChanged += CanvasRichTextBox_TextChanged;
            rtb.SelectionChanged += CanvasRichTextBox_SelectionChanged;
        }
        AttachContainerChrome(container);
        if (useSnap && SnapEnabled)
        {
            x = Snap(x);
            y = Snap(y);
        }
        Canvas.SetLeft(container, x);
        Canvas.SetTop(container, y);
        return container;
      }

    private void CanvasRichTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Inspector.Element == sender)
            Inspector.NotifyTextChanged();
    }

    private void CanvasRichTextBox_SelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (Inspector.Element == sender && Application.Current.MainWindow is MainWindow mw)
            mw.UpdateFontToolbarFormatting();
    }

    public Grid CreateRtbImageContainer(BitmapSource source)
    {
        double scale = Math.Min(100.0 / source.PixelWidth, 100.0 / source.PixelHeight);
        if (scale > 1)
            scale = 1;
        double width = source.PixelWidth * scale;
        double height = source.PixelHeight * scale;
        var img = new Image
        {
            Source = source,
            Width = width,
            Height = height,
            Stretch = Stretch.Uniform
        };
        var container = new Grid
        {
            Width = width,
            Height = height,
            Background = Brushes.Transparent
        };
        container.Children.Add(img);
        AttachInlineImageChrome(container);
        return container;
    }

    private void AttachInlineImageChrome(Grid container)
    {
        if (container.Children.Count == 0 || container.Children[0] is not Image)
            return;
        var selBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(200, 0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(-2),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        container.Children.Add(selBorder);
    }

    internal void AttachRichTextImages(RichTextBox rtb)
    {
        TextPointer pointer = rtb.Document.ContentStart;
        while (pointer != null && pointer.CompareTo(rtb.Document.ContentEnd) < 0)
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.EmbeddedElement &&
                pointer.GetAdjacentElement(LogicalDirection.Forward) is UIElement el &&
                pointer.Parent is InlineUIContainer iuic)
            {
                if (el is Image img)
                {
                    if (img.Source is BitmapSource bs)
                    {
                        var g = CreateRtbImageContainer(bs);
                        if (img.Width > 0)
                        {
                            g.Width = img.Width;
                            if (g.Children[0] is Image gi)
                                gi.Width = img.Width;
                        }
                        if (img.Height > 0)
                        {
                            g.Height = img.Height;
                            if (g.Children[0] is Image gi)
                                gi.Height = img.Height;
                        }
                        iuic.Child = g;
                    }
                }
                else if (el is Grid g)
                {
                    AttachInlineImageChrome(g);
                }
            }
            pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
        }
    }

    private void ResizeFromCorner(Grid container, DragDeltaEventArgs e, int xDir, int yDir)
    {
        if (_canvas == null)
            return;
        double left = Canvas.GetLeft(container);
        double top = Canvas.GetTop(container);
        double width = container.Width;
        double height = container.Height;
        double dx = e.HorizontalChange;
        double dy = e.VerticalChange;

        if (xDir < 0)
        {
            left += dx;
            width -= dx;
        }
        else
        {
            width += dx;
        }
        if (yDir < 0)
        {
            top += dy;
            height -= dy;
        }
        else
        {
            height += dy;
        }

        if (width < 10)
        {
            if (xDir < 0)
                left += width - 10;
            width = 10;
        }
        if (height < 10)
        {
            if (yDir < 0)
                top += height - 10;
            height = 10;
        }

        if (SnapEnabled)
        {
            left = Snap(left);
            top = Snap(top);
            width = Snap(width);
            height = Snap(height);
        }

        Canvas.SetLeft(container, Math.Max(0, left));
        Canvas.SetTop(container, Math.Max(0, top));
        container.Width = width;
        container.Height = height;
        if (container.Children[0] is FrameworkElement fe)
        {
            fe.Width = width;
            fe.Height = height;
        }
        if (_selected.Count == 1 && _selected[0] == container)
        {
            Inspector.SetElement((FrameworkElement)container.Children[0]);
            Inspector.RefreshPosition();
        }
    }

    private void RestoreResizeHandles()
    {
        if (_canvas == null)
            return;
        var items = _canvas.Children.Cast<UIElement>()
                    .Select(el => new { Element = el, X = Canvas.GetLeft(el), Y = Canvas.GetTop(el) })
                    .ToList();
        _canvas.Children.Clear();
        foreach (var item in items)
        {
            if (item.Element is Grid g)
            {
                if (g.Children.Count == 1 || g.Children[1] is not Border)
                    AttachContainerChrome(g);
                Canvas.SetLeft(g, item.X);
                Canvas.SetTop(g, item.Y);
                _canvas.Children.Add(g);
            }
            else if (item.Element is FrameworkElement fe)
            {
                double w = fe.Width;
                double h = fe.Height;
                var container = CreateContainer(fe, item.X, item.Y, w, h, useSnap: false);
                if (fe is Image img && img.Visibility == Visibility.Visible && img.Source != null)
                    container.Background = Brushes.White;
                if (fe.Tag is string t && !string.IsNullOrWhiteSpace(t))
                    container.Tag = t;
                _canvas.Children.Add(container);
            }
        }
        foreach (var rtb in _canvas.Children.OfType<Grid>().Select(g => g.Children[0]).OfType<RichTextBox>())
            AttachRichTextImages(rtb);
    }

    private void SelectSingle(Grid c)
    {
        ClearSelection();
        _selected.Add(c);
        UpdateSelectionVisuals();
        UpdateInspector();
    }
    private void ToggleSelection(Grid c)
    {
        if (_selected.Contains(c))
            _selected.Remove(c);
        else
            _selected.Add(c);
        UpdateSelectionVisuals();
        UpdateInspector();
    }
    public void ClearSelection()
    {
        _selected.Clear();
        if (_selectedRtbImage != null)
        {
            if (_selectedRtbImage.Children.Count > 1 && _selectedRtbImage.Children[1] is Border b)
                b.Visibility = Visibility.Collapsed;
            _selectedRtbImage = null;
        }
        UpdateSelectionVisuals();
        Inspector.SetElement(null);
        OnPropertyChanged(nameof(HasSelection));
        AlignLeftCommand.RaiseCanExecuteChanged();
        AlignCenterCommand.RaiseCanExecuteChanged();
        AlignRightCommand.RaiseCanExecuteChanged();
        AlignTopCommand.RaiseCanExecuteChanged();
        AlignMiddleCommand.RaiseCanExecuteChanged();
        AlignBottomCommand.RaiseCanExecuteChanged();
        DistributeHCommand.RaiseCanExecuteChanged();
        DistributeVCommand.RaiseCanExecuteChanged();
    }

    private void UpdateSelectionVisuals()
    {
        if (_canvas == null)
            return;
        foreach (var child in _canvas.Children.OfType<Grid>())
        {
            bool sel = _selected.Contains(child);
            if (child.Children.Count >= 2 && child.Children[1] is Border b)
                b.Visibility = sel ? Visibility.Visible : Visibility.Collapsed;
            for (int i = 2; i < child.Children.Count; i++)
                if (child.Children[i] is Thumb t)
                    t.Visibility = sel ? Visibility.Visible : Visibility.Collapsed;
        }
        OnPropertyChanged(nameof(SelectedItems));
        OnPropertyChanged(nameof(HasSelection));
        AlignLeftCommand.RaiseCanExecuteChanged();
        AlignCenterCommand.RaiseCanExecuteChanged();
        AlignRightCommand.RaiseCanExecuteChanged();
        AlignTopCommand.RaiseCanExecuteChanged();
        AlignMiddleCommand.RaiseCanExecuteChanged();
        AlignBottomCommand.RaiseCanExecuteChanged();
        DistributeHCommand.RaiseCanExecuteChanged();
        DistributeVCommand.RaiseCanExecuteChanged();
    }

    private void UpdateInspector()
    {
        if (_selected.Count == 1)
        {
            Inspector.SetElement((FrameworkElement)_selected[0].Children[0]);
            _lastControlName = Inspector.ControlName;
        }
        else
        {
            Inspector.SetElement(null);
            _lastControlName = "";
        }
    }

    private void DeleteSelected()
    {
        if (_canvas == null)
            return;
        foreach (var c in _selected.ToList())
            _canvas.Children.Remove(c);
        ClearSelection();
    }

    private void Save()
    {
        if (_canvas == null)
            return;
        var dlg = new SaveFileDialog { Filter = "Template JSON|*.json" };
        if (dlg.ShowDialog() == true)
            TemplateSerializer.SaveToJson(_canvas, dlg.FileName, CardWidth, CardHeight, _sheetColumns, _sheetRows);
    }
    private void Load()
    {
        if (_canvas == null)
            return;
        var dlg = new OpenFileDialog { Filter = "Template JSON or XAML|*.json;*.xaml" };
        if (dlg.ShowDialog() == true)
        {
            TemplateModel model = dlg.FileName.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                ? TemplateSerializer.LoadFromXaml(_canvas, dlg.FileName)
                : TemplateSerializer.LoadFromJson(_canvas, dlg.FileName);
            RestoreResizeHandles();
            ClearSelection();
            CardWidth = model.CardWidth;
            CardHeight = model.CardHeight;
            _sheetColumns = model.SheetColumns;
            _sheetRows = model.SheetRows;
        }
    }
    private void ExportXaml()
    {
        if (_canvas == null)
            return;
        var dlg = new SaveFileDialog { Filter = "XAML Canvas|*.xaml" };
        if (dlg.ShowDialog() == true)
            TemplateSerializer.ExportToXaml(_canvas, dlg.FileName, CardWidth, CardHeight, _sheetColumns, _sheetRows);
    }
    private void SaveImages()
    {
        if (_canvas == null || Cards.Count == 0)
            return;
        var folderDlg = new WinForms.FolderBrowserDialog();
        if (folderDlg.ShowDialog() != WinForms.DialogResult.OK)
            return;
        ClearSelection();
        string dir = folderDlg.SelectedPath;
        var prev = SelectedCard;
        for (int i = 0; i < Cards.Count; i++)
        {
            var card = Cards[i];
            SelectedCard = card;
            _canvas.UpdateLayout();
            var rtb = new RenderTargetBitmap((int)CardWidth, (int)CardHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(_canvas);
            BitmapEncoder encoder = _useJpeg ? new JpegBitmapEncoder() : new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            var safe = string.Join("_", card.Name.Split(IOPath.GetInvalidFileNameChars()));
            string path = IOPath.Combine(dir, $"{safe}{(_useJpeg ? ".jpg" : ".png")}");
            using var fs = new FileStream(path, FileMode.Create);
            encoder.Save(fs);
        }
        SelectedCard = prev;
        _canvas.UpdateLayout();
    }
    private void SaveSheets()
    {
        if (_canvas == null || Cards.Count == 0)
            return;
        var fileDlg = new SaveFileDialog { Filter = _useJpeg ? "JPEG Image|*.jpg;*.jpeg" : "PNG Image|*.png",
                                           FileName = "Sheet", DefaultExt = _useJpeg ? ".jpg" : ".png" };
        if (fileDlg.ShowDialog() != true)
            return;
        ClearSelection();
        int perSheet = _sheetColumns * _sheetRows;
        var images = new List<RenderTargetBitmap>();
        var prev = SelectedCard;
        foreach (var card in Cards)
        {
            SelectedCard = card;
            _canvas.UpdateLayout();
            var rtb = new RenderTargetBitmap((int)CardWidth, (int)CardHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(_canvas);
            for (int i = 0; i < Math.Max(1, card.Quantity); i++)
                images.Add(rtb);
        }
        SelectedCard = prev;
        _canvas.UpdateLayout();
        int sheetWidth = (int)(_sheetColumns * CardWidth);
        int sheetHeight = (int)(_sheetRows * CardHeight);
        int sheetCount = (images.Count + perSheet - 1) / perSheet;
        string dir = IOPath.GetDirectoryName(fileDlg.FileName)!;
        string baseName = IOPath.GetFileNameWithoutExtension(fileDlg.FileName);
        string ext = _useJpeg ? ".jpg" : ".png";
        for (int s = 0; s < sheetCount; s++)
        {

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                for (int i = 0; i < perSheet; i++)
                {
                    int idx = s * perSheet + i;
                    if (idx >= images.Count)
                        break;
                    int col = i % _sheetColumns;
                    int row = i / _sheetColumns;
                    dc.DrawImage(images[idx], new Rect(col * CardWidth, row * CardHeight, CardWidth, CardHeight));
                }
            }
            var sheetBmp = new RenderTargetBitmap(sheetWidth, sheetHeight, 96, 96, PixelFormats.Pbgra32);
            sheetBmp.Render(dv);
            BitmapEncoder encoder = _useJpeg ? new JpegBitmapEncoder() : new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(sheetBmp));
            string path = sheetCount == 1 ? fileDlg.FileName : IOPath.Combine(dir, $"{baseName}_{s + 1}{ext}");
            using var fs = new FileStream(path, FileMode.Create);
            encoder.Save(fs);
        }
    }

    public void AddCard()
    {
        if (_canvas == null)
            return;
        var card = new CardData { Name = $"Card {_cardCounter++}", Quantity = 1 };
        foreach (var obj in _canvas.Children)
        {
            if (obj is not Grid g || g.Children.Count == 0)
                continue;
            var inner = g.Children[0] as FrameworkElement;
            if (inner == null)
                continue;
            if (inner.Tag is string t && !string.IsNullOrWhiteSpace(t))
                card.Fields[t] = CreateFieldFromElement(inner);
        }
        Cards.Add(card);
        SelectedCard = card;
    }

    private void RemoveCard()
    {
        if (SelectedCard == null)
            return;
        int idx = Cards.IndexOf(SelectedCard);
        Cards.Remove(SelectedCard);
        SelectedCard = Cards.Count > 0 ? Cards[Math.Min(idx, Cards.Count - 1)] : null;
    }

    private void ApplyCardData()
    {
        if (_canvas == null || SelectedCard == null)
            return;
        foreach (var obj in _canvas.Children)
        {
            if (obj is not Grid g || g.Children.Count == 0)
                continue;
            var inner = g.Children[0] as FrameworkElement;
            if (inner == null)
                continue;
            if (inner.Tag is not string name || string.IsNullOrWhiteSpace(name))
                continue;
            if (SelectedCard.Fields.TryGetValue(name, out var field))
                ApplyFieldToElement(inner, field);
        }
        if (_selected.Count == 1)
        {
            Inspector.SetElement((FrameworkElement)_selected[0].Children[0]);
            Inspector.RefreshPosition();
        }
    }

    private CardField CreateFieldFromElement(FrameworkElement el)
    {
        var field = new CardField();
        field.Hidden = el.Visibility != Visibility.Visible;
        if (el is RichTextBox tb)
        {
            try { field.Text = XamlWriter.Save(tb.Document); } catch { field.Text = string.Empty; }
        }
        else if (el is Image img)
        {
            if (img.Source is BitmapImage bi)
                field.Source = bi.UriSource?.LocalPath ?? bi.UriSource?.ToString();
            field.Stretch = img.Stretch.ToString();
        }
        return field;
    }

    private void ApplyFieldToElement(FrameworkElement el, CardField field)
    {
        if (field.Hidden.HasValue)
        {
            el.Visibility = field.Hidden.Value ? Visibility.Hidden : Visibility.Visible;
            if (el.Parent is Grid g && el is Image img)
                g.Background = (field.Hidden.Value || img.Source == null) ? Brushes.Transparent : Brushes.White;
        }
        if (el is RichTextBox tb)
        {
            if (field.Text != null)
            {
                try { tb.Document = (FlowDocument)XamlReader.Parse(field.Text); }
                catch { tb.Document = new FlowDocument(new Paragraph(new Run(field.Text))); }
            }
        }
        else if (el is Image img)
        {
            if (field.Source != null)
            {
                try
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(field.Source, UriKind.RelativeOrAbsolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    img.Source = bi;
                }
                catch
                {
                }
            }
            if (field.Stretch != null && Enum.TryParse<Stretch>(field.Stretch, out var st))
                img.Stretch = st;
        }
    }

    private void SaveCsv()
    {
        if (_canvas == null)
            return;
        var dlg = new SaveFileDialog { Filter = "CSV|*.csv" };
        if (dlg.ShowDialog() != true)
            return;
        var controls = GetTemplateControls();
        var headers = new List<string> { "Name", "Quantity" };
        foreach (var c in controls)
        {
            if (c.type == "Text")
            {
                headers.Add($"{c.name}.Text");
                headers.Add($"{c.name}.Hidden");
            }
            else if (c.type == "Image")
            {
                headers.Add($"{c.name}.Source");
                headers.Add($"{c.name}.Hidden");
            }
        }
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(CsvEscape)));
        foreach (var card in Cards)
        {
            var values = new List<string> { CsvEscape(card.Name), CsvEscape(card.Quantity.ToString()) };
            foreach (var c in controls)
            {
                card.Fields.TryGetValue(c.name, out var field);
                if (c.type == "Text")
                {
                    values.Add(CsvEscape(field?.Text));
                    values.Add(CsvEscape((field?.Hidden ?? false).ToString()));
                }
                else if (c.type == "Image")
                {
                    values.Add(CsvEscape(field?.Source));
                    values.Add(CsvEscape((field?.Hidden ?? false).ToString()));
                }
            }
            sb.AppendLine(string.Join(",", values));
        }
        File.WriteAllText(dlg.FileName, sb.ToString());
    }

    private void LoadCsv()
    {
        if (_canvas == null)
            return;
        var dlg = new OpenFileDialog { Filter = "CSV|*.csv" };
        if (dlg.ShowDialog() != true)
            return;
        var lines = File.ReadAllLines(dlg.FileName);
        if (lines.Length == 0)
            return;
        var headers = ParseCsvLine(lines[0]);
        var columns = new List<(string control, string prop)>();
        for (int i = 2; i < headers.Count; i++)
        {
            var parts = headers[i].Split('.', 2);
            if (parts.Length == 2)
                columns.Add((parts[0], parts[1]));
        }
        Cards.Clear();
        for (int li = 1; li < lines.Length; li++)
        {
            var row = ParseCsvLine(lines[li]);
            if (row.Count == 0)
                continue;
            var card = new CardData();
            card.Name = row.Count > 0 ? row[0] : $"Card {li}";
            card.Quantity = row.Count > 1 && int.TryParse(row[1], out var q) ? q : 1;
            for (int ci = 2; ci < row.Count && ci - 2 < columns.Count; ci++)
            {
                var (control, prop) = columns[ci - 2];
                if (!card.Fields.TryGetValue(control, out var field))
                {
                    field = new CardField();
                    card.Fields[control] = field;
                }
                var val = row[ci];
                switch (prop)
                {
                case "Text":
                    field.Text = val;
                    break;
                case "Source":
                    field.Source = val;
                    break;
                case "Hidden":
                    if (bool.TryParse(val, out var hid))
                        field.Hidden = hid;
                    break;
                }
            }
            Cards.Add(card);
        }
        SelectedCard = Cards.FirstOrDefault();
    }

    private List<(string name, string type)> GetTemplateControls()
    {
        var list = new List<(string name, string type)>();
        if (_canvas == null)
            return list;
        foreach (var obj in _canvas.Children)
        {
            if (obj is not Grid g || g.Children.Count == 0)
                continue;
            var inner = g.Children[0] as FrameworkElement;
            if (inner == null)
                continue;
            if (inner.Tag is not string name || string.IsNullOrWhiteSpace(name))
                continue;
            var type = inner is RichTextBox ? "Text" : inner is Image ? "Image" : "";
            if (type != "")
                list.Add((name, type));
        }
        return list;
    }

    private string CsvEscape(string? s)
    {
        s ??= string.Empty;
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                        inQuotes = false;
                }
                else
                    sb.Append(c);
            }
            else
            {
                if (c == '"')
                    inQuotes = true;
                else if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                    sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    private void OnInspectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Inspector.Element == null)
            return;
        if (SelectedCard == null)
            return;
        var name = Inspector.ControlName;
        if (string.IsNullOrWhiteSpace(name) && e.PropertyName != nameof(SelectedElementViewModel.ControlName))
            return;
        switch (e.PropertyName)
        {
        case nameof(SelectedElementViewModel.Text):
        case nameof(SelectedElementViewModel.ImageSourcePath):
        case nameof(SelectedElementViewModel.ImageStretch):
        case nameof(SelectedElementViewModel.IsHidden):
            SelectedCard.Fields[name] = CreateFieldFromElement(Inspector.Element);
            break;
        case nameof(SelectedElementViewModel.ControlName):
            var newName = Inspector.ControlName;
            if (string.IsNullOrWhiteSpace(newName))
            {
                foreach (var c in Cards)
                    c.Fields.Remove(_lastControlName);
            }
            else
            {
                foreach (var c in Cards)
                {
                    if (c.Fields.Remove(_lastControlName, out var field))
                        c.Fields[newName] = field;
                    else if (!c.Fields.ContainsKey(newName))
                        c.Fields[newName] = CreateFieldFromElement(Inspector.Element);
                }
            }
            _lastControlName = newName;
            break;
        }
    }

    private void AlignLeft()
    {
        if (_canvas == null || _selected.Count < 2)
            return;
        double minX = _selected.Min(c => Canvas.GetLeft(c));
        foreach (var c in _selected)
            Canvas.SetLeft(c, SnapEnabled ? Snap(minX) : minX);
    }
    private void AlignCenter()
    {
        if (_canvas == null || _selected.Count < 2)
            return;
        double target = _selected.Select(c => Canvas.GetLeft(c) + c.Width / 2).Average();
        foreach (var c in _selected)
            Canvas.SetLeft(c, SnapEnabled ? Snap(target - c.Width / 2) : target - c.Width / 2);
    }
    private void AlignRight()
    {
        if (_canvas == null || _selected.Count < 2)
            return;
        double maxR = _selected.Max(c => Canvas.GetLeft(c) + c.Width);
        foreach (var c in _selected)
            Canvas.SetLeft(c, SnapEnabled ? Snap(maxR - c.Width) : maxR - c.Width);
    }
    private void AlignTop()
    {
        if (_canvas == null || _selected.Count < 2)
            return;
        double minY = _selected.Min(c => Canvas.GetTop(c));
        foreach (var c in _selected)
            Canvas.SetTop(c, SnapEnabled ? Snap(minY) : minY);
    }
    private void AlignMiddle()
    {
        if (_canvas == null || _selected.Count < 2)
            return;
        double target = _selected.Select(c => Canvas.GetTop(c) + c.Height / 2).Average();
        foreach (var c in _selected)
            Canvas.SetTop(c, SnapEnabled ? Snap(target - c.Height / 2) : target - c.Height / 2);
    }
    private void AlignBottom()
    {
        if (_canvas == null || _selected.Count < 2)
            return;
        double maxB = _selected.Max(c => Canvas.GetTop(c) + c.Height);
        foreach (var c in _selected)
            Canvas.SetTop(c, SnapEnabled ? Snap(maxB - c.Height) : maxB - c.Height);
    }

    private void DistributeH()
    {
        if (_canvas == null || _selected.Count < 3)
            return;
        var ordered = _selected.OrderBy(c => Canvas.GetLeft(c)).ToList();
        double left = Canvas.GetLeft(ordered.First());
        double right = Canvas.GetLeft(ordered.Last()) + ordered.Last().Width;
        double total = ordered.Sum(c => c.Width);
        double space = (right - left - total) / (ordered.Count - 1);
        double x = left;
        foreach (var c in ordered)
        {
            Canvas.SetLeft(c, SnapEnabled ? Snap(x) : x);
            x += c.Width + space;
        }
    }
    private void DistributeV()
    {
        if (_canvas == null || _selected.Count < 3)
            return;
        var ordered = _selected.OrderBy(c => Canvas.GetTop(c)).ToList();
        double top = Canvas.GetTop(ordered.First());
        double bottom = Canvas.GetTop(ordered.Last()) + ordered.Last().Height;
        double total = ordered.Sum(c => c.Height);
        double space = (bottom - top - total) / (ordered.Count - 1);
        double y = top;
        foreach (var c in ordered)
        {
            Canvas.SetTop(c, SnapEnabled ? Snap(y) : y);
            y += c.Height + space;
        }
    }
    private void ChangeZ(int delta)
    {
        if (_canvas == null || _selected.Count == 0)
            return;
        foreach (var c in _selected)
        {
            int current = Panel.GetZIndex(c);
            Panel.SetZIndex(c, current + delta);
        }
    }

    private void UpdateGuidelines(Grid moving)
    {
        if (_canvas == null || _guideH == null || _guideV == null)
            return;
        double x = Canvas.GetLeft(moving), y = Canvas.GetTop(moving), cx = x + moving.Width / 2,
               cy = y + moving.Height / 2, r = x + moving.Width, b = y + moving.Height;
        double tol = 5;
        bool showV = false, showH = false;
        foreach (var c in _canvas.Children.OfType<Grid>())
        {
            if (c == moving)
                continue;
            double x2 = Canvas.GetLeft(c), y2 = Canvas.GetTop(c), cx2 = x2 + c.Width / 2, cy2 = y2 + c.Height / 2,
                   r2 = x2 + c.Width, b2 = y2 + c.Height;
            if (Math.Abs(x - x2) <= tol || Math.Abs(cx - cx2) <= tol || Math.Abs(r - r2) <= tol)
            {
                double gx = Math.Abs(x - x2) <= tol ? x2 : (Math.Abs(cx - cx2) <= tol ? cx2 : r2);
                _guideV.X1 = _guideV.X2 = gx;
                _guideV.Y1 = 0;
                _guideV.Y2 = _canvas.Height > 0 ? _canvas.Height : CardHeight;
                _guideV.Visibility = Visibility.Visible;
                showV = true;
            }
            if (Math.Abs(y - y2) <= tol || Math.Abs(cy - cy2) <= tol || Math.Abs(b - b2) <= tol)
            {
                double gy = Math.Abs(y - y2) <= tol ? y2 : (Math.Abs(cy - cy2) <= tol ? cy2 : b2);
                _guideH.Y1 = _guideH.Y2 = gy;
                _guideH.X1 = 0;
                _guideH.X2 = _canvas.Width > 0 ? _canvas.Width : CardWidth;
                _guideH.Visibility = Visibility.Visible;
                showH = true;
            }
        }
        if (!showV)
            _guideV.Visibility = Visibility.Collapsed;
        if (!showH)
            _guideH.Visibility = Visibility.Collapsed;
    }

    private void HideGuides()
    {
        if (_guideH != null)
            _guideH.Visibility = Visibility.Collapsed;
        if (_guideV != null)
            _guideV.Visibility = Visibility.Collapsed;
    }

    private double Snap(double v) => SnapEnabled ? Math.Round(v / _gridSize) * _gridSize : v;
    internal static T? FindAncestor<T>(DependencyObject? from)
        where T : DependencyObject
    {
        while (from != null)
        {
            if (from is T t)
                return t;
            from = from is Visual
                ? VisualTreeHelper.GetParent(from)
                : LogicalTreeHelper.GetParent(from);
        }
        return null;
    }
    private void ShowMarquee(Point a, Point b)
    {
        if (_marquee == null)
            return;
        _marquee.Visibility = Visibility.Visible;
        Canvas.SetLeft(_marquee, Math.Min(a.X, b.X));
        Canvas.SetTop(_marquee, Math.Min(a.Y, b.Y));
        _marquee.Width = Math.Abs(a.X - b.X);
        _marquee.Height = Math.Abs(a.Y - b.Y);
    }
    private void UpdateMarqueeSelection()
    {
        if (_canvas == null || _marquee == null)
            return;

        Rect r = new Rect(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee), _marquee.Width, _marquee.Height);

        foreach (var c in _canvas.Children.OfType<Grid>())
        {
            // No need to skip _marquee; it's a Rectangle, not a Grid
            Rect cr = new Rect(Canvas.GetLeft(c), Canvas.GetTop(c), c.Width, c.Height);
            if (r.IntersectsWith(cr) || r.Contains(cr))
            {
                if (!_selected.Contains(c))
                    _selected.Add(c);
            }
        }

        UpdateSelectionVisuals();
    }

    private bool IsAdditiveSelect() => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                                       Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class NullToBoolInverseConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture) => value != null;
    public object ConvertBack(object value, Type targetType, object parameter,
                              System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}
}
