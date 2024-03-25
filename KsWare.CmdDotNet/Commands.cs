using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace KsWare.CmdDotNet;

public static partial class Commands {

	private static readonly List<string> s_pushDStack = new();

	public static int ExitCode { get; set; }

	public static void Echo(string text, TextWriter? redirect=null) {
		if (redirect == null) redirect = Console.Out;
		redirect.WriteLine(text);
		if(redirect==Console.Out) return;
		if(redirect==Console.Error) return;
		redirect.Close();
	}

	public static TextWriter ToFile(string path, bool append=false) {
		if(append && File.Exists(path)) return new StreamWriter(path,Encoding.UTF8, new FileStreamOptions{Mode = FileMode.Append, Access = FileAccess.Write});
		return new StreamWriter(path,Encoding.UTF8, new FileStreamOptions{Mode = FileMode.Create, Access = FileAccess.ReadWrite});
	}

	public static string? Prompt(string s) {
		Console.Write(s);
		return Console.ReadLine();
	}

	public static void Wait() {
		Console.ReadKey(true);
	}

	public static void Wait(bool condition) {
		if(condition) Wait();
	}

	/// <summary>
	/// Stores the current directory for use by the <see cref="PopD"/> command, and then changes to the specified directory.
	/// </summary>
	/// <param name="path">Specifies the directory to make the current directory. This command supports relative paths.</param>
	/// <exception cref="ArgumentNullException">Argument '<paramref name="path"/>' must not be null or empty.</exception>
	public static void PushD(string path) {
		if(string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path), $"Argument '{nameof(path)}' must not be null or empty.");
		path = Path.GetFullPath(path);
		s_pushDStack.Add(Environment.CurrentDirectory);
		Environment.CurrentDirectory = path;
	}

	/// <summary>
	/// Changes the current directory to the directory that was most recently stored by the <see cref="PushD"/> command.
	/// </summary>
	public static void PopD() {
		var path = s_pushDStack[s_pushDStack.Count - 1];
		s_pushDStack.RemoveAt(s_pushDStack.Count-1);
		Environment.CurrentDirectory = path;
	}

	public static int Robocopy(string arguments) {
		var psi = new ProcessStartInfo("robocopy.exe", arguments);
		var p = Process.Start(psi);
		p.WaitForExit();
		ExitCode = p.ExitCode;
		return p.ExitCode;
	}
}
