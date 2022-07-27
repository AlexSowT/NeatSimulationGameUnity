namespace Src
{
    public interface IInput
    {
        int Id { get; }
        public float GetInputValue(AgentController agent);
    }
}