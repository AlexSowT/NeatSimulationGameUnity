namespace Src
{
    public static class ExtensionMethods
    {
        public static decimal Map (this decimal value, decimal fromSource, decimal toSource, decimal fromTarget, decimal toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

    }
}