namespace OxyPlot.Blazor.Server.Demo;
using ExampleLibrary;
public record IndexedExample(int Index, ExampleInfo Example);
public class ExampleService
{
    readonly List<IndexedExample> _examples;

    public ExampleService()
    {
        _examples = Examples.GetList().Select((e, i) => new IndexedExample(i, e)).ToList();
    }
    public IReadOnlyList<IndexedExample> Values => _examples;

    public ExampleInfo? this[int index] => index >= 0 && index < _examples.Count ? _examples[index].Example : null;
}
