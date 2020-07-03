using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class AppTextSource : TextSource
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract void Init();
        /// <summary>
        /// 
        /// </summary>
        public abstract int Length { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public abstract GenericTextRunProperties BaseProps { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract BasicTextRunProperties BasicProps();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="InsertionPoint"></param>
        /// <param name="text"></param>
        public abstract void TextInput(int InsertionPoint, string text);
    }
}