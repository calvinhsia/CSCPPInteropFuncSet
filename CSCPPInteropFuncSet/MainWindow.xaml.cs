using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CSCPPInteropFuncSet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }
        [DllImport("CPPFunctioncSet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int Add(int a, int b);
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var x = System.Environment.CurrentDirectory;
            int a = 10;
            int b = 20;
            int c = Add(a, b);

            MessageBox.Show($"Add({a},{b})={c}");


        }
    }
    public class DllHandler
    {
        static class HResult
        {
            public const int S_OK = 0;
            public const int S_FALSE = 1;
            public const int E_FAIL = unchecked((int)0x80004005);
        }
        internal delegate int DllGetClassObject(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid ClassId,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IntPtr ppvObject);

        delegate int CanUnloadNowRoutine();
        CanUnloadNowRoutine? _deldllCanUnloadNow;

        IntPtr _hModule = IntPtr.Zero;
        /// <summary>Creates com object with the given clsid in the specified file</summary>
        /// <param name="fnameComClass">The path of the module</param>
        /// <param name="clsidOfComObj">The CLSID of the com object</param>
        /// <param name="riid">The IID of the interface requested</param>
        /// <param name="pvObject">The interface pointer. Upon failure pvObject is IntPtr.Zero</param>
        /// <returns>An HRESULT</returns>
        //[HandleProcessCorruptedStateExceptions]
        internal int CoCreateFromFile(string fnameComClass, Guid clsidOfComObj, Guid riid, out IntPtr pvObject)
        {
            pvObject = IntPtr.Zero;
            int hr = HResult.E_FAIL;
            try
            {
                _hModule = LoadLibrary(fnameComClass);
                if (_hModule != IntPtr.Zero)
                {
                    IntPtr optrDllGetClassObject = GetProcAddress(_hModule, "DllGetClassObject");
                    if (optrDllGetClassObject != IntPtr.Zero)
                    {
                        var delDllGetClassObject = Marshal.GetDelegateForFunctionPointer<DllGetClassObject>(optrDllGetClassObject);
                        var optrDllCanUnloadNow = GetProcAddress(_hModule, "DllCanUnloadNow");
                        _deldllCanUnloadNow = Marshal.GetDelegateForFunctionPointer<CanUnloadNowRoutine>(optrDllCanUnloadNow);

                        IntPtr pClassFactory = IntPtr.Zero;
                        Guid iidIUnknown = new Guid(IUnknownGuid);
                        hr = delDllGetClassObject(clsidOfComObj, iidIUnknown, out pClassFactory);
                        if (hr == HResult.S_OK)
                        {
                            var classFactory = (IClassFactory)Marshal.GetTypedObjectForIUnknown(pClassFactory, typeof(IClassFactory));
                            hr = classFactory.CreateInstance(IntPtr.Zero, ref riid, out pvObject);
                            Marshal.ReleaseComObject(classFactory);
                            Marshal.Release(pClassFactory);
                        }
                    }
                    else
                    {
                        hr = Marshal.GetHRForLastWin32Error();
                        Debug.Assert(false, $"Unable to find DllGetClassObject: {hr}");
                    }
                }
                else
                {
                    throw new FileNotFoundException(fnameComClass);
                }
            }
            catch (Exception ex)
            {
                var x = ex.ToString(); // HandleProcessCorruptedStateExceptions
                throw new InvalidOperationException(x);
            }
            return hr;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string dllName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int FreeLibrary(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procname);


        const string IUnknownGuid = "00000001-0000-0000-C000-000000000046";
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(IUnknownGuid)]
        private interface IClassFactory
        {
            [PreserveSig]
            int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
            int LockServer(int fLock);
        }
    }

    [ComVisible(true)]
    [Guid("D6B8AFEF-7DFD-45A2-B461-3B4433EA5357")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe public interface ICPPInteropFuncSet
    {
        int Add(int a, int b);
    }
}