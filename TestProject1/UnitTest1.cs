using CSCPPInteropFuncSet;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }
        [TestMethod]
        public async Task TestAdd2Nums()
        {
            TestContext.WriteLine("Running test. Curdir = " + Environment.CurrentDirectory);
            await RunInSTAExecutionContextAsync(async () =>
            {
                var x = new MainWindow();
                await Task.Yield();
                TestContext.WriteLine("adding 2 numbers" + MainWindow.Add(10, 20).ToString());
            });
        }
        [TestMethod]
        public async Task TestAdd2Strings()
        {
            TestContext.WriteLine("Running test. Curdir = " + Environment.CurrentDirectory);
            await RunInSTAExecutionContextAsync(async () =>
            {
                var x = new MainWindow();
                await Task.Yield();

                TestContext.WriteLine("adding 2 numbers" + MainWindow.AddStrings("hello","world").ToString());
            });
        }
        [TestMethod]
        public async Task TestAdd2StringsStress()
        {
            TestContext.WriteLine("Running test. Curdir = " + Environment.CurrentDirectory);
            await RunInSTAExecutionContextAsync(async () =>
            {
                var x = new MainWindow();
                await Task.Yield();
                var numIterations = 10000000;
                var lastTotMemory = 0l;
                var lstLeak = new List<string>();
                var modulo = 100000;
                for (int i = 0; i < numIterations; i++)
                {
                    if (i % modulo == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        var totMemory = GC.GetTotalMemory(true);
                        var delta = (totMemory - lastTotMemory)/modulo;
                        lastTotMemory = totMemory;

                        TestContext.WriteLine($"adding 2 numbers {totMemory:n1} {delta}");
                    }
                    var res = MainWindow.AddStrings("hello", "world").ToString();
                    //lstLeak.Add(res);
                }

                TestContext.WriteLine("adding 2 numbers" + MainWindow.AddStrings("hello", "world").ToString());
            });
        }

        public void LogMessage(string message)
        {
            TestContext.WriteLine(message);
        }
        public async Task RunInSTAExecutionContextAsync(Func<Task> actionAsync)
        {
            var tcsStaThread = new TaskCompletionSource<int>();
            LogMessage($"Creating ExecutionContext");
            var execContext = CreateExecutionContext(tcsStaThread);
            LogMessage($"Created ExecutionContext");
            var tcs = new TaskCompletionSource<int>();
            Exception? except = null;
            await execContext.Dispatcher.InvokeAsync(async () =>
            {
                await Task.Yield();
                try
                {
                    await actionAsync();
                }
                catch (Exception ex)
                {
                    except = ex;
                }
                LogMessage($"Before invokeshutdown");

                execContext.Dispatcher.InvokeShutdown();
                LogMessage($" after invokeshutdown");
                tcs.SetResult(0);
                LogMessage($" after set tcs");
            });
            await tcs.Task;
            if (except != null)
            {
                LogMessage($"Exception in execution thread {except}");
                throw except;
            }
            LogMessage($"done task. Now waiting for STAThread to end");
            await tcsStaThread.Task; // wait for sta thread to finish
        }

        MyExecutionContext CreateExecutionContext(TaskCompletionSource<int> tcsStaThread)
        {
            const string Threadname = "MyStaThread";
            var tcsGetExecutionContext = new TaskCompletionSource<MyExecutionContext>();

            LogMessage($"Creating {Threadname}");
            var myStaThread = new Thread(() =>
            {
                // Create the context, and install it:
                LogMessage($"{Threadname} start");
                var dispatcher = Dispatcher.CurrentDispatcher;
                var syncContext = new DispatcherSynchronizationContext(dispatcher);

                SynchronizationContext.SetSynchronizationContext(syncContext);

                tcsGetExecutionContext.SetResult(new MyExecutionContext(syncContext, dispatcher));

                // Start the Dispatcher Processing
                LogMessage($"MyStaThread before Dispatcher.run");
                Dispatcher.Run();
                LogMessage($"MyStaThread After Dispatcher.run");
                tcsStaThread.SetResult(0);
            });

            myStaThread.SetApartmentState(ApartmentState.STA);
            myStaThread.Name = Threadname;
            myStaThread.Start();
            LogMessage($"Starting {Threadname}");
            return tcsGetExecutionContext.Task.Result;
        }

        public class MyExecutionContext
        {
            public MyExecutionContext(DispatcherSynchronizationContext syncContext, Dispatcher dispatcher)
            {
                this.DispatcherSynchronizationContext = syncContext;
                this.Dispatcher = dispatcher;
            }
            public DispatcherSynchronizationContext DispatcherSynchronizationContext { get; set; }
            public Dispatcher Dispatcher { get; set; }
        }

    }
}