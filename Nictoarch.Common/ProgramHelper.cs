using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using NLog;

namespace Nictoarch.Common
{
    public static class ProgramHelper
    {
        public delegate void FatalExceptionCallback(FatalExceptionArgs args);

        public static event FatalExceptionCallback? OnFatalException;
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

        private static void SetupHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                if (e.IsTerminating)
                {
                    s_logger.Error("Application is terminating due to an unhandled exception in a secondary thread.");
                };
                HandleFatalException((Exception)e.ExceptionObject, ExceptionSource.AppDomain_CurrentDomain_UnhandledException);
            };

            TaskScheduler.UnobservedTaskException += (sender, e) => {
                HandleFatalException(e.Exception, ExceptionSource.TaskScheduler_UnobservedTaskException);
            };
        }

        public static void MainWrapper(Action mainAction)
        {
            SetupHandlers();

            if (!Debugger.IsAttached)
            {
                try
                {
                    mainAction.Invoke();
                }
                catch (Exception e)
                {
                    HandleFatalException(e, ExceptionSource.MainAction_Exception);
                    throw;
                }
            }
            else
            {
                mainAction.Invoke();
            }
        }

        public static async Task MainWrapperAsync(Func<Task> mainAction)
        {
            SetupHandlers();

            if (!Debugger.IsAttached)
            {
                try
                {
                    await mainAction.Invoke();
                }
                catch (Exception e)
                {
                    HandleFatalException(e, ExceptionSource.MainAction_Exception);
                    throw;
                }
            }
            else
            {
                await mainAction.Invoke();
            }
        }

        public static async Task<int> MainWrapperAsync(Func<Task<int>> mainAction)
        {
            SetupHandlers();

            if (!Debugger.IsAttached)
            {
                try
                {
                    return await mainAction.Invoke();
                }
                catch (Exception e)
                {
                    HandleFatalException(e, ExceptionSource.MainAction_Exception);
                    throw;
                }
            }
            else
            {
                return await mainAction.Invoke();
            }
        }

        public static void HandleFatalException(Exception e, ExceptionSource source)
        {
            s_logger.Trace(e, $"Processing {nameof(HandleFatalException)} for {e.GetType().Name} (from {source})");

            FatalExceptionArgs args = new FatalExceptionArgs(e, source);
            try
            {
                OnFatalException?.Invoke(args);
            }
            catch (Exception callbackException)
            {
                s_logger.Error(callbackException, "Failed to execute fatal exception callback: " + callbackException.Message);
            };

            if (!args.handled)
            {
                s_logger.Fatal(e, "Processing as fatal exception");
                LogManager.Flush();

                if (Debugger.IsAttached)
                {
                    //throw e;
                    ExceptionDispatchInfo.Capture(e).Throw();
                }

                Environment.Exit(1);
            }
            else
            {
                s_logger.Trace($"Exception {e.GetType().Name} handled and considered not fatal");
            }
        }

        //see https://stackoverflow.com/a/21648387/376066
        public static Task FailFastOnException(this Task task)
        {
            task.ContinueWith(
                continuationAction: c => HandleFatalException(c.Exception!, ExceptionSource.ProgramHelper_FailFastOnException),
                continuationOptions: TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously
            );
            return task;
        }

        public enum ExceptionSource
        {
            AppDomain_CurrentDomain_UnhandledException,
            TaskScheduler_UnobservedTaskException,
            Application_ThreadException,
            MainAction_Exception,
            ProgramHelper_FailFastOnException,

            ManualInvokeFromMainAction,
        }

        public sealed class FatalExceptionArgs
        {
            public readonly Exception exception;
            public readonly ExceptionSource source;
            public bool handled = false;

            public FatalExceptionArgs(Exception ex, ExceptionSource source)
            {
                this.exception = ex;
                this.source = source;
            }
        }
    }
}
