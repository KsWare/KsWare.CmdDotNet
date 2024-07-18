using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace KsWare.CmdDotNet;

public static partial class Commands {
	
	[PublicAPI]
	public static void MkLink(string linkFileName, string targetFileName, MkLinkType linkType) {

		if (File.Exists(linkFileName)) {
			var targetPath = GetTargetPath(linkFileName);
			if (targetFileName.Equals(targetPath, StringComparison.OrdinalIgnoreCase)) return;
		}

		// if (IsAdmin()==false) {
		// 	var p = StartAsAdmin(symlinkFileName, targetFileName);
		// 	if (p == null) return;
		// 	p.WaitForExit();
		// 	Console.Out.WriteLine(p.StandardOutput.ReadToEnd());
		// 	Console.Error.WriteLine(p.StandardError.ReadToEnd());
		// 	ExitCode = p.ExitCode;
		// 	return;
		// }

		if (linkType == MkLinkType.Hardlink) {
			CreateHardLink(linkFileName, targetFileName, IntPtr.Zero);
			if (Marshal.GetLastWin32Error() != 0) throw new Win32Exception();
			return;
		}

		if (linkType == MkLinkType.Junktion) {
			Junction.Create(linkFileName, targetFileName, true);
			return;
		}

		var flag = linkType switch {
			MkLinkType.Default => Directory.Exists(targetFileName)
				? SYMBOLIC_LINK_FLAG_DIRECTORY
				: SYMBOLIC_LINK_FLAG_FILE,
			MkLinkType.DirectorySymLink => SYMBOLIC_LINK_FLAG_DIRECTORY,
			_ => SYMBOLIC_LINK_FLAG_FILE
		};
		flag |= SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
		_CreateSymbolicLink(linkFileName, targetFileName, flag);
		if(Marshal.GetLastWin32Error()!=0) throw new Win32Exception();
	}

	// Importiere die CreateSymbolicLink-Funktion aus der kernel32.dll
	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateSymbolicLink")]
	private static extern bool _CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

