#region header
// Kay McCormick (mccor)
// 
// Proj
// AnalysisControls
// Converter1Param.cs
// 
// 2020-03-03-7:22 PM
// 
// ---
#endregion
namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public enum SyntaxNodeInfo
    {
        /// <summary>The ancestors</summary>
        Ancestors
      ,

       /// <summary>
       /// Get ancestors and self.
       /// </summary>
       AncestorsAndSelf
      ,

       /// <summary>
       /// Get first token.
       /// </summary>
       GetFirstToken
      ,

       /// <summary>
       /// 
       /// </summary>
       GetLocation
      ,

       /// <summary>
       /// 
       /// </summary>
       GetLastToken
      ,

       /// <summary>
       /// 
       /// </summary>
       GetReference
      ,

       /// <summary>
       /// 
       /// </summary>
       GetText
      ,

       /// <summary>
       /// 
       /// </summary>
       ToFullString
      ,

       /// <summary>
       /// 
       /// </summary>
       ToString
      ,

       /// <summary>
       /// 
       /// </summary>
       Kind
      ,

       /// <summary>
       /// 
       /// </summary>
       ChildNodesAndTokens
      ,

       /// <summary>
       /// 
       /// </summary>
       ChildNodes
      ,

       /// <summary>
       /// 
       /// </summary>
       ChildTokens
      ,

       /// <summary>
       /// 
       /// </summary>
       DescendantNodes
      ,

       /// <summary>
       /// 
       /// </summary>
       DescendantNodesAndSelf
      ,

       /// <summary>
       /// 
       /// </summary>
       DescendantNodesAndTokens
      ,

       /// <summary>
       /// 
       /// </summary>
       DescendantNodesAndTokensAndSelf
      ,


       /// <summary>
       /// 
       /// </summary>
       DescendantTokens
      ,

       /// <summary>
       /// 
       /// </summary>
       DescendantTrivia
      ,

       /// <summary>
       /// 
       /// </summary>
       GetLeadingTrivia
      ,

       /// <summary>
       /// 
       /// </summary>
       Diagnostics
    }
}