# Wagner.NamingStyles [BETA]
> [!WARNING]
> Please back up your projects before trying.
> 
> Known Issues:  
> - If an event in a `UserControl` references a misnamed method in the code behind, the method will be renamed in code behind but not in xaml. This behavior is not present in Microsoft's fixer.
> - In large projects or solutions, it may incorrectly rename the symbol. It is unclear exactly why but for a large solution it happened for ~120 symbols out of the 1721. Running it again on the document scale fixed those but if the new, incorrect, name conflicts with a different member, it can can be annoying.

> [!IMPORTANT]  
> Just like Microsoft's built-in IDE1006 fixer, it will rename a symbol even if the new name is already being used causing CS0102 and CS0229 errors.

## This extension enables FixAll for naming style violations (IDE1006).
When installed, there should be an additional option to fix naming style violations but this one has the ability to fix all enabled.

![image](https://github.com/mwagnerEE/Wagner.NamingStyles/assets/58664961/89fb796e-7776-4c4f-9e7f-95ef68080104)

## Implementation
The code fix works by simulating a simple code change on Microsoft's `NamingStyleCodeFixProvider` so instead of:

```cs
public override FixAllProvider? GetFixAllProvider()
{
    // Currently Fix All is not supported for naming style violations.
    return null;
}
```
It is:

```cs
public override FixAllProvider? GetFixAllProvider()
{
    // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
    return WellKnownFixAllProviders.BatchFixer;
}
```

And then overriding `GetChangedSolutionAsync` in `FixNameCodeAction`:

```cs
protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
{
    return await FixAsync(_startingSolution, _options, _symbol, _newName, cancellationToken);
}

protected static async Task<Solution> FixAsync(Solution startingSolution, OptionSet options, ISymbol symbol, string fixedName, CancellationToken cancellationToken)
{
    return await Renamer.RenameSymbolAsync(startingSolution, symbol, fixedName, options, cancellationToken).ConfigureAwait(false);
}
```


However, while it would be simple for them, my code fix does not have direct access to the services needed so I had to use reflection. The upshot is that because it's using Microsoft's built in language services, it is able to read the naming style preferences defined in Visual Studio without the need for a separate preference file.

I've tried it and it seems quite performant on small projects but I didn't do a lot of large project testing because I wanted to get this out for Christmas. Let me know if you run into any issues or think you can improve upon it.

### Note:
I created a test project but I didn't have the time to figure out how to apply code fixes using MsBuild so just ignore it.
