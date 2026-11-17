namespace PrReviewAssistant.Tests;

/// <summary>Small diffs used by the tests, in the same format as <c>git diff</c> output.</summary>
internal static class SampleDiffs
{
    /// <summary>A guard clause plus a matching unit test. Note that git's hunk header names the wrong method (Add).</summary>
    public const string MinorFix = """
        diff --git a/src/Sample.DotNetApp/MathService.cs b/src/Sample.DotNetApp/MathService.cs
        index 42388bb..6ec6fe3 100644
        --- a/src/Sample.DotNetApp/MathService.cs
        +++ b/src/Sample.DotNetApp/MathService.cs
        @@ -9,6 +9,11 @@ public double Add(double a, double b)
             public double Divide(double dividend, double divisor)
             {
        +        if (divisor == 0)
        +        {
        +            throw new DivideByZeroException("Divisor must not be zero.");
        +        }
        +
                 return dividend / divisor;
             }
         }
        diff --git a/tests/Sample.DotNetApp.Tests/MathServiceTests.cs b/tests/Sample.DotNetApp.Tests/MathServiceTests.cs
        index 36e0bce..71e23de 100644
        --- a/tests/Sample.DotNetApp.Tests/MathServiceTests.cs
        +++ b/tests/Sample.DotNetApp.Tests/MathServiceTests.cs
        @@ -11,4 +11,10 @@ public void Divide_ReturnsQuotient()
             {
                 Assert.Equal(2, _service.Divide(10, 5));
             }
        +
        +    [Fact]
        +    public void Divide_ByZero_Throws()
        +    {
        +        Assert.Throws<DivideByZeroException>(() => _service.Divide(10, 0));
        +    }
         }
        """;

    /// <summary>A new image and a file renamed without content changes, as git prints them.</summary>
    public const string RenameAndBinary = """
        diff --git a/logo.png b/logo.png
        new file mode 100644
        index 0000000..f584f40
        Binary files /dev/null and b/logo.png differ
        diff --git a/src/MathService.cs b/src/Calculator.cs
        similarity index 100%
        rename from src/MathService.cs
        rename to src/Calculator.cs
        """;

    /// <summary>A change to a single file, with no tests.</summary>
    public static string SingleFile(string path, params string[] addedLines) => $"""
        diff --git a/{path} b/{path}
        --- a/{path}
        +++ b/{path}
        @@ -1,1 +1,{addedLines.Length + 1} @@
         // existing line
        {string.Join("\n", addedLines.Select(line => "+" + line))}
        """;
}
