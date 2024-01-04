using System.Windows.Input;

namespace StepRecorder.ViewModel
{
    internal class ApplicationBaseViewModel
    {
    }

    internal class RelayCommand(Predicate<object?>? isExec, Action<object?> exec) : ICommand
    {
        public RelayCommand(Action<object?> exec) : this(null, exec) { }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return isExec == null || isExec(parameter);
        }

        public void Execute(object? parameter)
        {
            exec(parameter);
        }
    }
}
