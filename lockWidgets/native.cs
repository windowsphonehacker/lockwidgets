using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace lockWidgets
{
    using System.Runtime.InteropServices;

    [ComImport, ClassInterface(ClassInterfaceType.None), Guid("56624E8C-CF91-41DF-9C31-E25A98FAF464")]
    public class Cmangodll
    {
    }

    [ComImport, Guid("FA2E618D-CEA7-4C9E-B684-21FF5C71CEA8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface Imangodll
    {
        void TestMethod1();
        [return: MarshalAs(UnmanagedType.BStr)]
        string TestMethod2([MarshalAs(UnmanagedType.BStr)] string InputString);

        [PreserveSig]
        int StringCall(string dll, string method, string value);
        [PreserveSig]
        int UintCall(string dll, string method, uint value);

        [PreserveSig]
        int ShutdownOS(uint ewxCode);

        [return: MarshalAs(UnmanagedType.BStr)]
        string ReadRegistryStringValue(int key, [MarshalAs(UnmanagedType.BStr)] string path, [MarshalAs(UnmanagedType.BStr)]string value);

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetSubs(int key, [MarshalAs(UnmanagedType.BStr)] string path);

        [PreserveSig]
        int getHandleFromName(string query, out uint hnd);

        [PreserveSig]
        int GetLastError7();

        [PreserveSig]
        void GetCursorPos7();

        [PreserveSig]
        int createWindowTreeUpdater7(uint wnd, out uint test);

        [PreserveSig]
        int getUnreadSMSCount(out int count);

        [return: MarshalAs(UnmanagedType.BStr)]
        string getLatestSMS();


    }
}
