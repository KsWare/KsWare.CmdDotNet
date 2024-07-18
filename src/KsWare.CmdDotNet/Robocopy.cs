using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KsWare.CmdDotNet;

public static partial class Commands {

	public static int Robocopy(string arguments) {
		var psi = new ProcessStartInfo("robocopy.exe", arguments);
		var p = Process.Start(psi);
		p.WaitForExit();
		ExitCode = p.ExitCode;
		return p.ExitCode;
	}
}
