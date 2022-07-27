namespace Src
{
    public class PositionYInput : IInput
    {
        public int Id => 1;

        public float GetInputValue(AgentController agent)
        {
            return agent.transform.position.y;
        }
    }
}