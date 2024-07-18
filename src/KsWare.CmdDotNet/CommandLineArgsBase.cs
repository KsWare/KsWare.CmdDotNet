using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;

namespace KsWare.CmdDotNet;

/// <summary>
/// Base class for typed command line args including a parser (deserializer) for Environment.GetCommandlineArgs(). <br/>
/// Usage <code>
/// public CommandLineArgs : CommandLineArgsBase {
///		[CommandLineSwitch("-?","/?","--help")]
///		public bool IsHelp { get; set; }
/// } </code>
/// If you want to use a static initializer, you MUST implement constructor forwarding <code>
/// public CommandLineArgs():base() {}</code>
/// </summary>
public class CommandLineArgsBase {

	protected CommandLineArgsBase() {
		Read(Environment.GetCommandLineArgs());
	}

	protected CommandLineArgsBase(string[] args) {
		Read(args);
	}

	private bool Read(string[] args) {
		var dic = GetType().GetProperties().Select(pi => new {
				Name        = pi.Name,
				SwitchNames = pi.GetCustomAttribute<CommandLineSwitchAttribute>()?.Names??[],
				Type        = pi.PropertyType,
				Attribute   = pi.GetCustomAttribute<CommandLineSwitchAttribute>(),
				SetValue    = new Action<object>(value => pi.SetValue(this, value)),
			})
			.Where(p=>p.Attribute!=null)
			.ToDictionary(o => $"-{o.Name}", o => o, StringComparer.OrdinalIgnoreCase);
		foreach (var v in dic.Values.ToArray()) {
			foreach (var n in v.SwitchNames) {
				dic.TryAdd(n, v);
			}
		}

		for (var i = 0; i < args.Length; i++) {
			if(i==0) continue;
			var arg = args[i];
			if (arg == "--") {
				UnnamedArgs = args.Skip(i).ToArray();
				break;
			}
			var parameter = string.Empty;
			var argHasParameter = false;
			if (arg.Contains('=')) {
				parameter = arg.Split('=', 2)[1];
				arg = arg.Split('=', 2)[0];
				argHasParameter = true;
			}
			if (!dic.TryGetValue(arg, out var p)) {
				return setError($"Unknown argument '{arg}' at index {i}");
			}

			if (p.Type == typeof(bool)) 
				if(!setValue(argHasParameter ? parameter : "true")) return false; else continue;

			if(argHasParameter)
				if(!setValue(parameter)) return false; else continue;

			if(p.Attribute.IsOptionalParameter && (next().Length==0 || !next().StartsWith("-")))
				if(!setValue(p.Attribute.ParameterValue??"True")) return false; else continue;

			if (!p.Attribute.IsOptionalParameter && (next().Length == 0 || next().StartsWith("-")))
				if(!setError($"Missing parameter for '{arg}' at index {i}")) return false; else continue;
			
			if (!setValue(args[i + 1])) return false;
			i++;
			#region private functions
			string next() => i+1 >= args.Length ? string.Empty : args[i+1];
			bool setValue(object s) {
				if (!TryConvert(s, p.Type, arg, i, out var v)) return false;
				p.SetValue(v);
				return true;
			}
			bool setError(string s) { Error = s; return false; }
			#endregion
		}
		return Success = true;
	}

	private bool TryConvert(object value, Type type, string arg, int index, out object? v) {
		try {
			v = Convert.ChangeType(value, type);
			return true;
		}
		catch (FormatException) {
			Error = $"Invalid parameter for '{arg}' at index {index}. Expected:{type.Name}; but was '{value}'";
			v = null;
			return false;
		}
	}

	public string[] UnnamedArgs { get; private set; } = [];
	public bool Success { get; private set; }
	public string Error { get; private set; }

	public string GenerateOptionsHelp() {
		var excludeProperties = new[] {nameof(UnnamedArgs), nameof(Success), nameof(Error)};
		var props = GetType().GetProperties()
			.Where(p => !excludeProperties.Contains(p.Name)).Select(pi => new {
			Name = pi.Name,
			SwitchNames = pi.GetCustomAttribute<CommandLineSwitchAttribute>()?.Names ?? [],
			Parameter = pi.GetCustomAttribute<CommandLineSwitchAttribute>()?.Parameter,
			Help = pi.GetCustomAttribute<CommandLineSwitchAttribute>()?.Description,
			Description = pi.GetCustomAttribute<DescriptionAttribute>()?.Description,
			Type = pi.PropertyType
		});
		var sb = new StringBuilder();
		sb.AppendLine("Options:");
		foreach (var p in props) {
			var sw = string.Join(' ', p.SwitchNames);
			if (sw.Length == 0) sw = $"-{p.Name}";
			if (!string.IsNullOrEmpty(p.Parameter)) sw += $" {p.Parameter}";
			sb.AppendLine($"  {sw,-23} {FormatText(p.Help ?? p.Description,97,26).Trim()}");
		}

		return sb.ToString();
	}

	private static string FormatText(string? input, int lineLength, int indentSize) {
		if (string.IsNullOrEmpty(input)) return string.Empty;

		var indent = new string(' ', indentSize);
		var words = input.Split(' ');
		var result = indent;
		var currentLineLength = indentSize;

		foreach (var word in words) {
			if (currentLineLength + word.Length + 1 > lineLength) {
				result += "\n" + indent + word + " ";
				currentLineLength = indentSize + word.Length + 1;
			}
			else {
				result += word + " ";
				currentLineLength += word.Length + 1;
			}
		}

		return result.TrimEnd();
	}

}