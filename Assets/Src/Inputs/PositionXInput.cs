namespace Src
{
    public class PositionXInput : IInput
    {
        public int Id => 0;

        public float GetInputValue(AgentController agent)
        {
            // TODO:: Think if this should be normalized or not
            return agent.transform.position.x;
        }
    }
}