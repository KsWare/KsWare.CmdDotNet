# KsWare.CmdDotNet

- .Net methods for known console commands 
    - echo, prompt, pause, ...
    - mklink, robocopy, ...
- strongly typed command line args (CommandLineArgs class, with CommandLineSwitchAttributes)

## .Net Methods for known console commands 
- Echo, Get, Set, Start, PopD, PushD, Pause
- PauseIf, Prompt
- MkLink, Robocopy

### Echo(string text, TextWriter? redirect=null)
```csharp
// echo Hello World
Echo("Hello World")

// echo Hello World>>foo.txt
Echo("Hello World", ToFile("foo.txt", append:true))
```

# Set(string environmentVariable, string value)

```csharp
// set GREETINGS=Hello World
Set("GREETINGS", "Hello World")

// set /p GREETINGS=Ask Question?
Set("GREETINGS", Prompt("Ask Question?"))
```


## Strongly typed command line args

Using `CommandLineArgsBase` includes the parser.

```csharp
public CommandLineArgs : CommandLineArgsBase {

    [CommandLineSwitch("-?","/?","--help")]
    public bool IsHelp { get; set; }

    [CommandLineSwitch("-n", Desription="A number")]
    public int Number { get; set; }

    [CommandLineSwitch("-b", Description="A bool value")]
    public bool MyBool { get; set; }

    [CommandLineSwitch("-o", Parameter="[<file-name>]", Desription="Optional value")]
    public string Output { get; set; }
}
```

Auto generated help text:
```
Options:
  -? /? --help      This help
  -n <number>       A number
  -b                A bool value
  -o [<file-name>]  Optional value
```