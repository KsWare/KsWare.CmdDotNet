using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KsWare.CmdDotNet;

internal static class CreateProcessApi {

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool CreateProcess(
		string lpApplicationName,
		string lpCommandLine,
		ref SECURITY_ATTRIBUTES lpProcessAttributes,
		ref SECURITY_ATTRIBUTES lpThreadAttributes,
		bool bInheritHandles,
		uint dwCreationFlags,
		IntPtr lpEnvironment,
		string lpCurrentDirectory,
		[In] ref STARTUPINFO lpStartupInfo,
		out PROCESS_INFORMATION lpProcessInformation);

	[StructLayout(LayoutKind.Sequential)]
	public struct PROCESS_INFORMATION {

		public IntPtr hProcess;
		public IntPtr hThread;
		public uint dwProcessId;
		public uint dwThreadId;

	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SECURITY_ATTRIBUTES {

		public int length;
		public IntPtr lpSecurityDescriptor;
		public bool bInheritHandle;

	}

	[StructLayout(LayoutKind.Sequential)]
	public struct STARTUPINFO {

		public uint cb;
		public string lpReserved;
		public string lpDesktop;
		public string lpTitle;
		public uint dwX;
		public uint dwY;
		public uint dwXSize;
		public uint dwYSize;
		public uint dwXCountChars;
		public uint dwYCountChars;
		public uint dwFillAttribute;
		public uint dwFlags;
		public short wShowWindow;
		public short cbReserved2;
		public IntPtr lpReserved2;
		public IntPtr hStdInput;
		public IntPtr hStdOutput;
		public IntPtr hStdError;

	}

	private const uint STARTF_USESHOWWINDOW = 1;
	private const short SW_HIDE = 0;
	private const short SW_SHOWNOACTIVATE = 4;

	static void Main(string[] args) {
		var si = new STARTUPINFO();
		var pi = new PROCESS_INFORMATION();
		var saProcess = new SECURITY_ATTRIBUTES();
		var saThread = new SECURITY_ATTRIBUTES();

		si.cb = (uint) Marshal.SizeOf(si);
		si.dwFlags = STARTF_USESHOWWINDOW;
		si.wShowWindow = SW_SHOWNOACTIVATE;

		// Anpassen für spezifische Anforderungen
		var applicationName = @"C:\Windows\System32\notepad.exe";
		string commandLine = null; // Optional: Kommandozeilenargumente

		var result = CreateProcess(
			applicationName,
			commandLine,
			ref saProcess,
			ref saThread,
			false,
			0,
			IntPtr.Zero,
			null, // Arbeitsverzeichnis, null verwendet das Verzeichnis des Parent-Prozesses
			ref si,
			out pi);

		if (!result) {
			throw new Win32Exception();
		}

		// Verwenden Sie pi.hProcess und pi.hThread wie benötigt

		// Schließen der Handles
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool CloseHandle(IntPtr hObject);

}
