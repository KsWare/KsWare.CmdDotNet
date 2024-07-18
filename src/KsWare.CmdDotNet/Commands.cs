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
		var expandedText = Environment.ExpandEnvironmentVariables(text);
		redirect.WriteLine(expandedText);
		if(redirect is EchoStreamWriter) redirect.Close();
	}

	public static TextWriter ToFile(string path, bool append=false) {
		if(append && File.Exists(path)) return new EchoStreamWriter(path, Encoding.UTF8, new FileStreamOptions{Mode = FileMode.Append, Access = FileAccess.Write});
		return new EchoStreamWriter(path,Encoding.UTF8, new FileStreamOptions{Mode = FileMode.Create, Access = FileAccess.ReadWrite});
	}

	public static TextWriter ToError() => Console.Error;

	public static TextWriter ToNStd() => Console.Out;

	public static TextWriter ToNul() => TextWriter.Null;

	public static string? Prompt(string prompt) {
		Console.Write(Environment.ExpandEnvironmentVariables(prompt));
		return Console.ReadLine();
	}

	/// <summary>
	/// Suspends the processing of a batch program, displaying the prompt, <c>Press any key to continue . . .</c>
	/// </summary>
	/// <remarks>
	/// MSDN: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/pause
	/// </remarks>
	/// <example>
	/// <c>pause >nul</c>
	/// <code>Pause(silent:true)</code>
	/// </example>
	public static void Pause(bool silent = false) {
		if(!silent) Echo("Press any key to continue . . .");
		Console.ReadKey(true);
	}

	public static void Pause(string message) {
		Echo(message);
		Console.ReadKey(true);
	}

	public static void PauseIf(bool condition) {
		if(condition) Pause();
	}

	/// <summary>
	/// <c>chdir</c> equivalent.
	/// </summary>
	/// <param name="path">The path.</param>
	public static void ChDir(string? path = null) => Cd(path);

	/// <summary>
	/// Displays the name of the current directory or changes the current directory. If used with only a drive letter (for example, cd C:), cd displays the names of the current directory in the specified drive. If used without parameters, cd displays the current drive and directory.
	/// </summary>
	/// <param name="path">Specifies the path to the directory that you want to display or change.</param>
	/// <remarks>
	/// <p>Deviant implementation: The switch `/d` is used implicitly, so `Cd("path")` is equivalent to `cd /d [&lt;drive>:]&lt;path>`</p>
	/// MSDN: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/cd
	/// </remarks>
	public static void Cd(string path) {
		// cd /d [<drive>:<path>]
		if (string.IsNullOrEmpty(path)) Echo(Environment.CurrentDirectory);
		if (path == "..") {
			var parent = Path.GetDirectoryName(Environment.CurrentDirectory);
			if (parent == null) return;
			Environment.CurrentDirectory = parent;
			return;
		}
		if (Path.IsPathRooted(path)) {
			Environment.CurrentDirectory = path;
			return;
		}
		Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory,path);
	}

	public static void Cd(TextWriter? redirect = null) => Echo(Environment.CurrentDirectory, redirect);

	/// <summary>
	/// Creates a directory or subdirectory. Command extensions, which are enabled by default, allow you to use a single md command to create intermediate directories in a specified path.
	/// </summary>
	/// <param name="path">Specifies the name and location of the new directory. The maximum length of any single path is determined by the file system. This is a required parameter.</param>
	public static void MkDir(string path) {
		Directory.CreateDirectory(path);
	}

	/// <summary>
	/// Creates a directory or subdirectory. Command extensions, which are enabled by default, allow you to use a single md command to create intermediate directories in a specified path.
	/// </summary>
	/// <param name="path">Specifies the name and location of the new directory. The maximum length of any single path is determined by the file system. This is a required parameter.</param>
	public static void Md(string path) => MkDir(path);

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

	public static int Start(string command, string[] args, bool wait = false, ProcessWindowStyle windowStyle=ProcessWindowStyle.Normal) {
		var psi = new ProcessStartInfo(command);
		foreach (var s in args) psi.ArgumentList.Add(s);
		psi.WindowStyle = windowStyle;
		var p = Process.Start(psi);
		if(wait)p.WaitForExit();
		ExitCode = p.ExitCode;
		return p.ExitCode;
	}

	public static void Set(string environmentVariable, string value) {
		Environment.SetEnvironmentVariable(environmentVariable, value);
	}

	public static void SetX(string environmentVariable, string value, bool machine = false) {
		Environment.SetEnvironmentVariable(environmentVariable, value);
		Environment.SetEnvironmentVariable(environmentVariable, value, machine ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User);
	}

	public static string? Get(string environmentVariable) {
		return environmentVariable.Contains('%') 
			? Environment.ExpandEnvironmentVariables(environmentVariable) 
			: Environment.GetEnvironmentVariable(environmentVariable);
	}

	private class EchoStreamWriter : StreamWriter {

		public EchoStreamWriter(string path, Encoding encoding, FileStreamOptions options) : base(path, encoding, options) { }

	}
}