	private static Process? StartAsAdmin(string symlinkFileName, string targetFileName) {
		// Starte das aktuelle Skript erneut mit erhöhten Rechten (als Administrator)
		var startInfo = new ProcessStartInfo();
		startInfo.FileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"\\CmdTools.exe";
		startInfo.ArgumentList.Add(nameof(MkLink));
		startInfo.ArgumentList.Add(symlinkFileName);
		startInfo.ArgumentList.Add(targetFileName);
		startInfo.Verb = "runas"; // Starte mit Administratorrechten
		startInfo.RedirectStandardOutput = true;
		startInfo.RedirectStandardError = true;
		try {
			return Process.Start(startInfo);
		}
		catch (Win32Exception ex) {
			Console.Error.WriteLine($"{ex.GetType().Name}{ex.Message}");
			ExitCode = ex.NativeErrorCode;
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"{ex.GetType().Name}{ex.Message}");
			ExitCode = 31; // ERROR_GEN_FAILURE
		}
		return null;
	}

	// Definiere die Konstanten für die CreateSymbolicLink-Funktion
	private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;
	private const int SYMBOLIC_LINK_FLAG_FILE = 0x0;
	private const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int GetFinalPathNameByHandle(IntPtr handle, [In, Out] char[] path, int bufLen, int flags);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(IntPtr hObject);

	private const uint FILE_SHARE_READ = 0x00000001;
	private const uint OPEN_EXISTING = 3;
	private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);


	private static string GetTargetPath(string symlinkPath) {
		var hFile = CreateFile(symlinkPath, 0, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING,
			FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
		if (hFile.ToInt64() == -1) throw new Win32Exception();
		try {
			var path = new char[260];
			var size = GetFinalPathNameByHandle(hFile, path, path.Length, 0);
			if (size == 0) throw new Win32Exception();
			return new string(path, 0, size);
		}
		finally {
			CloseHandle(hFile);
		}
	}

	// Methode zum Überprüfen, ob der Benutzer Adminrechte hat
	private static bool IsAdmin() {
		using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent()) {
			var principal = new System.Security.Principal.WindowsPrincipal(identity);
			return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
		}
	}

	private static class Junction {

		[Flags]
		private enum Win32FileAccess : uint {

			GenericRead = 0x80000000U,
			GenericWrite = 0x40000000U,
			GenericExecute = 0x20000000U,
			GenericAll = 0x10000000U

		}

		[Flags]
		private enum Win32FileAttribute : uint {

			AttributeReadOnly = 0x1U,
			AttributeHidden = 0x2U,
			AttributeSystem = 0x4U,
			AttributeDirectory = 0x10U,
			AttributeArchive = 0x20U,
			AttributeDevice = 0x40U,
			AttributeNormal = 0x80U,
			AttributeTemporary = 0x100U,
			AttributeSparseFile = 0x200U,
			AttributeReparsePoint = 0x400U,
			AttributeCompressed = 0x800U,
			AttributeOffline = 0x1000U,
			AttributeNotContentIndexed = 0x2000U,
			AttributeEncrypted = 0x4000U,
			AttributeIntegrityStream = 0x8000U,
			AttributeVirtual = 0x10000U,
			AttributeNoScrubData = 0x20000U,
			AttributeEA = 0x40000U,
			AttributeRecallOnOpen = 0x40000U,
			AttributePinned = 0x80000U,
			AttributeUnpinned = 0x100000U,
			AttributeRecallOnDataAccess = 0x400000U,
			FlagOpenNoRecall = 0x100000U,

			/// <summary>
			/// Normal reparse point processing will not occur; CreateFile will attempt to open the reparse point. When a file is opened, a file handle is returned,
			/// whether or not the filter that controls the reparse point is operational.
			/// <br />This flag cannot be used with the <see cref="FileMode.Create"/> flag.
			/// <br />If the file is not a reparse point, then this flag is ignored.
			/// </summary>
			FlagOpenReparsePoint = 0x200000U,
			FlagSessionAware = 0x800000U,
			FlagPosixSemantics = 0x1000000U,

			/// <summary>
			/// You must set this flag to obtain a handle to a directory. A directory handle can be passed to some functions instead of a file handle.
			/// </summary>
			FlagBackupSemantics = 0x2000000U,
			FlagDeleteOnClose = 0x4000000U,
			FlagSequentialScan = 0x8000000U,
			FlagRandomAccess = 0x10000000U,
			FlagNoBuffering = 0x20000000U,
			FlagOverlapped = 0x40000000U,
			FlagWriteThrough = 0x80000000U

		}

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern SafeFileHandle CreateFile(string lpFileName, Win32FileAccess dwDesiredAccess,
			FileShare dwShareMode, IntPtr lpSecurityAttributes, FileMode dwCreationDisposition,
			Win32FileAttribute dwFlagsAndAttributes, IntPtr hTemplateFile);

		// Because the tag we're using is IO_REPARSE_TAG_MOUNT_POINT, we use the MountPointReparseBuffer struct in the DUMMYUNIONNAME union.
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct ReparseDataBuffer {

			/// <summary>Reparse point tag. Must be a Microsoft reparse point tag.</summary>
			public uint ReparseTag;

			/// <summary>Size, in bytes, of the reparse data in the buffer that <see cref="PathBuffer"/> points to.</summary>
			public ushort ReparseDataLength;

			/// <summary>Reserved; do not use.</summary>
			private ushort Reserved;

			/// <summary>Offset, in bytes, of the substitute name string in the <see cref="PathBuffer"/> array.</summary>
			public ushort SubstituteNameOffset;

			/// <summary>Length, in bytes, of the substitute name string. If this string is null-terminated, <see cref="SubstituteNameLength"/> does not include space for the null character.</summary>
			public ushort SubstituteNameLength;

			/// <summary>Offset, in bytes, of the print name string in the <see cref="PathBuffer"/> array.</summary>
			public ushort PrintNameOffset;

			/// <summary>Length, in bytes, of the print name string. If this string is null-terminated, <see cref="PrintNameLength"/> does not include space for the null character.</summary>
			public ushort PrintNameLength;

			/// <summary>
			/// A buffer containing the unicode-encoded path string. The path string contains the substitute name
			/// string and print name string. The substitute name and print name strings can appear in any order.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8184)]
			internal string PathBuffer;
			// with <MarshalAs(UnmanagedType.ByValArray, SizeConst:=16368)> Public PathBuffer As Byte()
			// 16368 is the amount of bytes. since a unicode string uses 2 bytes per character, constrain to 16368/2 = 8184 characters.

		}

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode,
			[In] ReparseDataBuffer lpInBuffer, uint nInBufferSize,
			IntPtr lpOutBuffer, uint nOutBufferSize,
			[Out] uint lpBytesReturned, IntPtr lpOverlapped);

		public static void Create(string junctionPath, string targetDir, bool overwrite = false) {
			const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003U;
			const uint IO_REPARSE_TAG_SYMLINK =0xA000000CU;
			const uint FSCTL_SET_REPARSE_POINT = 0x900A4U;
			// This prefix indicates to NTFS that the path is to be treated as a non-interpreted path in the virtual file system.
			const string NonInterpretedPathPrefix = @"\??\";

			if (Directory.Exists(junctionPath)) {
				if (!overwrite)
					throw new IOException("Directory already exists and overwrite parameter is false.");
			}
			else {
				Directory.CreateDirectory(junctionPath);
			}
			targetDir = NonInterpretedPathPrefix + Path.GetFullPath(targetDir);

			using (var reparsePointHandle = CreateFile(junctionPath, Win32FileAccess.GenericWrite,
				       FileShare.Read | FileShare.Write | FileShare.Delete, IntPtr.Zero, FileMode.Open,
				       Win32FileAttribute.FlagBackupSemantics | Win32FileAttribute.FlagOpenReparsePoint, IntPtr.Zero)) {
				if (reparsePointHandle.IsInvalid || Marshal.GetLastWin32Error() != 0) {
					throw new IOException("Unable to open reparse point.", new Win32Exception());
				}

				// unicode string is 2 bytes per character, so *2 to get byte length
				ushort byteLength = (ushort) (targetDir.Length * 2);
				var reparseDataBuffer = new ReparseDataBuffer() {
					ReparseTag = IO_REPARSE_TAG_MOUNT_POINT,
					ReparseDataLength = (ushort) (byteLength + 12u),
					SubstituteNameOffset = 0,
					SubstituteNameLength = byteLength,
					PrintNameOffset = (ushort) (byteLength + 2u),
					PrintNameLength = 0,
					PathBuffer = targetDir
				};

				bool result = DeviceIoControl(reparsePointHandle, FSCTL_SET_REPARSE_POINT, reparseDataBuffer,
					(uint) (byteLength + 20), IntPtr.Zero, 0u, 0u, IntPtr.Zero);
				if (!result)
					throw new IOException("Unable to create junction point.", new Win32Exception());
			}
		}

	}

}

public enum MkLinkType {

	Default,
	FileSymLink=Default,
	DirectorySymLink,
	Hardlink,
	Junktion
}
