// ReSharper disable MemberCanBeProtected.Global
using System.Windows.Input;
using Serilog;

namespace KinoshitaProductions.Emvvm.Base
{
    public abstract class Command<TViewModel> : CommandBase<TViewModel>, ICommand where TViewModel : ObservableViewModel
    {
        protected Command(TViewModel? forViewModel) : base(forViewModel)
        {
            ViewModel = forViewModel;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return CanExecute();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed while checking if command could be executed");
            }
            return false;
        }
        
        protected virtual bool CanExecute()
        {
            if (ViewModel == null) return false;
            return !ViewModel.IsBusy && !IsAsyncLock;
        }
        
        public void Execute(object? parameter)
        {
            if (ViewModel == null) return;
            ViewModel.IsBusy = true;
            try
            {
                Execute();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute command");
            }
            if (!IsAsyncLock || !IsViewModelLock)
                ViewModel.IsBusy = false;
        }

        protected virtual void Execute() { }
    }
    
    public abstract class Command<TViewModel, TParameter> : CommandBase<TViewModel>, ICommand where TViewModel : ObservableViewModel
    {
        protected Command(TViewModel? forViewModel) : base(forViewModel)
        {
            ViewModel = forViewModel;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return CanExecute((TParameter?)parameter);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed while checking if command could be executed");
            }
            return false;
        }
        
        protected virtual bool CanExecute(TParameter? parameter)
        {
            if (ViewModel == null) return false;
            return !ViewModel.IsBusy && !IsAsyncLock;
        }
        
        public void Execute(object? parameter)
        {
            if (ViewModel == null) return;
            ViewModel.IsBusy = true;
            try
            {
                Execute((TParameter?)parameter);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute command");
            }
            if (!IsAsyncLock || !IsViewModelLock)
                ViewModel.IsBusy = false;
        }

        protected virtual void Execute(TParameter? parameter) { }
    }
}