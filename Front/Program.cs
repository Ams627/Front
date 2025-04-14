using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


class Program
{
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

    [DllImport("user32.dll")]
    static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    const byte VK_MENU = 0x12; // Alt key


    [DllImport("user32.dll")]
    static extern bool BringWindowToTop(IntPtr hWnd);


    static string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length == 0) return string.Empty;

        var sb = new System.Text.StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    static void Main(string[] args)
    {
        if (args.Length != 1 || !int.TryParse(args[0], out int targetPid))
        {
            Console.Error.WriteLine("Usage: Front.exe <PID>2");
            return;
        }

        if (!TrySendToServer(targetPid))
        {
            File.AppendAllLines(@"c:\temp\2.txt", [$"first instance: {Process.GetCurrentProcess().Id}"]);
            // Couldn't reach pipe → we're the first instance
            StartServerAndRun(targetPid);
            Thread.Sleep(Timeout.Infinite); // Keep the server alive
        }

    }

    private static void HandlePid(int targetPid)
    {
        FrontHelper.BringToFrontSmart(targetPid);
    }

    static void StartServerAndRun(int initialPid)
    {
        Task.Run(() => ListenOnPipe());
        HandlePid(initialPid);
    }

    static void ListenOnPipe()
    {
        var pipeName = "front_pipe";
        while (true)
        {
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.In))
            using (var reader = new StreamReader(server))
            {
                server.WaitForConnection();

                string pidLine = reader.ReadLine();
                if (int.TryParse(pidLine, out int pid))
                {
                    File.AppendAllLines(@"c:\temp\2.txt", [$"handle pid {pid}"]);

                    HandlePid(pid);
                }
            }
        }
    }

    static bool TrySendToServer(int pid)
    {
        try
        {
            using (var client = new NamedPipeClientStream(".", "front_pipe", PipeDirection.Out))
            {
                client.Connect(500);
                using (var writer = new StreamWriter(client) { AutoFlush = true })
                {
                    writer.WriteLine(pid);
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }
    }

}
