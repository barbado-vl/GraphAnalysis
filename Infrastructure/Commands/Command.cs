using System;
using System.Windows.Input;

namespace GraphAnalysis.Infrastructure.Commands
{
    internal class Command : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public Command(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(Execute));
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);   // другой вариант с той же логикой CanExecute?.Invoke(parametr) && true;
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }
}