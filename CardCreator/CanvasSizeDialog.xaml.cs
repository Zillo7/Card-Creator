using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CardCreator;

public partial class CanvasSizeDialog : Window
{
    public CanvasSizeDialog(double widthDip, double heightDip)
    {
        InitializeComponent();
        VM = new CanvasSizeViewModel(widthDip, heightDip);
        DataContext = VM;
    }

    public CanvasSizeViewModel VM { get; }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}

public class CanvasSizeViewModel : INotifyPropertyChanged
{
    private double _widthDip;
    private double _heightDip;
    private bool _updatingPreset;

    public CanvasSizeViewModel(double widthDip, double heightDip)
    {
        _widthDip = widthDip;
        _heightDip = heightDip;
        Presets = new ObservableCollection<CardSize>
        {
            new("Poker", 2.5, 3.5),
            new("Bridge", 2.25, 3.5),
            new("Tarot", 2.75, 4.75),
            new("Jumbo", 3.5, 5)
        };
    }

    public ObservableCollection<CardSize> Presets { get; }

    private CardSize? _selectedPreset;
    public CardSize? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (_selectedPreset != value)
            {
                _selectedPreset = value;
                OnPropertyChanged();
                if (value != null)
                {
                    _updatingPreset = true;
                    WidthInch = value.WidthInch;
                    HeightInch = value.HeightInch;
                    _updatingPreset = false;
                }
            }
        }
    }

    public double WidthDip
    {
        get => _widthDip;
        set
        {
            if (_widthDip != value)
            {
                _widthDip = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WidthInch));
                if (!_updatingPreset)
                    SelectedPreset = null;
            }
        }
    }

    public double HeightDip
    {
        get => _heightDip;
        set
        {
            if (_heightDip != value)
            {
                _heightDip = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeightInch));
                if (!_updatingPreset)
                    SelectedPreset = null;
            }
        }
    }

    public double WidthInch
    {
        get => _widthDip / 96.0;
        set { WidthDip = value * 96.0; }
    }
    public double HeightInch
    {
        get => _heightDip / 96.0;
        set { HeightDip = value * 96.0; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public record CardSize(string Name, double WidthInch, double HeightInch);
