using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class ClipboardTest
{
	private const int WM_CLIPBOARDUPDATE = 0x031D;
	private static IntPtr HWND_MESSAGE = new IntPtr(-3);
	private static IntPtr hWndGlobal;

	private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	[StructLayout(LayoutKind.Sequential)]
	private struct WNDCLASSEX
	{
		public uint cbSize;
		public uint style;
		public IntPtr lpfnWndProc;
		public int cbClsExtra;
		public int cbWndExtra;
		public IntPtr hInstance;
		public IntPtr hIcon;
		public IntPtr hCursor;
		public IntPtr hbrBackground;
		[MarshalAs(UnmanagedType.LPTStr)] public string lpszMenuName;
		[MarshalAs(UnmanagedType.LPTStr)] public string lpszClassName;
		public IntPtr hIconSm;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MSG
	{
		public IntPtr hwnd;
		public uint message;
		public IntPtr wParam;
		public IntPtr lParam;
		public uint time;
		public int pt_x;
		public int pt_y;
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool AddClipboardFormatListener(IntPtr hwnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll")]
	private static extern bool TranslateMessage([In] ref MSG lpMsg);

	[DllImport("user32.dll")]
	private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr CreateWindowEx(
		int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
		int x, int y, int nWidth, int nHeight,
		IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

	[DllImport("user32.dll")]
	private static extern IntPtr GetClipboardOwner();

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("psapi.dll", SetLastError = true)]
	private static extern uint GetProcessImageFileName(IntPtr hProcess, StringBuilder lpImageFileName, int nSize);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(IntPtr hObject);

	private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

	static void Main()
	{
		IntPtr hInstance = GetModuleHandle("");
		string className = "MyWindowClass";

		WndProc wndProcDelegate = WndProcFunction;
		IntPtr wndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);

		WNDCLASSEX wndClassEx = new WNDCLASSEX
		{
			cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
			lpfnWndProc = wndProcPtr,
			hInstance = hInstance,
			lpszClassName = className
		};

		RegisterClassEx(ref wndClassEx);

		hWndGlobal = CreateWindowEx(0, className, "Hidden Clipboard Listener", 0,
			0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

		AddClipboardFormatListener(hWndGlobal);

		new Thread(() =>
		{
			Console.WriteLine("Type \"exit\" at any time to exit:");
			while (true)
			{
				string? input = Console.ReadLine()?.Trim()?.ToLower();
				if (input == "exit")
				{
					Console.WriteLine("Removing clipboard listener...");
					RemoveClipboardFormatListener(hWndGlobal);
					Console.WriteLine("Exiting program.");
					Environment.Exit(0);
				}
			}
		}).Start();

		MSG msg;
		while (GetMessage(out msg, hWndGlobal, 0, 0))
		{
			TranslateMessage(ref msg);
			DispatchMessage(ref msg);
		}
	}

	private static IntPtr WndProcFunction(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		switch (msg)
		{
			case WM_CLIPBOARDUPDATE:
				Console.WriteLine("WM_CLIPBOARDUPDATE");
				IntPtr owner = GetClipboardOwner();
				Console.WriteLine($"\tOwner HWND: {owner}");

				GetWindowThreadProcessId(owner, out int pid);
				string processName = GetProcessImageFileName(pid);
				Console.WriteLine($"\tOwner Proc: {processName}");
				break;
		}
		return DefWindowProc(hWnd, msg, wParam, lParam);
	}

	private static string GetProcessImageFileName(int pid)
	{
		IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
		if (hProcess == IntPtr.Zero)
			return "?";

		StringBuilder buffer = new StringBuilder(1024);
		GetProcessImageFileName(hProcess, buffer, buffer.Capacity);
		CloseHandle(hProcess);

		return buffer.ToString();
	}
}
