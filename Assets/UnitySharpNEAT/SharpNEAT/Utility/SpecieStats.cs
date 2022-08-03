namespace SharpNeat.Utility
{
    class SpecieStats
    {
        // Real/continuous stats.
        public double _meanFitness;
        public double _targetSizeReal;

        // Integer stats.
        public int _targetSizeInt;
        public int _eliteSizeInt;
        public int _offspringCount;
        public int _offspringAsexualCount;
        public int _offspringSexualCount;
      
        // Selection data.
        public int _selectionSizeInt;
    }
}