namespace Src
{
    public class SpeedInput : IInput
    {
        public int Id => 3;

        public float GetInputValue(AgentController agent)
        {
            return agent.speed;
        }
    }
}