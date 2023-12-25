# Wagner.NamingStyles [BETA]
> [!WARNING]
> Please back up your projects before trying. I haven't run into any problems but I only tested it on small projects

## This extension enables FixAll for naming style violations (IDE1006).
When installed, there should be an additional option to fix naming style violations but this one has the ability to fix all enabled.

![image](https://github.com/mwagnerEE/Wagner.NamingStyles/assets/58664961/89fb796e-7776-4c4f-9e7f-95ef68080104)

## Implementation
The code fix works by simulating a single code change on Microsoft's `NamingStyleCodeFixProvider` so instead of:

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

However, while it would be simple for them, my code fix does not have direct access to the services needed so I had to use reflection. The upshot is that because it's using Microsoft's built in language services, it is able to read the naming style preferences defined in Visual Studio without the need for a separate preference file.

I've tried it and it seems quite performant on small projects but I didn't do a lot of large project testing because I wanted to get this out for Christmas. Let me know if you run into any issues or think you can improve upon it.

### Note:
I created a test project but I didn't have the time to figure out how to apply code fixes using MsBuild so just ignore it.