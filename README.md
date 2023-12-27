# Wagner.NamingStyles [BETA]
> [!WARNING]
> Please back up your projects before trying.
> 
> Known Issues:  
> - If an event in a `UserControl` references a misnamed method in the code behind, the method will be renamed in code behind but not in xaml. This behavior is not present in Microsoft's fixer.

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

### Note:
I created a test project but I didn't have the time to figure out how to apply code fixes using MsBuild so just ignore it.
