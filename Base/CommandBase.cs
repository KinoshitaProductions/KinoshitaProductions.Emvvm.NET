// ReSharper disable MemberCanBeProtected.Global

namespace KinoshitaProductions.Emvvm.Base
{
    public abstract class CommandBase<TViewModel> : IDisposable where TViewModel : ObservableViewModel
    {
        protected bool IsAsyncLock;
        protected bool IsViewModelLock;
        // ReSharper disable once MemberCanBePrivate.Global
        protected TViewModel? ViewModel;
        public event EventHandler? CanExecuteChanged;

        protected CommandBase(TViewModel? forViewModel)
        {
            ViewModel = forViewModel;
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected bool IsDisposed;
        public  void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            
            // get rid of managed resources
            ViewModel = null;

            // get rid of unmanaged resources
            
            IsDisposed = true;
        }
        // this must be called for async commands
        protected void NotifyAsyncExecutionStarted(bool lockWholeViewModel = false)
        {
            if (ViewModel == null) return;
            IsAsyncLock = true;
            if (lockWholeViewModel) ViewModel.IsBusy = IsViewModelLock = true;
        }
        protected void NotifyAsyncExecutionCompleted()
        {
            if (ViewModel == null) return;
            IsAsyncLock = false;
            ViewModel.IsBusy = IsViewModelLock = false;
        }
    }
}