using System;
using SharpNeat.Decoders;

namespace Src
{
    [Serializable]
    public class Config
    {
        /*<ExperimentName>MovingAgent2</ExperimentName>
    <PopulationSize>10</PopulationSize>
    <SpecieCount>3</SpecieCount>
    <Activation>
    <Scheme>Acyclic</Scheme>
    </Activation>
    <ComplexityRegulationStrategy>Absolute</ComplexityRegulationStrategy>
    <ComplexityThreshold>10</ComplexityThreshold>
    <Description>Cars learn to race around a race track</Description>*/
        public string ExperimentName { get; set; }
        public int PopulationSize { get; set; }
        public int SpecieCount { get; set; }
        public NetworkActivationScheme NetworkActivationScheme { get; set; }

    }
}