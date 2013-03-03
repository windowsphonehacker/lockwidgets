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

namespace lockWidgets2
{
    using System.Runtime.InteropServices;

    [ComImport, ClassInterface(ClassInterfaceType.None), Guid("E79018CB-46A6-432D-8077-8C0863533001")]
    public class Cmangodll
    {
    }

    [ComImport, Guid("BBC99824-106F-4037-9303-FD4F38C09AE1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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


        [PreserveSig]
        int GetSystemPowerStatusEx7(out byte percent);

        [PreserveSig]
        int MessageBox7(string lpText, string lpCaption, uint uType, out int result);

        [PreserveSig]
        int getMemoryLoad(out byte percent);
    }
}
