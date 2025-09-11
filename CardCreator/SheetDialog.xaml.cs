using System.Windows;

namespace CardCreator;

public partial class SheetDialog : Window
{
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public SheetDialog(int cols, int rows)
    {
        InitializeComponent();
        ColumnsBox.Text = cols.ToString();
        RowsBox.Text = rows.ToString();
    }
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ColumnsBox.Text, out var cols) || cols <= 0) { MessageBox.Show("Invalid columns"); return; }
        if (!int.TryParse(RowsBox.Text, out var rows) || rows <= 0) { MessageBox.Show("Invalid rows"); return; }
        Columns = cols; Rows = rows; DialogResult = true;
    }
}
