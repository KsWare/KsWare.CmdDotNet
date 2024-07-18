using NUnit.Framework.Constraints;

namespace KsWare.CmdDotNet.Tests;

public class MkLinkTests {

	private string _folder;

	[SetUp]
	public void Setup() {
		_folder=Path.Combine(Path.GetTempPath(), "2E448241-F5D3-4E23-8256-01F8B6DF0A1D");
		if(Directory.Exists(_folder)) Directory.Delete(_folder,true);
		Directory.CreateDirectory(_folder);
		
	}

	[TearDown]
	public void Cleanup() {
		Environment.CurrentDirectory = Path.GetPathRoot(_folder);
		// BUG Directory.Delete(_folder,true); // https://github.com/dotnet/runtime/issues/86249
		
		foreach (var entry in Directory.EnumerateDirectories(_folder)) {
			Directory.Delete(entry,true);
		}
		Directory.Delete(_folder,true);
	}

	[Test]
	public void MkLink_Hardlink() {
		var symlinkFileName = _folder+"\\link";
		var targetFileName = _folder+"\\target";
		var t1 = "MkLink_Hardlink";

		CreteFile(targetFileName, t1);
		Commands.MkLink(symlinkFileName,targetFileName,MkLinkType.Hardlink);

		Assert.That(File.Exists(symlinkFileName), Is.True);
		Assert.That(ReadFile(symlinkFileName), Is.EqualTo(t1));
	}

	[Test]
	public void MkLink_SymLink() {
		var symlinkFileName = _folder+"\\link";
		var targetFileName = _folder+"\\target";
		var t1 = "MkLink_SymLink";

		CreteFile(targetFileName, t1);
		Commands.MkLink(symlinkFileName, targetFileName, MkLinkType.Default);

		Assert.That(File.Exists(symlinkFileName), Is.True);
		Assert.That(ReadFile(symlinkFileName), Is.EqualTo(t1));
	}

	[Test]
	public void MkLink_Junction() {
		var symlinkFileName = _folder+"\\link";
		var targetFileName = _folder+"\\target";
		Directory.CreateDirectory(targetFileName);
		var t1 = "MkLink_Junktion";

		CreteFile(targetFileName+"\\test", t1);
		Commands.MkLink(symlinkFileName, targetFileName, MkLinkType.Junktion);

		Assert.That(File.Exists(symlinkFileName+"\\test"), Is.True);
		Assert.That(ReadFile(symlinkFileName+"\\test"), Is.EqualTo(t1));
	}

	private string ReadFile(string filename) {
		using var f = File.OpenText(filename);
		return f.ReadToEnd();
	}

	private void CreteFile(string fileName, string content) {
		using var f = File.CreateText(fileName);
		f.Write(content);
	}

}
