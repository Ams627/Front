using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class FrontHelper
{
    const int SW_RESTORE = 9;
    const uint KEYEVENTF_KEYUP = 0x2;
    const byte VK_MENU = 0x12; // Alt key

    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_SHOWWINDOW = 0x0040;

    public static void BringToFrontSmart(int pid)
    {
        try
        {
            Process proc = Process.GetProcessById(pid);
            IntPtr hWnd = proc.MainWindowHandle;
            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("No main window found.");
                return;
            }

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            IntPtr fgWindow = GetForegroundWindow();
            uint fgThread = GetWindowThreadProcessId(fgWindow, out _);
            uint thisThread = GetCurrentThreadId();

            AttachThreadInput(thisThread, fgThread, true);
            bool success = SetForegroundWindow(hWnd);
            AttachThreadInput(thisThread, fgThread, false);

            if (!success)
            {
                // Try simulated Alt key press to satisfy focus-stealing rules
                keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                success = SetForegroundWindow(hWnd);
            }

            if (!success)
            {
                // Use TopMost trick to visually bring to front
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

                // Flash taskbar as a fallback
                FlashWindow(hWnd, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to bring PID {pid} to front: {ex.Message}");
        }
    }
}
