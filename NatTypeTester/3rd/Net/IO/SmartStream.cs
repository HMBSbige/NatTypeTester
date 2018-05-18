using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using LumiSoft.Net.Log;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// This class is wrapper to normal stream, provides most needed stream methods which are missing from normal stream.
    /// </summary>
    public class SmartStream : Stream
    { 
        private delegate void BufferCallback(Exception x);
                
        #region class ReadLineAsyncOP

        /// <summary>
        /// This class implements read line operation.
        /// </summary>
        /// <remarks>This class can be reused on multiple calls of <see cref="SmartStream.ReadLine(ReadLineAsyncOP,bool)">SmartStream.ReadLine</see> method.</remarks>
        public class ReadLineAsyncOP : AsyncOP,IDisposable
        {
            private bool               m_IsDisposed      = false;
            private bool               m_IsCompleted     = false;
            private bool               m_IsCompletedSync = false;
            private SmartStream        m_pOwner          = null;
            private byte[]             m_pBuffer         = null;
            private SizeExceededAction m_ExceededAction  = SizeExceededAction.JunkAndThrowException;
            private bool               m_CRLFLinesOnly   = true;
            private int                m_BytesInBuffer   = 0;
            private int                m_LastByte        = -1;
            private Exception          m_pException      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="buffer">Line buffer.</param>
            /// <param name="exceededAction">Specifies how line-reader behaves when maximum line size exceeded.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
            public ReadLineAsyncOP(byte[] buffer,SizeExceededAction exceededAction)
            {
                if(buffer == null){
                    throw new ArgumentNullException("buffer");
                }

                m_pBuffer        = buffer;
                m_ExceededAction = exceededAction;
            }

            /// <summary>
            /// Destructor.
            /// </summary>
            ~ReadLineAsyncOP()
            {
                Dispose();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_IsDisposed){
                    return;
                }
                m_IsDisposed = true;

                m_pOwner       = null;
                m_pBuffer      = null;
                m_pException   = null;
                this.Completed = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts reading line.
            /// </summary>
            /// <param name="async">If true then this method can complete asynchronously. If false, this method completed always syncronously.</param>
            /// <param name="stream">Owner SmartStream.</param>
            /// <returns>Returns true if read line completed synchronously, false if asynchronous operation pending.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
            internal bool Start(bool async,SmartStream stream)
            {   
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pOwner = stream;

                // Clear old data, if any.
                m_IsCompleted   = false;
                m_BytesInBuffer = 0;
                m_LastByte      = -1;
                m_pException    = null;
   
                m_IsCompletedSync = DoLineReading(async);

                return m_IsCompletedSync;
            }

            #endregion


            #region method Buffering_Completed

            /// <summary>
            /// Is called when asynchronous read buffer buffering has completed.
            /// </summary>
            /// <param name="x">Exception that occured during async operation.</param>
            private void Buffering_Completed(Exception x)
            {   
                if(m_pOwner.m_IsDisposed){
                    return;
                }
            
                if(x != null){
                    m_pException = x;
                    OnCompleted();
                }
                // We reached end of stream, no more data.
                else if(m_pOwner.BytesInReadBuffer == 0){
                    OnCompleted();
                }
                // Continue line reading.
                else{
                    if(DoLineReading(true)){
                        OnCompleted();
                    }
                }
            }

            #endregion

            #region method DoLineReading

            /// <summary>
            /// Starts/continues line reading.
            /// </summary>
            /// <param name="async">If true then this method can complete asynchronously. If false, this method completed always syncronously.</param>
            /// <returns>Returns true if line reading completed.</returns>
            private bool DoLineReading(bool async)
            {
                try{
                    while(true){                        
                        // Read buffer empty, buff next data block.
                        if(m_pOwner.BytesInReadBuffer == 0){
                            // Buffering started asynchronously.
                            if(m_pOwner.BufferRead(async,this.Buffering_Completed)){
                                return false;
                            }
                            // Buffering completed synchronously, continue processing.
                            else{
                                // We reached end of stream, no more data.
                                if(m_pOwner.BytesInReadBuffer == 0){                                    
                                    return true;
                                }
                            }
                        }

                        byte b = m_pOwner.m_pReadBuffer[m_pOwner.m_ReadBufferOffset++];
                        
                        // Line buffer full.
                        if(m_BytesInBuffer >= m_pBuffer.Length){
                            if(m_pException == null){
                                m_pException = new LineSizeExceededException();
                            }

                            if(m_ExceededAction == SizeExceededAction.ThrowException){                                
                                return true;
                            }
                        }
                        // Store byte.
                        else{
                            m_pBuffer[m_BytesInBuffer++] = b;
                        }

                        // We have LF line.
                        if(b == '\n'){
                            if(!m_CRLFLinesOnly || m_CRLFLinesOnly && m_LastByte == '\r'){
                                m_IsCompleted = true;
                                return true;
                            }                   
                        }

                        m_LastByte = b;
                    }
                }
                catch(Exception x){
                    m_pException = x;
                }

                return true;
            }

            #endregion


            #region method SetInfo

            /// <summary>
            /// Sets specified field values.
            /// </summary>
            /// <param name="bytesInBuffer">Number of bytes in buffer.</param>
            /// <param name="exception">Exception.</param>
            internal void SetInfo(int bytesInBuffer,Exception exception)
            {
                m_IsCompleted     = true;
                m_IsCompletedSync = true;
                m_BytesInBuffer   = bytesInBuffer;
                m_pException      = exception;
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets if this object is disposed.
            /// </summary>
            public override bool IsDisposed
            {
                get{ return m_IsDisposed; }
            }

            /// <summary>
            /// Gets if asynchronous operation has completed.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public override bool IsCompleted
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_IsCompleted; 
                }
            }

            /// <summary>
            /// Gets if operation completed synchronously.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public override bool IsCompletedSynchronously
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_IsCompletedSync; 
                }
            }

            /// <summary>
            /// Gets line size exceeded action.
            /// </summary>
            public SizeExceededAction SizeExceededAction
            {
                get{ return m_ExceededAction; }
            }

            /// <summary>
            /// Gets line buffer.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public byte[] Buffer
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pBuffer; 
                }
            }

            /// <summary>
            /// Gets number of bytes stored in the buffer. Ending line-feed characters included.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public int BytesInBuffer
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_BytesInBuffer; 
                }
            }

            /// <summary>
            /// Gets number of line data bytes stored in the buffer. Ending line-feed characters not included.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public int LineBytesInBuffer
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    int retVal = m_BytesInBuffer;

                    if(m_BytesInBuffer > 1){
                        if(m_pBuffer[m_BytesInBuffer - 1] == '\n'){
                            retVal--;
                            if(m_pBuffer[m_BytesInBuffer - 2] == '\r'){
                                retVal--;
                            }
                        }
                    }
                    else if(m_BytesInBuffer > 0){
                        if(m_pBuffer[m_BytesInBuffer - 1] == '\n'){
                            retVal--;
                        }
                    }

                    return retVal; 
                }
            }

            /// <summary>
            /// Gets line as ASCII string. Returns null if EOS(end of stream) reached. Ending line-feed characters not included.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public string LineAscii
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    if(this.BytesInBuffer == 0){
                        return null;
                    }
                    else{
                        return Encoding.ASCII.GetString(m_pBuffer,0,this.LineBytesInBuffer); 
                    }
                }
            }

            /// <summary>
            /// Gets line as UTF-8 string. Returns null if EOS(end of stream) reached. Ending line-feed characters not included.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public string LineUtf8
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    if(this.BytesInBuffer == 0){
                        return null;
                    }
                    else{
                        return Encoding.UTF8.GetString(m_pBuffer,0,this.LineBytesInBuffer);
                    }
                }
            }

            /// <summary>
            /// Gets line as UTF-32 string. Returns null if EOS(end of stream) reached. Ending line-feed characters not included.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public string LineUtf32
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    if(this.BytesInBuffer == 0){
                        return null;
                    }
                    else{
                        return Encoding.UTF32.GetString(m_pBuffer,0,this.LineBytesInBuffer);
                    }
                }
            }

            /// <summary>
            /// Gets error occured during asynchronous operation. Value null means no error.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public Exception Error
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
    
                    return m_pException; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is raised when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<ReadLineAsyncOP>> Completed = null;

            #region method OnCompleted

            /// <summary>
            /// Raises <b>Completed</b> event.
            /// </summary>
            private void OnCompleted()
            {
                m_IsCompleted = true;

                if(this.Completed != null){
                    this.Completed(this,new EventArgs<ReadLineAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        #region class ReadPeriodTerminatedAsyncOP

        /// <summary>
        /// This class implements read period-terminated operation.
        /// </summary>
        public class ReadPeriodTerminatedAsyncOP : AsyncOP,IDisposable
        {
            private bool               m_IsDisposed      = false;
            private bool               m_IsCompleted     = false;
            private bool               m_IsCompletedSync = false;
            private SmartStream        m_pOwner          = null;
            private Stream             m_pStream         = null;
            private long               m_MaxCount        = 0;
            private SizeExceededAction m_ExceededAction  = SizeExceededAction.JunkAndThrowException;
            private ReadLineAsyncOP    m_pReadLineOP     = null;
            private long               m_BytesStored     = 0;
            private int                m_LinesStored     = 0;
            private Exception          m_pException      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="stream">Stream wehre to sore readed data.</param>
            /// <param name="maxCount">Maximum number of bytes to read. Value 0 means not limited.</param>
            /// <param name="exceededAction">Specifies how period-terminated reader behaves when <b>maxCount</b> exceeded.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
            public ReadPeriodTerminatedAsyncOP(Stream stream,long maxCount,SizeExceededAction exceededAction)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pStream        = stream;
                m_MaxCount       = maxCount;
                m_ExceededAction = exceededAction;

                m_pReadLineOP = new ReadLineAsyncOP(new byte[32000],exceededAction);
                m_pReadLineOP.Completed += new EventHandler<EventArgs<ReadLineAsyncOP>>(m_pReadLineOP_Completed);
            }

            /// <summary>
            /// Destructor.
            /// </summary>
            ~ReadPeriodTerminatedAsyncOP()
            {
                Dispose();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_IsDisposed){
                    return;
                }
                m_IsDisposed = true;

                m_pOwner       = null;
                m_pStream      = null;
                m_pReadLineOP.Dispose();
                m_pReadLineOP  = null;
                m_pException   = null;
                this.Completed = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts period-terminated data reading.
            /// </summary>
            /// <param name="stream">Owner SmartStream.</param>
            /// <returns>Returns true if read line completed synchronously, false if asynchronous operation pending.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
            internal bool Start(SmartStream stream)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pOwner = stream;

                // Clear old data, if any.
                m_IsCompleted = false;
                m_BytesStored = 0;
                m_LinesStored = 0;
                m_pException  = null;

                m_IsCompletedSync = DoRead();
   
                return m_IsCompletedSync;
            }

            #endregion


            #region method m_pReadLineOP_Completed

            /// <summary>
            /// Is called when asynchronous line reading has completed.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="e">Event data.</param>
            private void m_pReadLineOP_Completed(object sender,EventArgs<ReadLineAsyncOP> e)
            {
                try{
                    if(ProcessReadedLine()){
                        OnCompleted();
                    }
                    else{
                        if(DoRead()){
                            OnCompleted();
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    OnCompleted();
                }
            }

            #endregion

            #region method DoRead

            /// <summary>
            /// Continues period-terminated reading.
            /// </summary>
            /// <returns>Returns true if read line completed synchronously, false if asynchronous operation pending.</returns>
            private bool DoRead()
            {
                try{
                    while(true){
                        if(m_pOwner.ReadLine(m_pReadLineOP,true)){
                            if(ProcessReadedLine()){
                                break;
                            }
                        }
                        // Goto next while loop.
                        else{
                            return false;
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                }
                                
                return true;
            }

            #endregion

            #region method ProcessReadedLine

            /// <summary>
            /// Processes readed line.
            /// </summary>
            /// <returns>Returns true if read period-terminated operation has completed.</returns>
            private bool ProcessReadedLine()
            {
                if(m_pReadLineOP.Error != null){
                    m_pException = m_pReadLineOP.Error;

                    return true;
                }
                // We reached end of stream, no more data.
                else if(m_pReadLineOP.BytesInBuffer == 0){
                    m_pException = new IncompleteDataException("Data is not period-terminated.");

                    return true;
                }
                // We have period terminator.
                else if(m_pReadLineOP.LineBytesInBuffer == 1 && m_pReadLineOP.Buffer[0] == '.'){
                    return true;
                }
                // Normal line.
                else{
                    if(m_MaxCount < 1 || m_BytesStored < m_MaxCount){
                        // Period handling: If line starts with '.', it must be removed.
                        if(m_pReadLineOP.Buffer[0] == '.'){
                            m_pStream.Write(m_pReadLineOP.Buffer,1,m_pReadLineOP.BytesInBuffer - 1);
                            m_BytesStored += m_pReadLineOP.BytesInBuffer - 1;
                            m_LinesStored++;
                        }
                        // Nomrmal line.
                        else{
                            m_pStream.Write(m_pReadLineOP.Buffer,0,m_pReadLineOP.BytesInBuffer);
                            m_BytesStored += m_pReadLineOP.BytesInBuffer;
                            m_LinesStored++;
                        }                        
                    }
                }

                return false;
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets if this object is disposed.
            /// </summary>
            public override bool IsDisposed
            {
                get{ return m_IsDisposed; }
            }

            /// <summary>
            /// Gets if asynchronous operation has completed.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public override bool IsCompleted
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_IsCompleted; 
                }
            }

            /// <summary>
            /// Gets if operation completed synchronously.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public override bool IsCompletedSynchronously
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_IsCompletedSync; 
                }
            }

            /// <summary>
            /// Gets stream where period terminated data has stored.
            /// </summary>
            public Stream Stream
            {
                get{ 
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pStream; 
                }
            }

            /// <summary>
            /// Gets number of bytes stored to <see cref="Stream">Stream</see> stream.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public long BytesStored
            {
                get{ 
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_BytesStored; 
                }
            }

            /// <summary>
            /// Gets number of lines stored to <see cref="Stream">Stream</see> stream.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public int LinesStored
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_LinesStored; 
                }
            }

            /// <summary>
            /// Gets error occured during asynchronous operation. Value null means no error.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public Exception Error
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
    
                    return m_pException; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is raised when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<ReadPeriodTerminatedAsyncOP>> Completed = null;

            #region method OnCompleted

            /// <summary>
            /// Raises <b>Completed</b> event.
            /// </summary>
            private void OnCompleted()
            {
                m_IsCompleted = true;

                if(this.Completed != null){
                    this.Completed(this,new EventArgs<ReadPeriodTerminatedAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        #region class BufferReadAsyncOP

        /// <summary>
        /// This class implements asynchronous read buffering.
        /// </summary>
        private class BufferReadAsyncOP : AsyncOP,IDisposable
        {
            private bool        m_IsDisposed      = false;
            private bool        m_IsCompleted     = false;            
            private bool        m_IsCompletedSync = false;
            private SmartStream m_pOwner          = null;
            private byte[]      m_pBuffer         = null;
            private int         m_MaxCount        = 0;
            private int         m_BytesInBuffer   = 0;
            private Exception   m_pException      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="owner">Owner stream.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            public BufferReadAsyncOP(SmartStream owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pOwner = owner;
            }

            /// <summary>
            /// Destructor.
            /// </summary>
            ~BufferReadAsyncOP()
            {
                Dispose();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_IsDisposed){
                    return;
                }
                m_IsDisposed = true;

                m_pOwner       = null;
                m_pBuffer      = null;
                this.Completed = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts asynchronous operation.
            /// </summary>
            /// <param name="async">If true then this method can complete asynchronously. If false, this method completed always syncronously.</param>
            /// <param name="buffer">Buffer where to store readed data.</param>
            /// <param name="count">Maximum number of bytes to read.</param>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
            /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            /// <returns>Returns true if operation completed synchronously, false if asynchronous operation pending.</returns>
            internal bool Start(bool async,byte[] buffer,int count)
            {
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(buffer == null){
                    throw new ArgumentNullException("buffer");
                }
                if(count < 0){
                    throw new ArgumentException("Argument 'count' value must be >= 0.");
                }
                if(count > buffer.Length){
                    throw new ArgumentException("Argumnet 'count' value must be <= buffer.Length.");
                }

                m_IsCompleted   = false;
                m_pBuffer       = buffer;
                m_MaxCount      = count;
                m_BytesInBuffer = 0;
                m_pException    = null;

                // Operation may complete asynchronously;
                if(async){
                    IAsyncResult ar = m_pOwner.m_pStream.BeginRead(buffer,0,count,new AsyncCallback(delegate(IAsyncResult r){
                        try{
                            m_BytesInBuffer = m_pOwner.m_pStream.EndRead(r);
                        }
                        catch(Exception x){
                            m_pException = x;
                        }

                        if(!r.CompletedSynchronously){
                            OnCompleted();
                        }

                        m_IsCompleted = true;
                    }),null);

                    m_IsCompletedSync = ar.CompletedSynchronously;
                }
                // Operation must complete synchronously.
                else{
                    m_BytesInBuffer = m_pOwner.m_pStream.Read(buffer,0,count);
                    m_IsCompleted     = true;
                    m_IsCompletedSync = true;
                }
                                
                return m_IsCompletedSync;
            }

            #endregion

            #region method ReleaseEvents

            /// <summary>
            /// Releases all events attached to this class.
            /// </summary>
            internal void ReleaseEvents()
            {
                this.Completed = null;
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets if this object is disposed.
            /// </summary>
            public override bool IsDisposed
            {
                get{ return m_IsDisposed; }
            }

            /// <summary>
            /// Gets if asynchronous operation has completed.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public override bool IsCompleted
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_IsCompleted; 
                }
            }

            /// <summary>
            /// Gets if operation completed synchronously.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public override bool IsCompletedSynchronously
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_IsCompletedSync; 
                }
            }

            /// <summary>
            /// Gets read buffer.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public byte[] Buffer
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pBuffer; 
                }
            }

            /// <summary>
            /// Gets number of bytes stored in read buffer.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public int BytesInBuffer
            {
                get{ 
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_BytesInBuffer; 
                }
            }

            /// <summary>
            /// Gets error occured during asynchronous operation. Value null means no error.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public Exception Error
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pException; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is raised when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<BufferReadAsyncOP>> Completed = null;

            #region method OnCompleted

            /// <summary>
            /// Raises <b>Completed</b> event.
            /// </summary>
            private void OnCompleted()
            {
                if(this.Completed != null){
                    this.Completed(this,new EventArgs<BufferReadAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        private bool              m_IsDisposed       = false;
        private Stream            m_pStream          = null;
        private bool              m_IsOwner          = false;
        private DateTime          m_LastActivity;
        private long              m_BytesReaded      = 0;
        private long              m_BytesWritten     = 0;
        private int               m_BufferSize       = 32000;
        private byte[]            m_pReadBuffer      = null;
        private int               m_ReadBufferOffset = 0;
        private int               m_ReadBufferCount  = 0;        
        private BufferReadAsyncOP m_pReadBufferOP    = null;
        private Encoding          m_pEncoding        = Encoding.Default;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stream">Stream to wrap.</param>
        /// <param name="owner">Specifies if SmartStream is owner of <b>stream</b>.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        public SmartStream(Stream stream,bool owner)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            
            m_pStream = stream;
            m_IsOwner = owner;
            m_pReadBuffer = new byte[m_BufferSize];
            m_pReadBufferOP = new BufferReadAsyncOP(this);

            m_LastActivity = DateTime.Now;
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public new void Dispose()
        {
            if(m_IsDisposed){
                return;
            }
            m_IsDisposed = true;

            if(m_IsOwner){
                m_pStream.Dispose();
            }
        }

        #endregion
        
                                        
        #region method ReadLine

        /// <summary>
        /// Begins line reading.
        /// </summary>
        /// <param name="op">Read line opeartion.</param>
        /// <param name="async">If true then this method can complete asynchronously. If false, this method completed always syncronously.</param>
        /// <returns>Returns true if read line completed synchronously, false if asynchronous operation pending.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool ReadLine(ReadLineAsyncOP op,bool async)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            #region async

            if(async){
                return op.Start(async,this);
            }

            #endregion

            #region sync

            else{
                byte[]             buffer         = op.Buffer;
                int                bytesInBuffer  = 0;
                int                lastByte       = -1;
                bool               CRLFLinesOnly  = true;
                int                lineBuffSize   = buffer.Length;
                SizeExceededAction exceededAction = op.SizeExceededAction;
                Exception          exception      = null;

                try{
                    while(true){                        
                        // Read buffer empty, buff next data block.
                        if(m_ReadBufferOffset >= m_ReadBufferCount){                        
                            this.BufferRead(false,null);
                        
                            // We reached end of stream, no more data.
                            if(m_ReadBufferCount == 0){                                    
                                break;
                            }                        
                        }

                        byte b = m_pReadBuffer[m_ReadBufferOffset++];
                        
                        // Line buffer full.
                        if(bytesInBuffer >= lineBuffSize){
                            if(exception == null){
                                exception = new LineSizeExceededException();
                            }

                            if(exceededAction == SizeExceededAction.ThrowException){                                
                                break;
                            }
                        }
                        // Store byte.
                        else{
                            buffer[bytesInBuffer++] = b;
                        }

                        // We have LF line.
                        if(b == '\n'){
                            if(!CRLFLinesOnly || CRLFLinesOnly && lastByte == '\r'){
                                break;
                            }
                        }

                        lastByte = b;
                    }
                }
                catch(Exception x){
                    exception = x;
                }

                // Set read line operation result data.
                op.SetInfo(bytesInBuffer,exception);

                return true;
            }

            #endregion
        }

        #endregion

        #region method BeginReadHeader

        /// <summary>
        /// Begins an asynchronous header reading from the source stream.
        /// </summary>
        /// <param name="storeStream">Stream where to store readed header.</param>
        /// <param name="maxCount">Maximum number of bytes to read. Value 0 means not limited.</param>
        /// <param name="exceededAction">Specifies action what is done if <b>maxCount</b> number of bytes has exceeded.</param>
        /// <param name="callback">The AsyncCallback delegate that is executed when asynchronous operation completes.</param>
        /// <param name="state">An object that contains any additional user-defined data.</param>
        /// <returns>An IAsyncResult that represents the asynchronous call.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>storeStream</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IAsyncResult BeginReadHeader(Stream storeStream,int maxCount,SizeExceededAction exceededAction,AsyncCallback callback,object state)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(storeStream == null){
                throw new ArgumentNullException("storeStream");
            }
            if(maxCount < 0){
                throw new ArgumentException("Argument 'maxCount' must be >= 0.");
            }

            return new ReadToTerminatorAsyncOperation(this,"",storeStream,maxCount,exceededAction,callback,state);
        }

        #endregion

        #region method EndReadHeader

        /// <summary>
        /// Handles the end of an asynchronous header reading.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that represents an asynchronous call.</param>
        /// <returns>Returns number of bytes stored to <b>storeStream</b>.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="InvalidOperationException">Is raised when <b>EndReadLine</b> has already been called for specified <b>asyncResult</b>.</exception>
        /// <exception cref="LineSizeExceededException">Is raised when source stream has too big line.</exception>
        /// <exception cref="DataSizeExceededException">Is raised when reading exceeds <b>maxCount</b> specified value.</exception>
        /// <exception cref="IncompleteDataException">Is raised when source stream closed before header-terminator reached.</exception>
        public int EndReadHeader(IAsyncResult asyncResult)
        {
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }
            if(!(asyncResult is ReadToTerminatorAsyncOperation)){
                throw new ArgumentException("Argument 'asyncResult' was not returned by a call to the BeginReadHeader method.");
            }

            ReadToTerminatorAsyncOperation ar = (ReadToTerminatorAsyncOperation)asyncResult;
            if(ar.IsEndCalled){
                throw new InvalidOperationException("EndReadHeader is already called for specified 'asyncResult'.");
            }
            ar.AsyncWaitHandle.WaitOne();
            ar.AsyncWaitHandle.Close();
            ar.IsEndCalled = true;
            if(ar.Exception != null){
                throw ar.Exception;
            }

            return (int)ar.BytesStored;
        }

        #endregion

        #region method ReadHeader

        /// <summary>
        /// Reads header from stream and stores to the specified <b>storeStream</b>.
        /// </summary>
        /// <param name="storeStream">Stream where to store readed header.</param>
        /// <param name="maxCount">Maximum number of bytes to read. Value 0 means not limited.</param>
        /// <param name="exceededAction">Specifies action what is done if <b>maxCount</b> number of bytes has exceeded.</param>
        /// <returns>Returns how many bytes readed from source stream.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>storeStream</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="LineSizeExceededException">Is raised when source stream has too big line.</exception>
        /// <exception cref="DataSizeExceededException">Is raised when reading exceeds <b>maxCount</b> specified value.</exception>
        /// <exception cref="IncompleteDataException">Is raised when source stream closed before header-terminator reached.</exception>
        public int ReadHeader(Stream storeStream,int maxCount,SizeExceededAction exceededAction)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(storeStream == null){
                throw new ArgumentNullException("storeStream");
            }            
            if(maxCount < 0){
                throw new ArgumentException("Argument 'maxCount' must be >= 0.");
            }

            IAsyncResult ar = BeginReadHeader(storeStream,maxCount,exceededAction,null,null);

            return EndReadHeader(ar);
        }

        #endregion

        #region method ReadPeriodTerminated

        /// <summary>
        /// Begins period-terminated data reading.
        /// </summary>
        /// <param name="op">Read period terminated opeartion.</param>
        /// <param name="async">If true then this method can complete asynchronously. If false, this method completed always syncronously.</param>
        /// <returns>Returns true if read line completed synchronously, false if asynchronous operation pending.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool ReadPeriodTerminated(ReadPeriodTerminatedAsyncOP op,bool async)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            if(!op.Start(this)){
                if(!async){
                    // Wait while async operation completes.
                    while(!op.IsCompleted){
                        Thread.Sleep(1);
                    }

                    return true;
                }
                else{
                    return false;
                }
            }
            // Completed synchronously.
            else{
                return true;
            }
        }

        #endregion

        #region method BeginReadFixedCount

        /// <summary>
        /// Begins an asynchronous data reading from the source stream.
        /// </summary>
        /// <param name="storeStream">Stream where to store readed header.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <param name="callback">The AsyncCallback delegate that is executed when asynchronous operation completes.</param>
        /// <param name="state">An object that contains any additional user-defined data.</param>
        /// <returns>An IAsyncResult that represents the asynchronous call.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>storeStream</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IAsyncResult BeginReadFixedCount(Stream storeStream,long count,AsyncCallback callback,object state)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(storeStream == null){
                throw new ArgumentNullException("storeStream");
            }
            if(count < 0){
                throw new ArgumentException("Argument 'count' value must be >= 0.");
            }

            return new ReadToStreamAsyncOperation(this,storeStream,count,callback,state);
        }

        #endregion

        #region method EndReadFixedCount

        /// <summary>
        /// Handles the end of an asynchronous data reading.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that represents an asynchronous call.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="InvalidOperationException">Is raised when <b>EndReadToStream</b> has already been called for specified <b>asyncResult</b>.</exception>
        public void EndReadFixedCount(IAsyncResult asyncResult)
        {
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }
            if(!(asyncResult is ReadToStreamAsyncOperation)){
                throw new ArgumentException("Argument 'asyncResult' was not returned by a call to the BeginReadFixedCount method.");
            }

            ReadToStreamAsyncOperation ar = (ReadToStreamAsyncOperation)asyncResult;
            if(ar.IsEndCalled){
                throw new InvalidOperationException("EndReadFixedCount is already called for specified 'asyncResult'.");
            }
            ar.AsyncWaitHandle.WaitOne();
            ar.AsyncWaitHandle.Close();
            ar.IsEndCalled = true;
            if(ar.Exception != null){
                throw ar.Exception;
            }
        }

        #endregion

        #region method ReadFixedCount

        /// <summary>
        /// Reads specified number of bytes from source stream and writes to the specified stream.
        /// </summary>
        /// <param name="storeStream">Stream where to store readed data.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>storeStream</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void ReadFixedCount(Stream storeStream,long count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(storeStream == null){
                throw new ArgumentNullException("storeStream");
            }
            if(count < 0){
                throw new ArgumentException("Argument 'count' value must be >= 0.");
            }

            IAsyncResult ar = BeginReadFixedCount(storeStream,count,null,null);

            EndReadFixedCount(ar);
        }

        #endregion

        #region method ReadFixedCountString

        /// <summary>
        /// Reads specified number of bytes from source stream and converts it to string with current encoding.
        /// </summary>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>Returns readed data as string.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public string ReadFixedCountString(int count)
        {    
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }        
            if(count < 0){
                throw new ArgumentException("Argument 'count' value must be >= 0.");
            }

            MemoryStream ms = new MemoryStream();
            ReadFixedCount(ms,count);

            return m_pEncoding.GetString(ms.ToArray());
        }

        #endregion
                
        #region method ReadAll

        /// <summary>
        /// Reads all data from source stream and stores to the specified stream.
        /// </summary>
        /// <param name="stream">Stream where to store readed data.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        public void ReadAll(Stream stream)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            byte[] buffer = new byte[32000];
            while(true){
                int readedCount = Read(buffer,0,buffer.Length);
                // End of stream reached, we readed file sucessfully.
                if(readedCount == 0){
                    break;
                }
                else{
                    stream.Write(buffer,0,readedCount);
                }
            }
        }

        #endregion

        #region method Peek

        /// <summary>
        /// Returns the next available character but does not consume it.
        /// </summary>
        /// <returns>An integer representing the next character to be read, or -1 if no more characters are available.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public int Peek()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(this.BytesInReadBuffer == 0){
                BufferRead(false,null);
            }

            // We are end of stream.
            if(this.BytesInReadBuffer == 0){
                return -1;
            }
            else{
                return m_pReadBuffer[m_ReadBufferOffset];
            }
        }

        #endregion

        #region method Write

        /// <summary>
        /// Writes specified string data to stream.
        /// </summary>
        /// <param name="data">Data to write.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>data</b> is null.</exception>
        public void Write(string data)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(data == null){
                throw new ArgumentNullException("data");
            }

            byte[] dataBytes = Encoding.Default.GetBytes(data);
            Write(dataBytes,0,dataBytes.Length);
            Flush();
        }

        #endregion

        #region method WriteLine

        /// <summary>
        /// Writes specified line to stream. If CRLF is missing, it will be added automatically to line data.
        /// </summary>
        /// <param name="line">Line to send.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>line</b> is null.</exception>
        /// <returns>Returns number of raw bytes written.</returns>
        public int WriteLine(string line)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(line == null){
                throw new ArgumentNullException("line");
            }

            if(!line.EndsWith("\r\n")){
                line += "\r\n";
            }

            byte[] dataBytes = m_pEncoding.GetBytes(line);
            Write(dataBytes,0,dataBytes.Length);
            Flush();

            return dataBytes.Length;
        }

        #endregion

        #region method WriteStream

        /// <summary>
        /// Writes all source <b>stream</b> data to stream.
        /// </summary>
        /// <param name="stream">Stream which data to write.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        public void WriteStream(Stream stream)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            byte[] buffer = new byte[m_BufferSize];
            while(true){
                int readed = stream.Read(buffer,0,buffer.Length);
                // We readed all data.
                if(readed == 0){
                    break;
                }
                Write(buffer,0,readed);
            }
            Flush();
        }

        /// <summary>
        /// Writes specified number of bytes from source <b>stream</b> to stream.
        /// </summary>
        /// <param name="stream">Stream which data to write.</param>
        /// <param name="count">Number of bytes to write.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when <b>count</b> argument has invalid value.</exception>
        public void WriteStream(Stream stream,long count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(count < 0){
                throw new ArgumentException("Argument 'count' value must be >= 0.");
            }

            byte[] buffer      = new byte[m_BufferSize];
            long   readedCount = 0;
            while(readedCount < count){
                int readed = stream.Read(buffer,0,(int)Math.Min(buffer.Length,count - readedCount));
                readedCount += readed;
                Write(buffer,0,readed);
            }
            Flush();
        }

        #endregion

        #region method WriteStreamAsync

        #region class WriteStreamAsyncOP

        /// <summary>
        /// This class represents <see cref="SmartStream.WriteStreamAsync"/> asynchronous operation.
        /// </summary>
        public class WriteStreamAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private bool          m_RiseCompleted = false;
            private SmartStream   m_pOwner        = null;
            private Stream        m_pStream       = null;
            private long          m_Count         = 0;
            private byte[]        m_pBuffer       = null;
            private long          m_BytesWritten  = 0;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="stream">Stream which data to write.</param>
            /// <param name="count">Number of bytes to write. Value -1 means all stream data will be written.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
            public WriteStreamAsyncOP(Stream stream,long count)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pStream = stream;
                m_Count   = count;
                m_pBuffer = new byte[32000];
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException = null;
                m_pStream    = null;
                m_pOwner     = null;
                m_pBuffer    = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SmartStream.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SmartStream owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                
                m_pOwner = owner;

                SetState(AsyncOP_State.Active);
                
                BeginReadData();

                // Set flag rise CompletedAsync event flag. The event is raised when async op completes.
                // If already completed sync, that flag has no effect.
                lock(m_pLock){
                    m_RiseCompleted = true;

                    return m_State == AsyncOP_State.Active;
                }
            }

            #endregion


            #region method SetState

            /// <summary>
            /// Sets operation state.
            /// </summary>
            /// <param name="state">New state.</param>
            private void SetState(AsyncOP_State state)
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }

                lock(m_pLock){
                    m_State = state;

                    if(m_State == AsyncOP_State.Completed && m_RiseCompleted){
                        OnCompletedAsync();
                    }
                }
            }

            #endregion

            #region method BeginReadData

            /// <summary>
            /// Starts reading data.
            /// </summary>
            private void BeginReadData()
            {                
                try{
                    while(true){
                        int count = m_Count == -1 ? m_pBuffer.Length : (int)Math.Min(m_pBuffer.Length,m_Count - m_BytesWritten);
                        IAsyncResult readResult = m_pStream.BeginRead(
                            m_pBuffer,
                            0,
                            count,
                            delegate(IAsyncResult r){
                                ProcessReadDataResult(r);
                            },
                            null
                        );
                        // Read data completed synchonously.
                        if(readResult.CompletedSynchronously){
                            // Operation completed asynchronously, it will continue processing.
                            if(ProcessReadDataResult(readResult)){
                                break;
                            }
                            // Error happened in ProcessReadDataResult method.
                            if(this.State != AsyncOP_State.Active){
                                break;
                            }
                        }
                        // Read data completed asynchonously.
                        else{
                            break;
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method ProcessReadDataResult

            /// <summary>
            /// Processes read data result.
            /// </summary>
            /// <param name="readResult">Asynchronous result.</param>
            /// <returns>Retruns true if this method completed asynchronously, otherwise false.</returns>
            private bool ProcessReadDataResult(IAsyncResult readResult)
            {
                try{
                    int countReaded = m_pStream.EndRead(readResult);
                    if(countReaded == 0){
                        // We readed all stream data and write count not specified, we are done.
                        if(m_Count == -1){
                            SetState(AsyncOP_State.Completed);
                        }
                        // Source stream has less data than specified by count.
                        else{
                            m_pException = new ArgumentException("Argument 'stream' has less data than specified in 'count'.","stream");
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                    else{
                        IAsyncResult writeResult = m_pOwner.BeginWrite(
                            m_pBuffer,
                            0,
                            countReaded,
                            delegate(IAsyncResult r){
                                try{
                                    m_pOwner.EndWrite(r);
                                    m_BytesWritten += countReaded;

                                    // We have read and sent all requested data.
                                    if(m_Count == m_BytesWritten){
                                        SetState(AsyncOP_State.Completed);
                                    }
                                    // Start reading next data block(s).
                                    else{                                        
                                        BeginReadData();
                                    }
                                }
                                catch(Exception x){
                                    m_pException = x;
                                    SetState(AsyncOP_State.Completed);
                                }
                            },
                            null
                        );
                        if(writeResult.CompletedSynchronously){
                            m_pOwner.EndWrite(writeResult);
                            m_BytesWritten += countReaded;

                            // We have read and sent all requested data.
                            if(m_Count == m_BytesWritten){
                                SetState(AsyncOP_State.Completed);
                            }
                        }
                        else{
                            return true;
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    SetState(AsyncOP_State.Completed);
                }

                return false;
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets asynchronous operation state.
            /// </summary>
            public AsyncOP_State State
            {
                get{ return m_State; }
            }

            /// <summary>
            /// Gets error happened during operation. Returns null if no error.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public Exception Error
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pException; 
                }
            }

            /// <summary>
            /// Gets number of bytes written.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public long BytesWritten
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Socket' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    return m_BytesWritten; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<WriteStreamAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<WriteStreamAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts writing stream data to this stream.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="WriteStreamAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool WriteStreamAsync(WriteStreamAsyncOP op)
        {
            if(this.m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }

            return op.Start(this);
        }

        #endregion

        #region method WritePeriodTerminated

        /// <summary>
        /// Writes period handled and terminated data to this stream.
        /// </summary>
        /// <param name="stream">Source stream. Reading starts from stream current location.</param>
        /// <returns>Returns number of bytes written to stream.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        /// <exception cref="LineSizeExceededException">Is raised when <b>stream</b> has too big line.</exception>        
        public long WritePeriodTerminated(Stream stream)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            WritePeriodTerminatedAsyncOP op = new WritePeriodTerminatedAsyncOP(stream);
            op.CompletedAsync += delegate(object s1,EventArgs<WritePeriodTerminatedAsyncOP> e1){
                wait.Set();
            };
            if(!this.WritePeriodTerminatedAsync(op)){
                wait.Set();
            }
            wait.WaitOne();
            wait.Close();

            if(op.Error != null){
                throw op.Error;
            }
            else{
                return op.BytesWritten;
            }
        }

        #endregion
        
        #region method WritePeriodTerminatedAsync

        #region class WritePeriodTerminatedAsyncOP

        /// <summary>
        /// This class represents <see cref="SmartStream.WritePeriodTerminatedAsync"/> asynchronous operation.
        /// </summary>
        public class WritePeriodTerminatedAsyncOP : IDisposable,IAsyncOP
        {
            private object          m_pLock         = new object();
            private AsyncOP_State   m_State         = AsyncOP_State.WaitingForStart;
            private Exception       m_pException    = null;
            private SmartStream     m_pStream       = null;
            private SmartStream     m_pOwner        = null;
            private ReadLineAsyncOP m_pReadLineOP   = null;
            private int             m_BytesWritten  = 0;
            private bool            m_EndsCRLF      = false;
            private bool            m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="stream">Source stream. Reading starts from stream current location.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
            public WritePeriodTerminatedAsyncOP(Stream stream)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pStream = new SmartStream(stream,false);
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException  = null;
                m_pStream     = null;
                m_pOwner      = null;
                m_pReadLineOP = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SmartStream.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SmartStream owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pOwner = owner;

                SetState(AsyncOP_State.Active);

                try{
                    // Read line.
                    m_pReadLineOP = new ReadLineAsyncOP(new byte[32000],SizeExceededAction.ThrowException);
                    m_pReadLineOP.Completed += delegate(object s,EventArgs<ReadLineAsyncOP> e){
                        ReadLineCompleted(m_pReadLineOP);
                    };
                    if(m_pStream.ReadLine(m_pReadLineOP,true)){
                        ReadLineCompleted(m_pReadLineOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    SetState(AsyncOP_State.Completed);
                    m_pReadLineOP.Dispose();
                }

                // Set flag rise CompletedAsync event flag. The event is raised when async op completes.
                // If already completed sync, that flag has no effect.
                lock(m_pLock){
                    m_RiseCompleted = true;

                    return m_State == AsyncOP_State.Active;
                }
            }

            #endregion


            #region method SetState

            /// <summary>
            /// Sets operation state.
            /// </summary>
            /// <param name="state">New state.</param>
            private void SetState(AsyncOP_State state)
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }

                lock(m_pLock){
                    m_State = state;

                    if(m_State == AsyncOP_State.Completed && m_RiseCompleted){
                        OnCompletedAsync();
                    }
                }
            }

            #endregion

            #region method ReadLineCompleted

            /// <summary>
            /// Is called when source stream read line reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ReadLineCompleted(ReadLineAsyncOP op)
            {
                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        SetState(AsyncOP_State.Completed);
                    }
                    else{
                        // We have readed all source stream data, we are done.
                        if(op.BytesInBuffer == 0){
                            // Line ends CRLF.
                            if(m_EndsCRLF){
                                m_BytesWritten += 3;
                                m_pOwner.BeginWrite(new byte[]{(byte)'.',(byte)'\r',(byte)'\n'},0,3,this.SendTerminatorCompleted,null);
                            }
                            // Line doesn't end CRLF, we need to add it.
                            else{
                                m_BytesWritten += 5;
                                m_pOwner.BeginWrite(new byte[]{(byte)'\r',(byte)'\n',(byte)'.',(byte)'\r',(byte)'\n'},0,5,this.SendTerminatorCompleted,null);
                            }

                            op.Dispose();
                        }
                        // Write readed line.
                        else{
                            m_BytesWritten += op.BytesInBuffer;

                            // Check if line ends CRLF.
                            if(op.BytesInBuffer >= 2 && op.Buffer[op.BytesInBuffer - 2] == '\r' && op.Buffer[op.BytesInBuffer - 1] == '\n'){
                                m_EndsCRLF = true;
                            }
                            else{
                                m_EndsCRLF = false;
                            }

                            // Period handling. If line starts with period(.), additional period is added.
                            if(op.Buffer[0] == '.'){
                                byte[] buffer = new byte[op.BytesInBuffer + 1];
                                buffer[0] = (byte)'.';
                                Array.Copy(op.Buffer,0,buffer,1,op.BytesInBuffer);

                                m_pOwner.BeginWrite(buffer,0,buffer.Length,this.SendLineCompleted,null);
                            }
                            // Normal line.
                            else{
                                m_pOwner.BeginWrite(op.Buffer,0,op.BytesInBuffer,this.SendLineCompleted,null);
                            }
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    SetState(AsyncOP_State.Completed);
                    op.Dispose();
                }
            }

            #endregion

            #region method SendLineCompleted

            /// <summary>
            /// Is called when line sending has completed.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void SendLineCompleted(IAsyncResult ar)
            {
                try{
                    m_pOwner.EndWrite(ar);

                    // Read next line.
                    // We already have attahed m_pReadLineOP.Completed in start method, so skip it here.
                    // m_pReadLineOP.Completed += delegate(object s,EventArgs<ReadLineAsyncOP> e){                        
                    if(m_pStream.ReadLine(m_pReadLineOP,true)){
                        ReadLineCompleted(m_pReadLineOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method SendTerminatorCompleted

            /// <summary>
            /// Is called when ".CRLF" or "CRLF.CRLF" terminator sending has completed.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void SendTerminatorCompleted(IAsyncResult ar)
            {
                try{
                    m_pOwner.EndWrite(ar);                    
                }
                catch(Exception x){
                    m_pException = x;
                }

                SetState(AsyncOP_State.Completed);
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets asynchronous operation state.
            /// </summary>
            public AsyncOP_State State
            {
                get{ return m_State; }
            }

            /// <summary>
            /// Gets error happened during operation. Returns null if no error.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public Exception Error
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pException; 
                }
            }

            /// <summary>
            /// Gets number of bytes written.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public int BytesWritten
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Socket' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    return m_BytesWritten; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<WritePeriodTerminatedAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<WritePeriodTerminatedAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts writing period handled and terminated data to this stream.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="WritePeriodTerminatedAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool WritePeriodTerminatedAsync(WritePeriodTerminatedAsyncOP op)
        {
            if(this.m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }

            return op.Start(this);
        }

        #endregion

        #region method WriteHeader

        /// <summary>
        /// Reads header from source <b>stream</b> and writes it to stream.
        /// </summary>
        /// <param name="stream">Stream from where to read header.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        public void WriteHeader(Stream stream)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            SmartStream reader = new SmartStream(stream,false);
            reader.ReadHeader(this,0,SizeExceededAction.ThrowException);
        }

        #endregion


        #region override method Flush

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public override void Flush()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }

            m_pStream.Flush();
        }

        #endregion

        #region override method Seek

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <b>origin</b> parameter.</param>
        /// <param name="origin">A value of type SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public override long Seek(long offset,SeekOrigin origin)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }

            return m_pStream.Seek(offset,origin);
        }

        #endregion

        #region override method SetLength

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public override void SetLength(long value)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }

            m_pStream.SetLength(value);

            // Clear read buffer.
            m_ReadBufferOffset = 0;
            m_ReadBufferCount  = 0;
        }

        #endregion
                
        #region override method BeginRead

        /// <summary>
        /// Begins an asynchronous read operation.
        /// </summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the read is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An IAsyncResult that represents the asynchronous read, which could still be pending.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the arguments has out of valid range.</exception>
        public override IAsyncResult BeginRead(byte[] buffer,int offset,int count,AsyncCallback callback,object state)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0){
                throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be >= 0.");
            }
            if(offset > buffer.Length){
                throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be < buffer.Length.");
            }
            if(count < 0){
                throw new ArgumentOutOfRangeException("count","Argument 'count' value must be >= 0.");
            }
            if(offset + count > buffer.Length){
                throw new ArgumentOutOfRangeException("count","Argument 'count' is bigger than than argument 'buffer' can store.");
            }

            return new ReadAsyncOperation(this,buffer,offset,count,callback,state);
        }

        #endregion

        #region override method EndRead

        /// <summary>
        /// Handles the end of an asynchronous data reading.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The total number of bytes read into the <b>buffer</b>. This can be less than the number of bytes requested 
        /// if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null reference.</exception>
        public override int EndRead(IAsyncResult asyncResult)
        {
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }
            if(!(asyncResult is ReadAsyncOperation)){
                throw new ArgumentException("Argument 'asyncResult' was not returned by a call to the BeginRead method.");
            }

            ReadAsyncOperation ar = (ReadAsyncOperation)asyncResult;
            if(ar.IsEndCalled){
                throw new InvalidOperationException("EndRead is already called for specified 'asyncResult'.");
            }
            ar.AsyncWaitHandle.WaitOne();
            ar.AsyncWaitHandle.Close();
            ar.IsEndCalled = true;
            
            return ar.BytesStored;            
        }

        #endregion

        #region override method Read

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the arguments has out of valid range.</exception>
        public override int Read(byte[] buffer,int offset,int count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }            
            if(offset < 0){
                throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be >= 0.");
            }
            if(count < 0){
                throw new ArgumentOutOfRangeException("count","Argument 'count' value must be >= 0.");
            }
            if(offset + count > buffer.Length){
                throw new ArgumentOutOfRangeException("count","Argument 'count' is bigger than than argument 'buffer' can store.");
            }

            if(this.BytesInReadBuffer == 0){
                this.BufferRead(false,null);
            }
            
            if(this.BytesInReadBuffer == 0){
                return 0;
            }
            else{
                int countToCopy = Math.Min(count,this.BytesInReadBuffer);
                Array.Copy(m_pReadBuffer,m_ReadBufferOffset,buffer,offset,countToCopy);
                m_ReadBufferOffset += countToCopy;

                return countToCopy;
            }
        }

        #endregion

        #region override method BeginWrite

        /// <summary>
        /// Begins an asynchronous write operation.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The byte offset in buffer from which to begin writing.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the write is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An IAsyncResult that represents the asynchronous write, which could still be pending.</returns>
        public override IAsyncResult BeginWrite(byte[] buffer,int offset,int count,AsyncCallback callback,object state)
        {
            m_LastActivity  = DateTime.Now;
            m_BytesWritten += count;

            return m_pStream.BeginWrite(buffer, offset, count, callback, state);
        }

        #endregion

        #region override method EndWrite

        /// <summary>
        /// Ends an asynchronous write operation.
        /// </summary>
        /// <param name="asyncResult">A reference to the outstanding asynchronous I/O request.</param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            m_pStream.EndWrite(asyncResult);
        }

        #endregion

        #region override method Write

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public override void Write(byte[] buffer,int offset,int count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }            

            m_pStream.Write(buffer,offset,count);
            
            m_LastActivity = DateTime.Now;
            m_BytesWritten += count;
        }

        #endregion


        #region method BufferRead

        /// <summary>
        /// Begins buffering read-buffer.
        /// </summary>
        /// <param name="async">If true then this method can complete asynchronously. If false, this method completed always syncronously.</param>
        /// <param name="asyncCallback">The callback that is executed when asynchronous operation completes. 
        /// If operation completes synchronously, no callback called.</param>
        /// <returns>
        /// Returns true if the I/O operation is pending. The BufferReadAsyncEventArgs.Completed event on the context parameter will be raised upon completion of the operation. 
        /// Returns false if the I/O operation completed synchronously. The BufferReadAsyncEventArgs.Completed event on the context parameter will not be raised and the context object passed as a parameter may be examined immediately after the method call returns to retrieve the result of the operation. 
        /// </returns>
        /// <exception cref="InvalidOperationException">Is raised when there is data in read buffer and this method is called.</exception>
        private bool BufferRead(bool async,BufferCallback asyncCallback)
        {
            if(this.BytesInReadBuffer != 0){
                throw new InvalidOperationException("There is already data in read buffer.");
            }

            m_ReadBufferOffset =  0;
            m_ReadBufferCount  =  0;

            #region async

            if(async){
                m_pReadBufferOP.ReleaseEvents();
                m_pReadBufferOP.Completed += new EventHandler<EventArgs<BufferReadAsyncOP>>(delegate(object s,EventArgs<BufferReadAsyncOP> e){            
                    if(e.Value.Error != null){
                        if(asyncCallback != null){
                            asyncCallback(e.Value.Error);
                        }
                    }
                    else{
                        m_ReadBufferOffset =  0;
                        m_ReadBufferCount  =  e.Value.BytesInBuffer;
                        m_BytesReaded      += e.Value.BytesInBuffer;
                        m_LastActivity     =  DateTime.Now; 

                        if(asyncCallback != null){
                            asyncCallback(null);
                        }
                    }
                });
                        
                if(!m_pReadBufferOP.Start(async,m_pReadBuffer,m_pReadBuffer.Length)){
                    return true;
                }
                else{
                    if(m_pReadBufferOP.Error != null){
                        throw m_pReadBufferOP.Error;
                    }
                    else{
                        m_ReadBufferOffset =  0;
                        m_ReadBufferCount  =  m_pReadBufferOP.BytesInBuffer;
                        m_BytesReaded      += m_pReadBufferOP.BytesInBuffer;
                        m_LastActivity     =  DateTime.Now; 
                    }

                    return false;
                }
            }

            #endregion

            #region sync

            else{
                int countReaded = m_pStream.Read(m_pReadBuffer,0,m_pReadBuffer.Length);
                m_ReadBufferCount  =  countReaded;
                m_BytesReaded      += countReaded;
                m_LastActivity     =  DateTime.Now;

                return false;
            }

            #endregion                        
        }

        #endregion
                

        #region Properties Implementation

        /// <summary>
        /// Gets this stream underlying stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public Stream SourceStream
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_pStream; 
            }
        }

        /// <summary>
        /// Gets if SmartStream is owner of source stream. This property affects like closing this stream will close SourceStream if IsOwner true.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public bool IsOwner
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_IsOwner; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                m_IsOwner = value;
            }
        }

        /// <summary>
        /// Gets the last time when data was read or written.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public DateTime LastActivity
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_LastActivity; 
            }
        }

        /// <summary>
        /// Gets how many bytes are readed through this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public long BytesReaded
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_BytesReaded; 
            }
        }

        /// <summary>
        /// Gets how many bytes are written through this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public long BytesWritten
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_BytesWritten;
            }
        }

        /// <summary>
        /// Gets number of bytes in read buffer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int BytesInReadBuffer
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_ReadBufferCount - m_ReadBufferOffset; 
            }
        }

        /// <summary>
        /// Gets or sets string related methods default encoding.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
        public Encoding Encoding
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_pEncoding; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }
                if(value == null){
                    throw new ArgumentNullException();
                }

                m_pEncoding = value;
            }
        }


        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override bool CanRead
        { 
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_pStream.CanRead;
            } 
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override bool CanSeek
        { 
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_pStream.CanSeek;
            } 
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override bool CanWrite
        { 
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_pStream.CanWrite;
            } 
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override long Length
        { 
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_pStream.Length;
            } 
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override long Position
        { 
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_pStream.Position;
            } 

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                m_pStream.Position = value;

                // Clear read buffer.
                m_ReadBufferOffset = 0;
                m_ReadBufferCount  = 0;
            }
        }

        #endregion


        //------- Obsolete

        #region class ReadLineAsyncOperation

        /// <summary>
        /// This class implements asynchronous line reading.
        /// </summary>
        private class ReadLineAsyncOperation : IAsyncResult
        {
            private SmartStream        m_pOwner                 = null;
            private byte[]             m_pBuffer                = null;
            private int                m_OffsetInBuffer         = 0;
            private int                m_MaxCount               = 0;
            private SizeExceededAction m_SizeExceededAction     = SizeExceededAction.JunkAndThrowException;
            private AsyncCallback      m_pAsyncCallback         = null;
            private object             m_pAsyncState            = null;
            private AutoResetEvent     m_pAsyncWaitHandle       = null;
            private bool               m_CompletedSynchronously = false;
            private bool               m_IsCompleted            = false;
            private bool               m_IsEndCalled            = false;
            private int                m_BytesReaded            = 0;
            private int                m_BytesStored            = 0;
            private Exception          m_pException             = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="owner">Owner stream.</param>
            /// <param name="buffer">Buffer where to store data.</param>
            /// <param name="offset">The location in <b>buffer</b> to begin storing the data.</param>
            /// <param name="maxCount">Maximum number of bytes to read.</param>
            /// <param name="exceededAction">Specifies how this method behaves when maximum line size exceeded.</param>
            /// <param name="callback">The AsyncCallback delegate that is executed when asynchronous operation completes.</param>
            /// <param name="asyncState">User-defined object that qualifies or contains information about an asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b>,<b>buffer</b> is null reference.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the arguments has out of valid range.</exception>
            public ReadLineAsyncOperation(SmartStream owner,byte[] buffer,int offset,int maxCount,SizeExceededAction exceededAction,AsyncCallback callback,object asyncState)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                if(buffer == null){
                    throw new ArgumentNullException("buffer");
                }
                if(offset < 0){
                    throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be >= 0.");
                }
                if(offset > buffer.Length){
                    throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be < buffer.Length.");
                }
                if(maxCount < 0){
                    throw new ArgumentOutOfRangeException("maxCount","Argument 'maxCount' value must be >= 0.");
                }
                if(offset + maxCount > buffer.Length){
                    throw new ArgumentOutOfRangeException("maxCount","Argument 'maxCount' is bigger than than argument 'buffer' can store.");
                }

                m_pOwner             = owner;
                m_pBuffer            = buffer;
                m_OffsetInBuffer     = offset;
                m_MaxCount           = maxCount;
                m_SizeExceededAction = exceededAction;
                m_pAsyncCallback     = callback;
                m_pAsyncState        = asyncState;

                m_pAsyncWaitHandle = new AutoResetEvent(false);

                DoLineReading();                                
            }


            #region method Buffering_Completed

            /// <summary>
            /// Is called when asynchronous read buffer buffering has completed.
            /// </summary>
            /// <param name="x">Exception that occured during async operation.</param>
            private void Buffering_Completed(Exception x)
            {
                if(x != null){
                    m_pException = x;
                    Completed();
                }
                // We reached end of stream, no more data.
                else if(m_pOwner.BytesInReadBuffer == 0){
                    Completed();
                }
                // Continue line reading.
                else{
                    DoLineReading();
                }
            }

            #endregion

            #region method DoLineReading

            /// <summary>
            /// Does line reading.
            /// </summary>
            private void DoLineReading()
            {           
                try{
                    while(true){
                        // Read buffer empty, buff next data block.
                        if(m_pOwner.BytesInReadBuffer == 0){
                            // Buffering started asynchronously.
                            if(m_pOwner.BufferRead(true,this.Buffering_Completed)){
                                return;
                            }
                            // Buffering completed synchronously, continue processing.
                            else{
                                // We reached end of stream, no more data.
                                if(m_pOwner.BytesInReadBuffer == 0){
                                    Completed();
                                    return;
                                }
                            }
                        }

                        byte b = m_pOwner.m_pReadBuffer[m_pOwner.m_ReadBufferOffset++];
                        m_BytesReaded++;

                        // We have LF line.
                        if(b == '\n'){
                            break;               
                        }
                        // We have CRLF line.
                        else if(b == '\r' && m_pOwner.Peek() == '\n'){
                            // Consume LF char.
                            m_pOwner.ReadByte();
                            m_BytesReaded++;

                            break;
                        }
                        // We have CR line.
                        else if(b == '\r'){
                            break;
                        }
                        // We have normal line data char.
                        else{
                            // Line buffer full.
                            if(m_BytesStored >= m_MaxCount){
                                if(m_SizeExceededAction == SizeExceededAction.ThrowException){
                                    throw new LineSizeExceededException();
                                }
                                // Just skip storing.
                                else{
                                }
                            }
                            else{
                                m_pBuffer[m_OffsetInBuffer++] = b;
                                m_BytesStored++;
                            }
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;                    
                }

                Completed();
            }

            #endregion

            #region method Completed

            /// <summary>
            /// This method must be called when asynchronous operation has completed.
            /// </summary>
            private void Completed()
            {
                m_IsCompleted = true;
                m_pAsyncWaitHandle.Set();
                if(m_pAsyncCallback != null){
                    m_pAsyncCallback(this);
                }
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
            /// </summary>
            public object AsyncState
            {
                get{ return m_pAsyncState; }
            }

            /// <summary>
            /// Gets a WaitHandle that is used to wait for an asynchronous operation to complete.
            /// </summary>
            public WaitHandle AsyncWaitHandle
            {
                get{ return m_pAsyncWaitHandle; }
            }

            /// <summary>
            /// Gets an indication of whether the asynchronous operation completed synchronously.
            /// </summary>
            public bool CompletedSynchronously
            {
                get{ return m_CompletedSynchronously; }
            }

            /// <summary>
            /// Gets an indication whether the asynchronous operation has completed.
            /// </summary>
            public bool IsCompleted
            {
                get{ return m_IsCompleted; }
            }


            /// <summary>
            /// Gets or sets if <b>EndReadLine</b> method is called for this asynchronous operation.
            /// </summary>
            internal bool IsEndCalled
            {
                get{ return m_IsEndCalled; }

                set{ m_IsEndCalled = value; }
            }

            /// <summary>
            /// Gets store buffer.
            /// </summary>
            internal byte[] Buffer
            {
                get{ return m_pBuffer; }
            }

            /// <summary>
            /// Gets number of bytes readed from source stream.
            /// </summary>
            internal int BytesReaded
            {
                get{ return m_BytesReaded; }
            }

            /// <summary>
            /// Gets number of bytes stored in to <b>Buffer</b>.
            /// </summary>
            internal int BytesStored
            {
                get{ return m_BytesStored; }
            }

            #endregion
        }

        #endregion

        #region class ReadToTerminatorAsyncOperation

        /// <summary>
        /// This class implements asynchronous line-based terminated data reader, where terminator is on line itself.
        /// </summary>
        private class ReadToTerminatorAsyncOperation : IAsyncResult
        {
            private SmartStream        m_pOwner                 = null;
            private string             m_Terminator             = "";
            private byte[]             m_pTerminatorBytes       = null;
            private Stream             m_pStoreStream           = null;
            private long               m_MaxCount               = 0;            
            private SizeExceededAction m_SizeExceededAction     = SizeExceededAction.JunkAndThrowException;
            private AsyncCallback      m_pAsyncCallback         = null;
            private object             m_pAsyncState            = null;
            private AutoResetEvent     m_pAsyncWaitHandle       = null;
            private bool               m_CompletedSynchronously = false;
            private bool               m_IsCompleted            = false;
            private bool               m_IsEndCalled            = false;
            private byte[]             m_pLineBuffer            = null;
            private long               m_BytesStored            = 0;
            private Exception          m_pException             = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="owner">Owner stream.</param>
            /// <param name="terminator">Data terminator.</param>
            /// <param name="storeStream">Stream where to store readed header.</param>
            /// <param name="maxCount">Maximum number of bytes to read. Value 0 means not limited.</param>
            /// <param name="exceededAction">Specifies how this method behaves when maximum line size exceeded.</param>
            /// <param name="callback">The AsyncCallback delegate that is executed when asynchronous operation completes.</param>
            /// <param name="asyncState">User-defined object that qualifies or contains information about an asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b>,<b>terminator</b> or <b>storeStream</b> is null reference.</exception>
            public ReadToTerminatorAsyncOperation(SmartStream owner,string terminator,Stream storeStream,long maxCount,SizeExceededAction exceededAction,AsyncCallback callback,object asyncState)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                if(terminator == null){
                    throw new ArgumentNullException("terminator");
                }
                if(storeStream == null){
                    throw new ArgumentNullException("storeStream");
                }
                if(maxCount < 0){
                    throw new ArgumentException("Argument 'maxCount' must be >= 0.");
                }

                m_pOwner             = owner;
                m_Terminator         = terminator;
                m_pTerminatorBytes   = Encoding.ASCII.GetBytes(terminator);
                m_pStoreStream       = storeStream;
                m_MaxCount           = maxCount;
                m_SizeExceededAction = exceededAction;
                m_pAsyncCallback     = callback;
                m_pAsyncState        = asyncState;

                m_pAsyncWaitHandle = new AutoResetEvent(false);

                m_pLineBuffer = new byte[32000];

                // Start reading data.
                #pragma warning disable
                m_pOwner.BeginReadLine(m_pLineBuffer,0,m_pLineBuffer.Length - 2,m_SizeExceededAction,new AsyncCallback(this.ReadLine_Completed),null);
                #pragma warning restore
            }


            #region mehtod ReadLine_Completed

            /// <summary>
            /// This method is called when asyynchronous line reading has completed.
            /// </summary>
            /// <param name="asyncResult">An IAsyncResult that represents an asynchronous call.</param>
            private void ReadLine_Completed(IAsyncResult asyncResult)
            {
                try{
                    int storedCount = 0;                    
                    try{
                        #pragma warning disable
                        storedCount = m_pOwner.EndReadLine(asyncResult);
                        #pragma warning restore
                    }
                    catch(LineSizeExceededException lx){
                        if(m_SizeExceededAction == SizeExceededAction.ThrowException){
                            throw lx;
                        }
                        m_pException = new LineSizeExceededException();
                        storedCount = 32000 - 2;
                    }

                    // Source stream closed berore we reached terminator.
                    if(storedCount == -1){
                        throw new IncompleteDataException();
                    }

                    // Check for terminator.
                    if(Net_Utils.CompareArray(m_pTerminatorBytes,m_pLineBuffer,storedCount)){
                        Completed();
                    }
                    else{
                        // We have exceeded maximum allowed data count.
                        if(m_MaxCount > 0 && (m_BytesStored + storedCount + 2) > m_MaxCount){
                            if(m_SizeExceededAction == SizeExceededAction.ThrowException){
                                throw new DataSizeExceededException();
                            }
                            // Just skip storing.
                            else{
                                m_pException = new DataSizeExceededException();
                            }
                        }
                        else{
                            // Store readed line.
                            m_pLineBuffer[storedCount++] = (byte)'\r';
                            m_pLineBuffer[storedCount++] = (byte)'\n';
                            m_pStoreStream.Write(m_pLineBuffer,0,storedCount);
                            m_BytesStored += storedCount;                           
                        }

                        // Strart reading new line.
                        #pragma warning disable
                        m_pOwner.BeginReadLine(m_pLineBuffer,0,m_pLineBuffer.Length - 2,m_SizeExceededAction,new AsyncCallback(this.ReadLine_Completed),null);
                        #pragma warning restore
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    Completed();                
                }
            }

            #endregion

            #region method Completed

            /// <summary>
            /// This method must be called when asynchronous operation has completed.
            /// </summary>
            private void Completed()
            {
                m_IsCompleted = true;
                m_pAsyncWaitHandle.Set();
                if(m_pAsyncCallback != null){
                    m_pAsyncCallback(this);
                }
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets terminator.
            /// </summary>
            public string Terminator
            {
                get{ return m_Terminator; }
            }

            /// <summary>
            /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
            /// </summary>
            public object AsyncState
            {
                get{ return m_pAsyncState; }
            }

            /// <summary>
            /// Gets a WaitHandle that is used to wait for an asynchronous operation to complete.
            /// </summary>
            public WaitHandle AsyncWaitHandle
            {
                get{ return m_pAsyncWaitHandle; }
            }

            /// <summary>
            /// Gets an indication of whether the asynchronous operation completed synchronously.
            /// </summary>
            public bool CompletedSynchronously
            {
                get{ return m_CompletedSynchronously; }
            }

            /// <summary>
            /// Gets an indication whether the asynchronous operation has completed.
            /// </summary>
            public bool IsCompleted
            {
                get{ return m_IsCompleted; }
            }


            /// <summary>
            /// Gets or sets if <b>EndReadLine</b> method is called for this asynchronous operation.
            /// </summary>
            internal bool IsEndCalled
            {
                get{ return m_IsEndCalled; }

                set{ m_IsEndCalled = value; }
            }

            /// <summary>
            /// Gets number of bytes stored in to <b>storeStream</b>.
            /// </summary>
            internal long BytesStored
            {
                get{ return m_BytesStored; }
            }

            /// <summary>
            /// Gets exception happened on asynchronous operation. Returns null if operation was successfull.
            /// </summary>
            internal Exception Exception
            {
                get{ return m_pException; }
            }

            #endregion

        }

        #endregion

        #region class ReadToStreamAsyncOperation

        /// <summary>
        /// This class implements asynchronous read to stream data reader.
        /// </summary>
        private class ReadToStreamAsyncOperation : IAsyncResult
        {
            private SmartStream        m_pOwner                 = null;
            private Stream             m_pStoreStream           = null;
            private long               m_Count                  = 0;
            private AsyncCallback      m_pAsyncCallback         = null;
            private object             m_pAsyncState            = null;
            private AutoResetEvent     m_pAsyncWaitHandle       = null;
            private bool               m_CompletedSynchronously = false;
            private bool               m_IsCompleted            = false;
            private bool               m_IsEndCalled            = false;
            private long               m_BytesStored            = 0;
            private Exception          m_pException             = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="owner">Owner stream.</param>
            /// <param name="storeStream">Stream where to store readed data.</param>
            /// <param name="count">Number of bytes to read from source stream.</param>
            /// <param name="callback">The AsyncCallback delegate that is executed when asynchronous operation completes.</param>
            /// <param name="asyncState">User-defined object that qualifies or contains information about an asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> or <b>storeStream</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public ReadToStreamAsyncOperation(SmartStream owner,Stream storeStream,long count,AsyncCallback callback,object asyncState)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                if(storeStream == null){
                    throw new ArgumentNullException("storeStream");
                }
                if(count < 0){
                    throw new ArgumentException("Argument 'count' must be >= 0.");
                }

                m_pOwner             = owner;
                m_pStoreStream       = storeStream;
                m_Count              = count;
                m_pAsyncCallback     = callback;
                m_pAsyncState        = asyncState;

                m_pAsyncWaitHandle = new AutoResetEvent(false);

                if(m_Count == 0){
                    Completed();                    
                }
                else{
                    DoDataReading();
                }
            }


            #region method Buffering_Completed

            /// <summary>
            /// Is called when asynchronous read buffer buffering has completed.
            /// </summary>
            /// <param name="x">Exception that occured during async operation.</param>
            private void Buffering_Completed(Exception x)
            {
                if(x != null){
                    m_pException = x;
                    Completed();
                }
                // We reached end of stream, no more data.
                else if(m_pOwner.BytesInReadBuffer == 0){
                    m_pException = new IncompleteDataException();
                    Completed();
                }
                // Continue line reading.
                else{
                    DoDataReading();
                }
            }

            #endregion

            #region method DoReading

            /// <summary>
            /// Does data reading.
            /// </summary>
            private void DoDataReading()
            {
                try{
                    while(true){
                        // Read buffer empty, buff next data block.
                        if(m_pOwner.BytesInReadBuffer == 0){
                            // Buffering started asynchronously.
                            if(m_pOwner.BufferRead(true,this.Buffering_Completed)){
                                return;
                            }
                            // Buffering completed synchronously, continue processing.
                            else{
                                // We reached end of stream, no more data.
                                if(m_pOwner.BytesInReadBuffer == 0){
                                    throw new IncompleteDataException();
                                }
                            }
                        }
                    
                        int countToRead = (int)Math.Min(m_Count - m_BytesStored,m_pOwner.BytesInReadBuffer);                
                        m_pStoreStream.Write(m_pOwner.m_pReadBuffer,m_pOwner.m_ReadBufferOffset,countToRead);
                        m_BytesStored += countToRead;
                        m_pOwner.m_ReadBufferOffset += countToRead;
   
                        // We have readed all data.
                        if(m_Count == m_BytesStored){
                            Completed();
                            return;
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    Completed();
                }
            }

            #endregion

            #region method Completed

            /// <summary>
            /// This method must be called when asynchronous operation has completed.
            /// </summary>
            private void Completed()
            {
                m_IsCompleted = true;
                m_pAsyncWaitHandle.Set();
                if(m_pAsyncCallback != null){
                    m_pAsyncCallback(this);
                }
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
            /// </summary>
            public object AsyncState
            {
                get{ return m_pAsyncState; }
            }

            /// <summary>
            /// Gets a WaitHandle that is used to wait for an asynchronous operation to complete.
            /// </summary>
            public WaitHandle AsyncWaitHandle
            {
                get{ return m_pAsyncWaitHandle; }
            }

            /// <summary>
            /// Gets an indication of whether the asynchronous operation completed synchronously.
            /// </summary>
            public bool CompletedSynchronously
            {
                get{ return m_CompletedSynchronously; }
            }

            /// <summary>
            /// Gets an indication whether the asynchronous operation has completed.
            /// </summary>
            public bool IsCompleted
            {
                get{ return m_IsCompleted; }
            }


            /// <summary>
            /// Gets or sets if <b>EndReadLine</b> method is called for this asynchronous operation.
            /// </summary>
            internal bool IsEndCalled
            {
                get{ return m_IsEndCalled; }

                set{ m_IsEndCalled = value; }
            }

            /// <summary>
            /// Gets number of bytes stored in to <b>storeStream</b>.
            /// </summary>
            internal long BytesStored
            {
                get{ return m_BytesStored; }
            }

            /// <summary>
            /// Gets exception happened on asynchronous operation. Returns null if operation was successfull.
            /// </summary>
            internal Exception Exception
            {
                get{ return m_pException; }
            }

            #endregion

        }

        #endregion

        #region class ReadAsyncOperation

        /// <summary>
        /// This class implements asynchronous data reader.
        /// </summary>
        private class ReadAsyncOperation : IAsyncResult
        {
            private SmartStream        m_pOwner                 = null;
            private byte[]             m_pBuffer                = null;
            private int                m_OffsetInBuffer         = 0;
            private int                m_MaxSize                = 0;
            private AsyncCallback      m_pAsyncCallback         = null;
            private object             m_pAsyncState            = null;
            private AutoResetEvent     m_pAsyncWaitHandle       = null;
            private bool               m_CompletedSynchronously = false;
            private bool               m_IsCompleted            = false;
            private bool               m_IsEndCalled            = false;
            private int                m_BytesStored            = 0;
            private Exception          m_pException             = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="owner">Owner stream.</param>
            /// <param name="buffer">Buffer where to store data.</param>
            /// <param name="offset">The location in <b>buffer</b> to begin storing the data.</param>
            /// <param name="maxSize">Maximum number of bytes to read.</param>
            /// <param name="callback">The AsyncCallback delegate that is executed when asynchronous operation completes.</param>
            /// <param name="asyncState">User-defined object that qualifies or contains information about an asynchronous operation.</param>
            public ReadAsyncOperation(SmartStream owner,byte[] buffer,int offset,int maxSize,AsyncCallback callback,object asyncState)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                if(buffer == null){
                    throw new ArgumentNullException("buffer");
                }
                if(offset < 0){
                    throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be >= 0.");
                }
                if(offset > buffer.Length){
                    throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be < buffer.Length.");
                }
                if(maxSize < 0){
                    throw new ArgumentOutOfRangeException("maxSize","Argument 'maxSize' value must be >= 0.");
                }
                if(offset + maxSize > buffer.Length){
                    throw new ArgumentOutOfRangeException("maxSize","Argument 'maxSize' is bigger than than argument 'buffer' can store.");
                }

                m_pOwner             = owner;
                m_pBuffer            = buffer;
                m_OffsetInBuffer     = offset;
                m_MaxSize            = maxSize;
                m_pAsyncCallback     = callback;
                m_pAsyncState        = asyncState;

                m_pAsyncWaitHandle = new AutoResetEvent(false);

                DoRead();
            }


            #region method Buffering_Completed

            /// <summary>
            /// Is called when asynchronous read buffer buffering has completed.
            /// </summary>
            /// <param name="x">Exception that occured during async operation.</param>
            private void Buffering_Completed(Exception x)
            {
                if(x != null){
                    m_pException = x;
                    Completed();
                }
                // We reached end of stream, no more data.
                else if(m_pOwner.BytesInReadBuffer == 0){
                    Completed();
                }
                // Continue data reading.
                else{
                    DoRead();
                }
            }

            #endregion

            #region method DoRead

            /// <summary>
            /// Does asynchronous data reading.
            /// </summary>
            private void DoRead()
            {
                try{
                    // Read buffer empty, buff next data block.
                    if(m_pOwner.BytesInReadBuffer == 0){
                        // Buffering started asynchronously.
                        if(m_pOwner.BufferRead(true,this.Buffering_Completed)){
                            return;
                        }
                        // Buffering completed synchronously, continue processing.
                        else{
                            // We reached end of stream, no more data.
                            if(m_pOwner.BytesInReadBuffer == 0){
                                Completed();
                                return;
                            }
                        }
                    }

                    int readedCount = Math.Min(m_MaxSize,m_pOwner.BytesInReadBuffer);
                    Array.Copy(m_pOwner.m_pReadBuffer,m_pOwner.m_ReadBufferOffset,m_pBuffer,m_OffsetInBuffer,readedCount);
                    m_pOwner.m_ReadBufferOffset += readedCount;
                    m_pOwner.m_LastActivity = DateTime.Now;
                    m_BytesStored += readedCount;

                    Completed();
                }
                catch(Exception x){
                    m_pException = x;
                    Completed();
                }
            }

            #endregion

            #region method Completed

            /// <summary>
            /// This method must be called when asynchronous operation has completed.
            /// </summary>
            private void Completed()
            {
                m_IsCompleted = true;
                m_pAsyncWaitHandle.Set();
                if(m_pAsyncCallback != null){
                    m_pAsyncCallback(this);
                }
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
            /// </summary>
            public object AsyncState
            {
                get{ return m_pAsyncState; }
            }

            /// <summary>
            /// Gets a WaitHandle that is used to wait for an asynchronous operation to complete.
            /// </summary>
            public WaitHandle AsyncWaitHandle
            {
                get{ return m_pAsyncWaitHandle; }
            }

            /// <summary>
            /// Gets an indication of whether the asynchronous operation completed synchronously.
            /// </summary>
            public bool CompletedSynchronously
            {
                get{ return m_CompletedSynchronously; }
            }

            /// <summary>
            /// Gets an indication whether the asynchronous operation has completed.
            /// </summary>
            public bool IsCompleted
            {
                get{ return m_IsCompleted; }
            }


            /// <summary>
            /// Gets or sets if <b>EndReadLine</b> method is called for this asynchronous operation.
            /// </summary>
            internal bool IsEndCalled
            {
                get{ return m_IsEndCalled; }

                set{ m_IsEndCalled = value; }
            }

            /// <summary>
            /// Gets store buffer.
            /// </summary>
            internal byte[] Buffer
            {
                get{ return m_pBuffer; }
            }

            /// <summary>
            /// Gets number of bytes stored in to <b>Buffer</b>.
            /// </summary>
            internal int BytesStored
            {
                get{ return m_BytesStored; }
            }

            #endregion
        }

        #endregion


        #region method BeginReadLine

        /// <summary>
        /// Begins an asynchronous line reading from the source stream.
        /// </summary>
        /// <param name="buffer">Buffer where to store readed line data.</param>
        /// <param name="offset">The location in <b>buffer</b> to begin storing the data.</param>
        /// <param name="maxCount">Maximum number of bytes to read.</param>
        /// <param name="exceededAction">Specifies how this method behaves when maximum line size exceeded.</param>
        /// <param name="callback">The AsyncCallback delegate that is executed when asynchronous operation completes.</param>
        /// <param name="state">An object that contains any additional user-defined data.</param>
        /// <returns>An IAsyncResult that represents the asynchronous call.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">is raised when any of the arguments has invalid value.</exception>
        [Obsolete("Use method 'ReadLine' instead.")]
        public IAsyncResult BeginReadLine(byte[] buffer,int offset,int maxCount,SizeExceededAction exceededAction,AsyncCallback callback,object state)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0){
                throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be >= 0.");
            }
            if(offset > buffer.Length){
                throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be < buffer.Length.");
            }
            if(maxCount < 0){
                throw new ArgumentOutOfRangeException("maxCount","Argument 'maxCount' value must be >= 0.");
            }
            if(offset + maxCount > buffer.Length){
                throw new ArgumentOutOfRangeException("maxCount","Argument 'maxCount' is bigger than than argument 'buffer' can store.");
            }

            return new ReadLineAsyncOperation(this,buffer,offset,maxCount,exceededAction,callback,state);
        }

        #endregion

        #region method EndReadLine

        /// <summary>
        /// Handles the end of an asynchronous line reading.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that represents an asynchronous call.</param>
        /// <returns>Returns number of bytes stored to <b>buffer</b>. Returns -1 if no more data, end of stream reached.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="InvalidOperationException">Is raised when <b>EndReadLine</b> has already been called for specified <b>asyncResult</b>.</exception>
        /// <exception cref="LineSizeExceededException">Is raised when <b>maxCount</b> value is exceeded.</exception>        
        [Obsolete("Use method 'ReadLine' instead.")]
        public int EndReadLine(IAsyncResult asyncResult)
        {
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }
            if(!(asyncResult is ReadLineAsyncOperation)){
                throw new ArgumentException("Argument 'asyncResult' was not returned by a call to the BeginReadLine method.");
            }

            ReadLineAsyncOperation ar = (ReadLineAsyncOperation)asyncResult;
            if(ar.IsEndCalled){
                throw new InvalidOperationException("EndReadLine is already called for specified 'asyncResult'.");
            }
            ar.AsyncWaitHandle.WaitOne();
            ar.AsyncWaitHandle.Close();
            ar.IsEndCalled = true;
            
            if(ar.BytesReaded == 0){
                return -1;
            }
            else{
                return ar.BytesStored;
            }
        }

        #endregion

    }
}
