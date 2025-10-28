import java.util.Scanner;

import com.sun.jna.Native;
import com.sun.jna.platform.win32.Kernel32;
import com.sun.jna.platform.win32.User32;
import com.sun.jna.platform.win32.WTypes.LPSTR;
import com.sun.jna.platform.win32.WinDef.DWORD;
import com.sun.jna.platform.win32.WinDef.HMODULE;
import com.sun.jna.platform.win32.WinDef.HWND;
import com.sun.jna.platform.win32.WinDef.LPARAM;
import com.sun.jna.platform.win32.WinDef.LRESULT;
import com.sun.jna.platform.win32.WinDef.WPARAM;
import com.sun.jna.platform.win32.WinNT;
import com.sun.jna.platform.win32.WinUser;
import com.sun.jna.platform.win32.WinUser.MSG;
import com.sun.jna.platform.win32.WinUser.WNDCLASSEX;
import com.sun.jna.platform.win32.WinUser.WindowProc;
import com.sun.jna.ptr.IntByReference;

public class ClipboardTest {

	public static void main(String[] args) {
		HMODULE hInst = Kernel32.INSTANCE.GetModuleHandle("");
		WNDCLASSEX wClass = new WNDCLASSEX();
		wClass.hInstance = hInst;

		wClass.lpfnWndProc = new WindowProc() {

			@Override
			public LRESULT callback(HWND hwnd, int uMsg, WPARAM wParam, LPARAM lParam) {
				switch (uMsg) {
					case WinUser.WM_CREATE:
						System.out.println("WM_CREATE");
						MyUser32.INSTANCE.AddClipboardFormatListener(hwnd);
						return new LRESULT(0);
					case 0x031D: // WM_CLIPBOARDUPDATE
						System.out.println("WM_CLIPBOARDUPDATE");

						HWND owner = MyUser32.INSTANCE.GetClipboardOwner();
						System.out.println("\tOwner HWND: " + owner);

						IntByReference processId = new IntByReference();
						User32.INSTANCE.GetWindowThreadProcessId(owner, processId);
						int pid = processId.getValue();
						System.out.println("\tOwner Proc: " + getProcessImageFileName(pid));

						return new LRESULT(0);
					default:
						return User32.INSTANCE.DefWindowProc(hwnd, uMsg, wParam, lParam);
				}
			}

		};

		String windowClass = "MyWindowClass";
		wClass.lpszClassName = windowClass;
		User32.INSTANCE.RegisterClassEx(wClass);

		HWND hWnd = User32.INSTANCE.CreateWindowEx(User32.WS_EX_TOPMOST, windowClass, "My hidden helper window", 0, 0,
				0, 0, 0, null, null, hInst, null);

		new Thread(() -> {
			System.out.println("Type \"exit\" at any time to exit");
			@SuppressWarnings("resource")
			Scanner scanner = new Scanner(System.in);

			while (true) {
				String input = scanner.nextLine().trim().toLowerCase();
				if (input.equals("exit"))
					break;
			}

			System.out.println("Exiting program");
			System.exit(0);
		}).start();

		MSG msg = new MSG();
		while (User32.INSTANCE.GetMessage(msg, hWnd, 0, 0) > 0) {
			User32.INSTANCE.TranslateMessage(msg);
			User32.INSTANCE.DispatchMessage(msg);
		}
	}

	public interface MyUser32 extends User32 {

		MyUser32 INSTANCE = com.sun.jna.Native.load("user32", MyUser32.class);

		boolean AddClipboardFormatListener(HWND hwnd);

		boolean RemoveClipboardFormatListener(HWND hwnd);

		HWND GetClipboardOwner();

	}

	interface Psapi extends com.sun.jna.platform.win32.Psapi {

		Psapi INSTANCE = Native.load("Psapi", Psapi.class);

		int GetProcessImageFileNameA(WinNT.HANDLE hProcess, LPSTR lpImageFileName, DWORD nSize);

	}

	private static String getProcessImageFileName(int pid) {
		WinNT.HANDLE h = Kernel32.INSTANCE.OpenProcess(WinNT.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
		if (h == null)
			return null;
		LPSTR buf = new LPSTR(new String(new byte[1024]));
		try {
			return Psapi.INSTANCE.GetProcessImageFileNameA(h, buf, new DWORD(1024)) > 0 ? buf.toString().trim() : null;
		} finally {
			Kernel32.INSTANCE.CloseHandle(h);
		}
	}

}
