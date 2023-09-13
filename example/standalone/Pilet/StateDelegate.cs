namespace Pilet;

public class StateDelegate : IStateDelegate
{
    public SaveSample SaveSample { get; set; } = new SaveSample();
}

public interface IStateDelegate
{
    SaveSample SaveSample { get; set; }
}
