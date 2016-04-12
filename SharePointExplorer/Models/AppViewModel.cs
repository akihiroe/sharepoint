using SharePointExplorer.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ViewMaker;
using ViewMaker.Core;
using ViewMaker.Core.Wpf;

namespace SharePointExplorer.Models
{
    public class AppViewModel : ViewModelBase
    {
        public AppViewModel TopViewModel { get { return AppViewModel.TopViewModelInstance; } }

        public static AppViewModel TopViewModelInstance { get; set; }

        public AppViewModel()
        {
        }


        /// <summary>
        /// 処理中
        /// </summary>
        public virtual bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; OnPropertyChanged("IsBusy", "IsEnabled");  }
        }
        private bool _isBusy;

        /// <summary>
        /// キャンセル中
        /// </summary>
        public virtual bool IsCancelled
        {
            get { return _isCanceled; }
            set { _isCanceled = value; OnPropertyChanged("IsCanceled");  }
        }
        private bool _isCanceled;

        /// <summary>
        /// キャンセル可能か
        /// </summary>
        public virtual bool CanCanceled
        {
            get { return _canCanceled; }
            set { _canCanceled = value; OnPropertyChanged("CanCanceled");  }
        }
        private bool _canCanceled;

        public virtual string CancelConfirmMessage
        {
            get { return _cancelConfirmMessage; }
            set { _cancelConfirmMessage = value; OnPropertyChanged("CancelConfirmMessage"); }
        }
        private string _cancelConfirmMessage;

        ///// <summary>
        ///// 処理進捗
        ///// </summary>
        //public int Progress
        //{
        //    get { return _progress; }
        //    set { _progress = value; OnPropertyChanged("Progess"); }
        //}
        //private int _progress;

        /// <summary>
        /// 処理中メッセージ
        /// </summary>
        public string ProgressMessage
        {
            get { return _progressMessage; }
            set { _progressMessage = value; OnPropertyChanged("ProgressMessage"); }
        }
        private string _progressMessage;

        /// <summary>
        /// 処理可能か
        /// </summary>
        public bool IsEnabled
        {
            get { return !IsBusy; }
        }

        /// <summary>
        /// 処理結果メッセージ
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged("Message"); }
        }
        private string _message;



        protected void NotifyProgressMessage(string message)
        {
            ExecuteUIProc(()=>{
                ProgressMessage = message;
                TopViewModel.ProgressMessage = message;
            });
        }

        protected virtual void ShowDialog(ViewModelBase vm, string title = null, ResizeMode resize = ResizeMode.NoResize)
        {
            var view = (WpfWindowView)ViewUtil.BuildView(vm, true);
            if (title != null) view.Window.Title = title;
            view.Window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var active = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            if (active != null) view.Window.Owner = active;
            view.Window.ResizeMode = resize;
            view.ShowDialog();
        }

        protected virtual bool Confirm(string title, string message)
        {
            if (Application.Current == null) return true; 
            var active = Application.Current.Windows.OfType<Window>().SingleOrDefault(y => y.IsActive);
            if (active == null)
            {
                return MessageBox.Show(message, title, MessageBoxButton.OKCancel) == MessageBoxResult.OK;
            }
            else
            {
                return MessageBox.Show(active, message, title, MessageBoxButton.OKCancel) == MessageBoxResult.OK;
            }
        }

        protected virtual void ShowMessage(string message, string caption)
        {
            Debug.WriteLine("MessageBox:" + message);
            if (Application.Current == null) return;
            var active = Application.Current.Windows.OfType<Window>().SingleOrDefault(y => y.IsActive);
            if (active == null)
            {
                MessageBox.Show( message, caption);
            }
            else
            {
                MessageBox.Show(active, message, caption);
            }
        }
        /// <summary>
        /// テスト用に非同期処理を同期的に行うように動作
        /// </summary>
        public static bool ExecuteActionAsyncMode = true;


        /// <summary>
        /// アクションを実行するテンプレートメソッド
        /// </summary>
        /// <param name="task"></param>
        /// <param name="callback"></param>
        /// <param name="message"></param>
        /// <param name="canCancel"></param>
        /// <param name="isBusy"></param>
        protected void ExecuteActionAsync(Task task, Action<Task> callback = null, string message = null, bool canCancel = false, bool isBusy = true, string cancelConfirmMessage = null)
        {
            SetBusy(message,isBusy, canCancel, cancelConfirmMessage);


            if (ExecuteActionAsyncMode)
            {
                var uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
                task.ContinueWith((x) =>
                {
                    uiFactory.StartNew(() =>
                    {
                        if (callback != null) callback(x);
                        ResetBusy();
                        if (x.IsFaulted)
                        {
                            Trace.WriteLine(x.Exception.ToString());
                            if (x.Exception.InnerException != null)
                            {
                                Message = x.Exception.InnerException.Message;
                            }
                            else
                            {
                                Message = x.Exception.Message;
                            }

                            ShowMessage(Message, "Error");
                        }
                        //else if(x.Status == TaskStatus.Canceled)
                        //{
                        //    this.ShowMessage(Resources.MsgCancelled);
                        //}
                        else
                        {
                            NotifyProgressMessage("");
                        }

                    });
                });
            }
            else
            {
                try
                {
                    task.Wait();
                    NotifyProgressMessage("");
                    Message = "";
                    ResetBusy();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                    ResetBusy();
                    if (ex.InnerException != null)
                    {
                        Message = ex.InnerException.Message;
                    }
                    else
                    {
                        Message = ex.Message;
                    }
                }
            }


        }

        public void ResetBusy()
        {
            IsBusy = false;
        }

        public void SetBusy(string message, bool isBusy, bool canCancel, string cancelConfirmMessage)
        {
            NotifyProgressMessage(message ?? Resources.MsgProcessing);
            CanCanceled = canCancel;
            IsCancelled = false;
            IsBusy = isBusy;
            CancelConfirmMessage = cancelConfirmMessage;

        }

        protected override void OnCommandExecuted(ICommand command, Exception error)
        {
            base.OnCommandExecuted(command, null);
            Trace.WriteLine(error);
            if (error != null) ShowMessage(error.Message, "ERROR");
        }


        private object syncObject = new object();

        protected void ExecuteUIProc(Action action)
        {
            if (Application.Current == null || Application.Current.Dispatcher.CheckAccess())
            {
                lock (syncObject)
                {
                    action();
                }
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    action();
                }));
            }

        }

        public void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrames), frame);
            Dispatcher.PushFrame(frame);
        }

        public object ExitFrames(object f)
        {
            ((DispatcherFrame)f).Continue = false;

            return null;
        }

        protected virtual string ShowFolderDailog()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return null;
            }
            else
            {
                return dialog.SelectedPath;
            }

        }
    }
}
