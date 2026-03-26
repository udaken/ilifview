namespace ilifview;

interface IOutputFormatter
{
    void Write(AssemblyInfo assembly, TextWriter output);
}
