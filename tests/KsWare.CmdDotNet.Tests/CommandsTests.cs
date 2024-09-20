namespace KsWare.CmdDotNet.Tests;

[TestFixture]
public class CommandsTests {

	private string _restoreDir;
	private StringWriter _consoleOut;
	private StringWriter _consoleError;
	private TextWriter _restoreConsoleOut;
	private TextWriter _restoreConsoleError;

	[SetUp]
	public void SetUp() {
		_restoreDir = Environment.CurrentDirectory;
		_restoreConsoleOut = Console.Out;
		Console.SetOut(_consoleOut = new StringWriter());
		_restoreConsoleError = Console.Error;
		Console.SetError(_consoleError = new StringWriter());
	}

	private string ConsoleOutText => _consoleOut.ToString();
	public Action? TestTearDown { get; set; }

	[TearDown]
	public void TearDown() {
		Environment.CurrentDirectory = _restoreDir;
		Console.SetOut(_restoreConsoleOut);
		Console.SetError(_restoreConsoleError);
		TestTearDown?.Invoke();
		TestTearDown = null;
	}

	[Test]
	public void Echo_writeToConsole() {
		Commands.Echo("Hello World");
		Assert.That(ConsoleOutText, Is.EqualTo("Hello World"+Environment.NewLine));
	}
	
	[Test]
	public void Echo_writeEnvVarToConsole() {
		Commands.Echo("%USERNAME%");
		Assert.That(ConsoleOutText, Is.EqualTo(Environment.UserName+Environment.NewLine));
	}

	[Test]
	public void Cd_writeToConsole() {
		Commands.Cd();
		Assert.That(ConsoleOutText, Is.EqualTo(Environment.CurrentDirectory+Environment.NewLine));
	}

	[Test]
	public void Get() {
		Assert.That(Commands.Get("USERNAME"), Is.EqualTo(Environment.UserName));
	}

	[Test]
	public void Prompt() {
		var restoreConsoleIn = Console.In;
		Console.SetIn(new StringReader("Hello" + Environment.NewLine));
		try {
			var v = Commands.Prompt("Ask:");
			Assert.That(ConsoleOutText, Is.EqualTo("Ask:"));
			Assert.That(v, Is.EqualTo("Hello"));
		}
		finally {
			Console.SetIn(restoreConsoleIn);
		}
	}

	[Test]
	public void Set() {
		var v = $"A{new Random().Next(100000, 99999 + 1)}";
		Commands.Set("_prompt", v);
		Assert.That(Environment.GetEnvironmentVariable("_prompt"), Is.EqualTo(v));
	}

	[Test]
	public void SetPrompt() {
		var restoreConsoleIn = Console.In;
		TestTearDown=new Action(() => {
			Console.SetIn(restoreConsoleIn);
		});
		Console.SetIn(new StringReader("Hello" + Environment.NewLine));

		Commands.Set("_prompt", Commands.Prompt("Ask:"));
		Assert.That(Environment.GetEnvironmentVariable("_prompt"), Is.EqualTo("Hello"));
	}
}