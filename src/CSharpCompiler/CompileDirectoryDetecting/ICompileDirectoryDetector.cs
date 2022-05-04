namespace CSharpCompiler.CompileDirectoryDetecting;

internal interface ICompileDirectoryDetector
{
    CompileDirectoryDetectResult Detect(string[] fileContents, bool allowUnsafe);
}