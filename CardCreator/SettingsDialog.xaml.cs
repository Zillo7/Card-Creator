using System.Windows;

namespace CardCreator;

public partial class SettingsDialog : Window
{
    public CanvasSizeViewModel VM { get; }
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public bool UseJpeg { get; private set; }

    public SettingsDialog(double widthDip, double heightDip, int cols, int rows, bool useJpeg)
    {
        InitializeComponent();
        VM = new CanvasSizeViewModel(widthDip, heightDip);
        ColumnsBox.Text = cols.ToString();
        RowsBox.Text = rows.ToString();
        FormatBox.SelectedIndex = useJpeg ? 1 : 0;
        DataContext = this;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ColumnsBox.Text, out var cols) || cols <= 0)
        {
            MessageBox.Show("Invalid columns");
            return;
        }
        if (!int.TryParse(RowsBox.Text, out var rows) || rows <= 0)
        {
            MessageBox.Show("Invalid rows");
            return;
        }
        Columns = cols;
        Rows = rows;
        UseJpeg = FormatBox.SelectedIndex == 1;
        DialogResult = true;
    }
}
