namespace KsWare.CmdDotNet.Tests;

[TestFixture]
public class CommandLineArgsBaseTests {

	private static readonly string Location = typeof(CommandLineArgsBase).Assembly.Location;

	[Test]
	public void BoolArg() {
		var sut = new CommandLineArgs([Location,"-b"]);
		Assert.That(sut.Success, Is.True);
		Assert.That(sut.B, Is.True);
	}

	[Test]
	public void StringArgParameter() {
		var sut = new CommandLineArgs([Location,"-s", "foo"]);
		Assert.That(sut.Success, Is.True);
		Assert.That(sut.S, Is.EqualTo("foo"));
	}

	[Test]
	public void StringArg() {
		var sut = new CommandLineArgs([Location,"-s=foo"]);
		Assert.That(sut.Success, Is.True);
		Assert.That(sut.S, Is.EqualTo("foo"));
	}

	[Test]
	public void OptionalArgParameter() {
		var sut = new CommandLineArgs([Location,"-O"]);
		Assert.That(sut.Success, Is.True);
		Assert.That(sut.O, Is.EqualTo("default"));
	}

	[Test]
	public void IntegerArgParameter() {
		var sut = new CommandLineArgs([Location,"-i","1"]);
		Assert.That(sut.Success, Is.True);
		Assert.That(sut.I, Is.EqualTo(1));
	}

	[Test]
	public void MissingParameter() {
		var sut = new CommandLineArgs([Location,"-s"]);
		Assert.That(sut.Success, Is.False);
		Assert.That(sut.Error, Is.Not.Null);
	}

	[Test]
	public void InvalidType() {
		var sut = new CommandLineArgs([Location,"-i","a"]);
		Assert.That(sut.Success, Is.False);
		Assert.That(sut.Error, Is.Not.Null);
		Console.WriteLine(sut.Error);
	}
}

public class CommandLineArgs : CommandLineArgsBase {

	public CommandLineArgs() : base(){ }
	public CommandLineArgs(string[] args) : base(args) {}

	[CommandLineSwitch]
	public bool B { get; set; }

	[CommandLineSwitch]
	public string S { get; set; }

	[CommandLineSwitch("-i")]
	public int I { get; set; }

	[CommandLineSwitch("-o", Parameter = "[<value>]", ParameterValue = "default")]
	public string O { get; set; }

}