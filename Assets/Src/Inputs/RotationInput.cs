namespace Src
{
    public class RotationInput : IInput
    {
        public int Id => 2;
        
        public float GetInputValue(AgentController agent)
        {
            return agent.transform.rotation.normalized.z;
        }
    }
}