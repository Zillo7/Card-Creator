using System;
using System.Windows.Input;
namespace CardCreator {
  public class RelayCommand : ICommand {
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute=null){ _execute=execute; _canExecute=canExecute; }
    public bool CanExecute(object? p)=>_canExecute?.Invoke(p)??true;
    public void Execute(object? p)=>_execute(p);
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged()=>CanExecuteChanged?.Invoke(this, EventArgs.Empty);
  }
}
