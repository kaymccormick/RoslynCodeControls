namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILineDrawer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lineContext"></param>
        /// <param name="clear"></param>
        void PrepareDrawLines(LineContext lineContext, bool clear);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lineContext"></param>
        void PrepareDrawLine(LineContext lineContext);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lineContext"></param>
        void DrawLine(LineContext lineContext);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lineContext"></param>
        void EndDrawLines(LineContext lineContext);
    }
}