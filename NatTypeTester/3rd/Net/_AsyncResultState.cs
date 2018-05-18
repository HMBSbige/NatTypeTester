using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LumiSoft.Net
{
    /// <summary>
    /// (For internal use only). This class provides holder for IAsyncResult interface and extends it's features.
    /// </summary>
    internal class AsyncResultState : IAsyncResult
    {
        private object        m_pAsyncObject   = null;
        private Delegate      m_pAsyncDelegate = null;
        private AsyncCallback m_pCallback      = null;
        private object        m_pState         = null;
        private IAsyncResult  m_pAsyncResult   = null;
        private bool          m_IsEndCalled    = false;
                
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="asyncObject">Caller's async object.</param>
        /// <param name="asyncDelegate">Delegate which is called asynchronously.</param>
        /// <param name="callback">Callback to call when the connect operation is complete.</param>
        /// <param name="state">User data.</param>
        public AsyncResultState(object asyncObject,Delegate asyncDelegate,AsyncCallback callback,object state)
        {
            m_pAsyncObject   = asyncObject;
            m_pAsyncDelegate = asyncDelegate;
            m_pCallback      = callback;
            m_pState         = state;
        }


        #region mehtod SetAsyncResult

        /// <summary>
        /// Sets AsyncResult value.
        /// </summary>
        /// <param name="asyncResult">Asycnhronous result to wrap.</param>
        public void SetAsyncResult(IAsyncResult asyncResult)
        {
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }

            m_pAsyncResult = asyncResult;
        }

        #endregion

        #region method CompletedCallback

        /// <summary>
        /// This method is called by AsyncDelegate when asynchronous operation completes. 
        /// </summary>
        /// <param name="ar">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        public void CompletedCallback(IAsyncResult ar)
        {
            if(m_pCallback != null){
                m_pCallback(this);
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets caller's async object.
        /// </summary>
        public object AsyncObject
        {
            get{ return m_pAsyncObject; }
        }

        /// <summary>
        /// Gets delegate which is called asynchronously.
        /// </summary>
        public Delegate AsyncDelegate
        {
            get{ return m_pAsyncDelegate; }
        }

        /// <summary>
        /// Gets source asynchronous result what we wrap.
        /// </summary>
        public IAsyncResult AsyncResult
        {
            get{ return m_pAsyncResult; }
        }

        /// <summary>
        /// Gets if the user called the End*() method.
        /// </summary>
        public bool IsEndCalled
        {
            get{ return m_IsEndCalled; }

            set{ m_IsEndCalled = value; }
        }


        /// <summary>
        /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
        /// </summary>
        public object AsyncState 
        { 
            get { return m_pState; } 
        }

        /// <summary>
        /// Gets a WaitHandle that is used to wait for an asynchronous operation to complete.
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get{ return m_pAsyncResult.AsyncWaitHandle; }
        }

        /// <summary>
        /// Gets an indication of whether the asynchronous operation completed synchronously.
        /// </summary>
        public bool CompletedSynchronously
        { 
            get{ return m_pAsyncResult.CompletedSynchronously; }
        }

        /// <summary>
        /// Gets an indication whether the asynchronous operation has completed.
        /// </summary>
        public bool IsCompleted
        {
            get{ return m_pAsyncResult.IsCompleted; } 
        }


        #endregion

    }
}
