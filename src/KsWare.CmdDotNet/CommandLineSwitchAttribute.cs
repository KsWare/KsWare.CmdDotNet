using System;

namespace KsWare.CmdDotNet;

[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property, AllowMultiple = false)]
public class CommandLineSwitchAttribute : Attribute {

	public CommandLineSwitchAttribute(params string[] names) {
		Names = names;
	}

	public string[] Names { get; set; }

	public string? Description { get; set; }

	public string? Parameter { get; set; }

	public bool IsOptionalParameter => !string.IsNullOrEmpty(Parameter) && Parameter.StartsWith("[", StringComparison.OrdinalIgnoreCase);

	public bool HasParameter => !string.IsNullOrEmpty(Parameter);
	public object? ParameterValue { get; set; }

}