// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.PublicPartialProgramClassAnalyzer,
    Microsoft.AspNetCore.Fixers.PublicPartialProgramClassFixer>;

namespace Microsoft.AspNetCore.Analyzers;

public class PublicPartialProgramClassTest
{
    [Fact]
    public async Task DoesNothingWhenNoDeclarationIsFound()
    {
        // Arrange
        var source = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();
""";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Theory]
    [InlineData("public partial class Program { }")]
    [InlineData("public partial class Program { /* This is just for tests */ }")]
    [InlineData("public partial class Program { \n // This is just for tests \n }")]
    [InlineData("public partial class Program;")]
    public async Task RemovesDeclarationIfItIsFound(string declarationStyle)
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

{|#0:{{declarationStyle}}|}
""";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.PublicPartialProgramClassNotRequired)
                .WithLocation(0);

        var fixedSource = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

""";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, [diagnostic], fixedSource);
    }

    [Fact]
    public async Task DoesNotGeneratesSource_IfProgramDeclaresExplicitInternalAccess()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

internal partial class Program { }
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task DoesNotFix_ExplicitPublicProgramClass()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

public class Program
{
    public static void Main()
    {
        var app = WebApplication.Create();

        app.MapGet("/", () => "Hello, World!");

        app.Run();
    }
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task DoesNotFix_ExplicitInternalProgramClass()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

internal class Program
{
    public static void Main()
    {
        var app = WebApplication.Create();

        app.MapGet("/", () => "Hello, World!");

        app.Run();
    }
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Theory]
    [InlineData("interface")]
    [InlineData("struct")]
    public async Task DoesNotFix_ExplicitInternalProgramType(string type)
    {
        var source = $$"""
using Microsoft.AspNetCore.Builder;

internal {{type}} Program
{
    public static void Main(string[] args)
    {
        var app = WebApplication.Create();

        app.MapGet("/", () => "Hello, World!");

        app.Run();
    }
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }
}

