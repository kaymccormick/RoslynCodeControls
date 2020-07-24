namespace RoslynCodeControls
{
    public class ParametersBase: IDebugParam
    {
        protected ParametersBase(RoslynCodeBase.DebugDelegate debugFn)
        {
            DebugFn = debugFn;
        }

        public double PixelsPerDip { get; set; }
        public double EmSize0 { get; set; }

        /// <inheritdoc />
        public RoslynCodeBase.DebugDelegate DebugFn { get; }
    }
}