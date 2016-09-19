using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    /// <summary>
    /// 自動リトライを実装するためのインタフェース
    /// </summary>
    public interface IAutoRetry
    {
        /// <summary>
        /// エラー発生時に呼び出されます。リトライする場合はtrueを返します。
        /// </summary>
        /// <param name="method">リトライするメソッド</param>
        /// <param name="ex">発生した例外</param>
        /// <param name="count">リトライ回数</param>
        /// <returns>リトライする場合はtrue</returns>
        bool CatchError(MethodBase method, Exception ex, int count);
    }
}
