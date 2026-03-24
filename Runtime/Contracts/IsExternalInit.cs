// Unitys BCL subset does not include System.Runtime.CompilerServices.IsExternalInit, which the C# 9 compiler requires to compile init setters and record types.
// Defining it here as a public type is the standard polyfill approach the compiler only needs the type to exist somewhere in the compilation unit. it does not need to be the official BCL version.
//
// This file must live in a Runtime assembly so it is available when compiling any assembly that references the VoidWireInteractive.Messaging.Core and uses record or init syntax.
//
// Reference: https://docs.unity3d.com/2021.2/Documentation/Manual/CSharpCompiler.html (I know, its for an older version but it still works so shut up lol. Most people will probably never read this far into this code but if you do, hi :D)

namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}
