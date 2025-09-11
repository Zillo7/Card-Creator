using CardCreator.Models;
using CardCreator.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CardCreator
{
    public partial class MainWindow : Window
    {
        public MainViewModel VM => (MainViewModel)DataContext;
        public MainWindow() { InitializeComponent(); Resources["NullToBoolInverse"] = new NullToBoolInverseConverter(); VM.AttachCanvas(CardCanvas, GuideH, GuideV, Marquee); }
        private void CardCanvas_MouseLeftButtonDown(object s, MouseButtonEventArgs e) => VM.OnCanvasMouseLeftDown(e);
        private void CardCanvas_MouseMove(object s, MouseEventArgs e) => VM.OnCanvasMouseMove(e);
        private void CardCanvas_MouseLeftButtonUp(object s, MouseButtonEventArgs e) => VM.OnCanvasMouseLeftUp(e);
        private void BrowseImage_Click(object s, RoutedEventArgs e) { if (!VM.HasSelection || VM.SingleSelectedInner is not Image) return; var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*" }; if (dlg.ShowDialog() == true) VM.Inspector.ImageSourcePath = dlg.FileName; }
        private void Window_KeyDown(object s, KeyEventArgs e) => VM.OnKeyDown(e);
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private Canvas? _canvas; private Line? _guideH, _guideV; private Rectangle? _marquee;
        private Point? _dragStart; private Vector _dragOffset; private Grid? _draggingContainer;
        private bool _draggingMarquee; private Point _marqueeStart;

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
        public RelayCommand ChangeCardSizeCommand { get; }

        public SelectedElementViewModel Inspector { get; } = new();
        private readonly List<Grid> _selected = new();
        private double _cardWidth = 750; public double CardWidth { get => _cardWidth; set { _cardWidth = value; OnPropertyChanged(); } }
        private double _cardHeight = 1050; public double CardHeight { get => _cardHeight; set { _cardHeight = value; OnPropertyChanged(); } }
        public IEnumerable<Grid> SelectedItems => _selected;
        public FrameworkElement? SingleSelectedInner => _selected.Count == 1 ? (FrameworkElement?)_selected[0].Children[0] : null;
        public bool HasSelection => _selected.Count > 0;

        private bool _snapEnabled = true; public bool SnapEnabled { get => _snapEnabled; set { _snapEnabled = value; OnPropertyChanged(); } }
        private int _gridSize = 10; public int GridSize { get => _gridSize; set { _gridSize = value; OnPropertyChanged(); } }
        private bool _guidelinesEnabled = true; public bool GuidelinesEnabled { get => _guidelinesEnabled; set { _guidelinesEnabled = value; OnPropertyChanged(); } }

        public MainViewModel()
        {
            AddTextCommand = new RelayCommand(_ => AddText());
            AddImageCommand = new RelayCommand(_ => AddImage());
            SaveCommand = new RelayCommand(_ => Save());
            LoadCommand = new RelayCommand(_ => Load());
            ExportXamlCommand = new RelayCommand(_ => ExportXaml());
            AlignLeftCommand = new RelayCommand(_ => AlignLeft(), _ => _selected.Count >= 2);
            AlignCenterCommand = new RelayCommand(_ => AlignCenter(), _ => _selected.Count >= 2);
            AlignRightCommand = new RelayCommand(_ => AlignRight(), _ => _selected.Count >= 2);
            AlignTopCommand = new RelayCommand(_ => AlignTop(), _ => _selected.Count >= 2);
            AlignMiddleCommand = new RelayCommand(_ => AlignMiddle(), _ => _selected.Count >= 2);
            AlignBottomCommand = new RelayCommand(_ => AlignBottom(), _ => _selected.Count >= 2);
            DistributeHCommand = new RelayCommand(_ => DistributeH(), _ => _selected.Count >= 3);
            DistributeVCommand = new RelayCommand(_ => DistributeV(), _ => _selected.Count >= 3);
            BringForwardCommand = new RelayCommand(_ => ChangeZ(1), _ => _selected.Count >= 1);
            SendBackwardCommand = new RelayCommand(_ => ChangeZ(-1), _ => _selected.Count >= 1);
            ChangeCardSizeCommand = new RelayCommand(_ => ChangeCardSize());
        }

        public void AttachCanvas(Canvas canvas, Line guideH, Line guideV, Rectangle marquee) { _canvas = canvas; _guideH = guideH; _guideV = guideV; _marquee = marquee; }

        public void OnCanvasMouseLeftDown(MouseButtonEventArgs e)
        {
            if (_canvas == null) return;
            var pos = e.GetPosition(_canvas);
            if (e.OriginalSource == _canvas)
            {
                _draggingMarquee = true; _marqueeStart = pos; ShowMarquee(pos, pos);
                if (!IsAdditiveSelect()) ClearSelection();
                return;
            }
            var container = FindAncestor<Grid>(e.OriginalSource as DependencyObject);
            if (container != null && _canvas.Children.Contains(container))
            {
                if (IsAdditiveSelect()) ToggleSelection(container);
                else { if (!_selected.Contains(container)) { ClearSelection(); _selected.Add(container); UpdateSelectionVisuals(); } }
                _draggingContainer = container; _dragStart = pos; _dragOffset = new Vector(Canvas.GetLeft(container), Canvas.GetTop(container));
                container.CaptureMouse(); e.Handled = true;
            }
        }

        public void OnCanvasMouseMove(MouseEventArgs e)
        {
            if (_canvas == null) return;
            var pos = e.GetPosition(_canvas);
            if (_draggingMarquee && _marquee != null) { ShowMarquee(_marqueeStart, pos); UpdateMarqueeSelection(); return; }
            if (_dragStart.HasValue && _draggingContainer != null && _draggingContainer.IsMouseCaptured)
            {
                var delta = pos - _dragStart.Value; var nx = _dragOffset.X + delta.X; var ny = _dragOffset.Y + delta.Y;
                if (SnapEnabled) { nx = Snap(nx); ny = Snap(ny); }
                Canvas.SetLeft(_draggingContainer, Math.Max(0, nx)); Canvas.SetTop(_draggingContainer, Math.Max(0, ny));
                if (GuidelinesEnabled) UpdateGuidelines(_draggingContainer);
                if (_selected.Count == 1 && _selected[0] == _draggingContainer)
                    Inspector.RefreshPosition();
            }
        }

        public void OnCanvasMouseLeftUp(MouseButtonEventArgs e)
        {
            if (_draggingContainer != null) { _draggingContainer.ReleaseMouseCapture(); _draggingContainer = null; }
            _dragStart = null; HideGuides();
            if (_draggingMarquee) { _draggingMarquee = false; if (_marquee != null) _marquee.Visibility = Visibility.Collapsed; }
            UpdateInspector();
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (_canvas == null || _selected.Count == 0) return;
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
                    case Key.Left: x -= step; handled = true; break;
                    case Key.Right: x += step; handled = true; break;
                    case Key.Up: y -= step; handled = true; break;
                    case Key.Down: y += step; handled = true; break;
                }
                if (handled)
                {
                    if (SnapEnabled) { x = Snap(x); y = Snap(y); }
                    Canvas.SetLeft(c, Math.Max(0, x)); Canvas.SetTop(c, Math.Max(0, y));
                }
            }
            if (handled) { e.Handled = true; UpdateInspector(); }
        }

        public void AddText()
        {
            if (_canvas == null) return;
            var tb = new TextBlock { Text = "Text", FontSize = 28, Foreground = Brushes.Black, RenderTransformOrigin = new Point(0.5, 0.5), TextWrapping = TextWrapping.Wrap };
            var container = CreateContainer(tb, 60, 60, 180, 60);
            tb.HorizontalAlignment = HorizontalAlignment.Stretch; tb.VerticalAlignment = VerticalAlignment.Stretch;
            _canvas.Children.Add(container); SelectSingle(container);
        }
        public void AddImage()
        {
            if (_canvas == null) return;
            var img = new Image { Stretch = Stretch.Uniform, RenderTransformOrigin = new Point(0.5, 0.5) };
            var container = CreateContainer(img, 100, 100, 200, 140);
            container.Background = Brushes.White;
            _canvas.Children.Add(container); SelectSingle(container);
        }

        private Grid CreateContainer(FrameworkElement inner, double x, double y, double w, double h)
        {
            var container = new Grid { Background = Brushes.Transparent, Width = w, Height = h };
            container.Children.Add(inner);
            var selBorder = new Border { BorderBrush = new SolidColorBrush(Color.FromArgb(200, 0, 120, 215)), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(4), Margin = new Thickness(-2), IsHitTestVisible = false, Visibility = Visibility.Collapsed };
            container.Children.Add(selBorder);
            Canvas.SetLeft(container, SnapEnabled ? Snap(x) : x); Canvas.SetTop(container, SnapEnabled ? Snap(y) : y);
            return container;
        }

        private void SelectSingle(Grid c) { ClearSelection(); _selected.Add(c); UpdateSelectionVisuals(); UpdateInspector(); }
        private void ToggleSelection(Grid c) { if (_selected.Contains(c)) _selected.Remove(c); else _selected.Add(c); UpdateSelectionVisuals(); UpdateInspector(); }
        private void ClearSelection()
        {
            _selected.Clear(); UpdateSelectionVisuals(); Inspector.SetElement(null);
            OnPropertyChanged(nameof(HasSelection));
            AlignLeftCommand.RaiseCanExecuteChanged(); AlignCenterCommand.RaiseCanExecuteChanged(); AlignRightCommand.RaiseCanExecuteChanged();
            AlignTopCommand.RaiseCanExecuteChanged(); AlignMiddleCommand.RaiseCanExecuteChanged(); AlignBottomCommand.RaiseCanExecuteChanged();
            DistributeHCommand.RaiseCanExecuteChanged(); DistributeVCommand.RaiseCanExecuteChanged();
        }

        private void UpdateSelectionVisuals()
        {
            if (_canvas == null) return;
            foreach (var child in _canvas.Children.OfType<Grid>())
            {
                if (child.Children.Count >= 2 && child.Children[1] is Border b) b.Visibility = _selected.Contains(child) ? Visibility.Visible : Visibility.Collapsed;
            }
            OnPropertyChanged(nameof(SelectedItems)); OnPropertyChanged(nameof(HasSelection));
            AlignLeftCommand.RaiseCanExecuteChanged(); AlignCenterCommand.RaiseCanExecuteChanged(); AlignRightCommand.RaiseCanExecuteChanged();
            AlignTopCommand.RaiseCanExecuteChanged(); AlignMiddleCommand.RaiseCanExecuteChanged(); AlignBottomCommand.RaiseCanExecuteChanged();
            DistributeHCommand.RaiseCanExecuteChanged(); DistributeVCommand.RaiseCanExecuteChanged();
        }

        private void UpdateInspector() { if (_selected.Count == 1) Inspector.SetElement((FrameworkElement)_selected[0].Children[0]); else Inspector.SetElement(null); }

        private void DeleteSelected() { if (_canvas == null) return; foreach (var c in _selected.ToList()) _canvas.Children.Remove(c); ClearSelection(); }

        private void Save() { if (_canvas == null) return; var dlg = new SaveFileDialog { Filter = "Template JSON|*.json" }; if (dlg.ShowDialog() == true) TemplateSerializer.SaveToJson(_canvas, dlg.FileName, CardWidth, CardHeight); }
        private void Load() { if (_canvas == null) return; var dlg = new OpenFileDialog { Filter = "Template JSON|*.json" }; if (dlg.ShowDialog() == true) { var model = TemplateSerializer.LoadFromJson(_canvas, dlg.FileName); ClearSelection(); CardWidth = model.CardWidth; CardHeight = model.CardHeight; } }
        private void ExportXaml() { if (_canvas == null) return; var dlg = new SaveFileDialog { Filter = "XAML Canvas|*.xaml" }; if (dlg.ShowDialog() == true) TemplateSerializer.ExportToXaml(_canvas, dlg.FileName, CardWidth, CardHeight); }

        private void AlignLeft() { if (_canvas == null || _selected.Count < 2) return; double minX = _selected.Min(c => Canvas.GetLeft(c)); foreach (var c in _selected) Canvas.SetLeft(c, SnapEnabled ? Snap(minX) : minX); }
        private void AlignCenter() { if (_canvas == null || _selected.Count < 2) return; double target = _selected.Select(c => Canvas.GetLeft(c) + c.Width / 2).Average(); foreach (var c in _selected) Canvas.SetLeft(c, SnapEnabled ? Snap(target - c.Width / 2) : target - c.Width / 2); }
        private void AlignRight() { if (_canvas == null || _selected.Count < 2) return; double maxR = _selected.Max(c => Canvas.GetLeft(c) + c.Width); foreach (var c in _selected) Canvas.SetLeft(c, SnapEnabled ? Snap(maxR - c.Width) : maxR - c.Width); }
        private void AlignTop() { if (_canvas == null || _selected.Count < 2) return; double minY = _selected.Min(c => Canvas.GetTop(c)); foreach (var c in _selected) Canvas.SetTop(c, SnapEnabled ? Snap(minY) : minY); }
        private void AlignMiddle() { if (_canvas == null || _selected.Count < 2) return; double target = _selected.Select(c => Canvas.GetTop(c) + c.Height / 2).Average(); foreach (var c in _selected) Canvas.SetTop(c, SnapEnabled ? Snap(target - c.Height / 2) : target - c.Height / 2); }
        private void AlignBottom() { if (_canvas == null || _selected.Count < 2) return; double maxB = _selected.Max(c => Canvas.GetTop(c) + c.Height); foreach (var c in _selected) Canvas.SetTop(c, SnapEnabled ? Snap(maxB - c.Height) : maxB - c.Height); }

        private void DistributeH()
        {
            if (_canvas == null || _selected.Count < 3) return; var ordered = _selected.OrderBy(c => Canvas.GetLeft(c)).ToList();
            double left = Canvas.GetLeft(ordered.First()); double right = Canvas.GetLeft(ordered.Last()) + ordered.Last().Width;
            double total = ordered.Sum(c => c.Width); double space = (right - left - total) / (ordered.Count - 1); double x = left;
            foreach (var c in ordered) { Canvas.SetLeft(c, SnapEnabled ? Snap(x) : x); x += c.Width + space; }
        }
        private void DistributeV()
        {
            if (_canvas == null || _selected.Count < 3) return; var ordered = _selected.OrderBy(c => Canvas.GetTop(c)).ToList();
            double top = Canvas.GetTop(ordered.First()); double bottom = Canvas.GetTop(ordered.Last()) + ordered.Last().Height;
            double total = ordered.Sum(c => c.Height); double space = (bottom - top - total) / (ordered.Count - 1); double y = top;
            foreach (var c in ordered) { Canvas.SetTop(c, SnapEnabled ? Snap(y) : y); y += c.Height + space; }
        }
        private void ChangeZ(int delta) { if (_canvas == null || _selected.Count == 0) return; foreach (var c in _selected) { int current = Panel.GetZIndex(c); Panel.SetZIndex(c, current + delta); } }

        private void UpdateGuidelines(Grid moving)
        {
            if (_canvas == null || _guideH == null || _guideV == null) return;
            double x = Canvas.GetLeft(moving), y = Canvas.GetTop(moving), cx = x + moving.Width / 2, cy = y + moving.Height / 2, r = x + moving.Width, b = y + moving.Height;
            double tol = 5; bool showV = false, showH = false;
            foreach (var c in _canvas.Children.OfType<Grid>())
            {
                if (c == moving) continue;
                double x2 = Canvas.GetLeft(c), y2 = Canvas.GetTop(c), cx2 = x2 + c.Width / 2, cy2 = y2 + c.Height / 2, r2 = x2 + c.Width, b2 = y2 + c.Height;
                if (Math.Abs(x - x2) <= tol || Math.Abs(cx - cx2) <= tol || Math.Abs(r - r2) <= tol)
                {
                    double gx = Math.Abs(x - x2) <= tol ? x2 : (Math.Abs(cx - cx2) <= tol ? cx2 : r2);
                    _guideV.X1 = _guideV.X2 = gx; _guideV.Y1 = 0; _guideV.Y2 = _canvas.Height > 0 ? _canvas.Height : CardHeight; _guideV.Visibility = Visibility.Visible; showV = true;
                }
                if (Math.Abs(y - y2) <= tol || Math.Abs(cy - cy2) <= tol || Math.Abs(b - b2) <= tol)
                {
                    double gy = Math.Abs(y - y2) <= tol ? y2 : (Math.Abs(cy - cy2) <= tol ? cy2 : b2);
                    _guideH.Y1 = _guideH.Y2 = gy; _guideH.X1 = 0; _guideH.X2 = _canvas.Width > 0 ? _canvas.Width : CardWidth; _guideH.Visibility = Visibility.Visible; showH = true;
                }
            }
            if (!showV) _guideV.Visibility = Visibility.Collapsed; if (!showH) _guideH.Visibility = Visibility.Collapsed;
        }

        private void ChangeCardSize()
        {
            var dlg = new CanvasSizeDialog(CardWidth, CardHeight) { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() == true)
            {
                CardWidth = dlg.VM.WidthDip;
                CardHeight = dlg.VM.HeightDip;
            }
        }
        private void HideGuides() { if (_guideH != null) _guideH.Visibility = Visibility.Collapsed; if (_guideV != null) _guideV.Visibility = Visibility.Collapsed; }

        private double Snap(double v) => SnapEnabled ? Math.Round(v / _gridSize) * _gridSize : v;
        private static T? FindAncestor<T>(DependencyObject? from) where T : DependencyObject { while (from != null) { if (from is T t) return t; from = VisualTreeHelper.GetParent(from); } return null; }
        private void ShowMarquee(Point a, Point b) { if (_marquee == null) return; _marquee.Visibility = Visibility.Visible; Canvas.SetLeft(_marquee, Math.Min(a.X, b.X)); Canvas.SetTop(_marquee, Math.Min(a.Y, b.Y)); _marquee.Width = Math.Abs(a.X - b.X); _marquee.Height = Math.Abs(a.Y - b.Y); }
        private void UpdateMarqueeSelection()
        {
            if (_canvas == null || _marquee == null) return;

            Rect r = new Rect(
                Canvas.GetLeft(_marquee),
                Canvas.GetTop(_marquee),
                _marquee.Width,
                _marquee.Height
            );

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

        private bool IsAdditiveSelect() => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class NullToBoolInverseConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value != null;
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
