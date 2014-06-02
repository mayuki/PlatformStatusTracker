using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.ServiceLocation;

namespace PlatformStatusTracker.Web.Infrastracture
{
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// ServiceLocatorを取得します。
        /// </summary>
        public IServiceLocator ServiceLocator { get { return Microsoft.Practices.ServiceLocation.ServiceLocator.Current; } }


        /// <summary>
        /// ServiceLocatorを利用して指定された型のハンドラのインスタンスを取得します。
        /// ハンドラのコンストラクタのパラメータはServiceLocatorが自動的にセットします。
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected THandler Using<THandler>() where THandler : class
        {
            return this.ServiceLocator.GetInstance<THandler>();
        }

    }
}