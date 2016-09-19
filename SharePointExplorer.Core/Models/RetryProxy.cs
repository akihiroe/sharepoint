using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Services;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public class RetryProxy : RealProxy
    {

        MarshalByRefObject targetObject;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">プロキシを提供する元オブジェクト</param>
        public RetryProxy(MarshalByRefObject target)
            : this(target.GetType(), target)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="targetType">元オブジェクトのクラスやインターフェース</param>
        /// <param name="target">プロキシを提供する元オブジェクト</param>
        public RetryProxy(Type targetType, MarshalByRefObject target)
            : base(targetType)
        {
            this.targetObject = target;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="targetType">プロキシを提供する元オブジェクトの型</param>
        public RetryProxy(Type targetType)
            : base(targetType)
        {
        }

        /// <summary>
        /// プロキシの元オブジェクト
        /// </summary>
        /// <remarks>
        /// publicで公開しているのはProxyAttributeの実装で直接コンストラクタに
        /// TargetObjectをオブジェクトを渡すとことができないためです
        /// オブジェクトのインスタンスのタイミングでプロキシにアクセスしてしまう
        /// </remarks>
        public MarshalByRefObject TargetObject
        {
            get { return targetObject; }
            set { targetObject = value; }
        }

        /// <summary>
        /// メソッドの実行
        /// </summary>
        /// <param name="msg">メソッドメッセージ</param>
        /// <returns>戻り値メッセージ</returns>
        public override IMessage Invoke(IMessage msg)
        {
            if (msg is IConstructionCallMessage)
            {
                IConstructionCallMessage constMethod = (IConstructionCallMessage)msg;
                RemotingServices.GetRealProxy(this.targetObject).InitializeServerObject(constMethod);
                MarshalByRefObject tp = (MarshalByRefObject)this.GetTransparentProxy();
                IConstructionReturnMessage mrm = EnterpriseServicesHelper.CreateConstructionReturnMessage(constMethod, tp);
                return mrm;
            }

            if (msg is IMethodCallMessage)
            {
                //IsSpecialNameの場合はそのまま呼び出すほうが良いか
                //検討したが頻繁に呼び出されるようなことも断定できないため
                //特別なことはしない
                return InvokeMethod(msg);
            }
            return null;
        }

        /// <summary>
        /// メソッドの呼び出し
        /// </summary>
        /// <param name="msg">メソッドメッセージ</param>
        /// <returns>戻り値メッセージ</returns>
        protected virtual IMessage InvokeMethod(IMessage msg)
        {
            IMethodCallMessage mcm = (IMethodCallMessage)msg;
            IMethodReturnMessage mrm = null;

            //プロパティの場合はバイパスする
            if (mcm.MethodName.StartsWith("set_") || mcm.MethodName.StartsWith("get_"))
            {
                return RemotingServices.ExecuteMessage(this.targetObject, mcm);
            }

            IAutoRetry retry = targetObject as IAutoRetry;
            int retryCount = 0;
            bool exit;
            do
            {
                exit = true;
                try
                {
                    var sw = Stopwatch.StartNew();
                    //                    Debug.WriteLine("Start:"+ mcm.MethodName);

                    mrm = RemotingServices.ExecuteMessage(this.targetObject, mcm);
                    if (mrm.Exception != null)
                    {
                        if (retry != null)
                        {
                            if (retry.CatchError(mcm.MethodBase, mrm.Exception, retryCount))
                            {
                                exit = false;
                                retryCount++;
                            }
                        }

                    }
                    //                  Debug.WriteLine("End:"+ mcm.MethodName + " " + sw.Elapsed.ToString());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception:" + ex.ToString());
                    if (retry != null)
                    {
                        if (retry.CatchError(mcm.MethodBase, ex, retryCount))
                        {
                            exit = false;
                            retryCount++;
                        }
                    }
                    mrm = new ReturnMessage(ex, mcm);
                }
            }
            while (!exit);
            return mrm;
        }

    }

}
