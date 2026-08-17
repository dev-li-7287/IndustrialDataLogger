using System.Windows.Input;

namespace IndustrialDataLogger
{

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExcute;

        public RelayCommand(Action execute, Func<bool> canExcute = null)
        {
            _execute = execute;
            _canExcute = canExcute;
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => _canExcute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();

    }
}