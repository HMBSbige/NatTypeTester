using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// Implements read-only stream what operates on specified range of source stream
    /// </summary>
    public class PartialStream : Stream
    {
        private bool   m_IsDisposed = false;
        private Stream m_pStream    = null;
        private long   m_Start      = 0;
        private long   m_Length     = 0;
        private long   m_Position   = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stream">Source stream.</param>
        /// <param name="start">Zero based start positon in source stream.</param>
        /// <param name="length">Length of stream.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public PartialStream(Stream stream,long start,long length)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(!stream.CanSeek){
                throw new ArgumentException("Argument 'stream' does not support seeking.");
            }
            if(start < 0){
                throw new ArgumentException("Argument 'start' value must be >= 0.");
            }
            if((start + length) > stream.Length){
                throw new ArgumentException("Argument 'length' value will exceed source stream length.");
            }

            m_pStream = stream;
            m_Start   = start;
            m_Length  = length;
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

            base.Dispose();
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

            if(origin == SeekOrigin.Begin){
                m_Position = 0;
            }
            else if(origin == SeekOrigin.Current){
            }
            else if(origin == SeekOrigin.End){
                m_Position = m_Length;
            } 

            return m_Position;
        }

        #endregion

        #region override method SetLength

        /// <summary>
        /// Sets the length of the current stream. This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="NotSupportedException">Is raised when this method is accessed.</exception>
        public override void SetLength(long value)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }

            throw new NotSupportedException();
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
        public override int Read(byte[] buffer,int offset,int count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }
            
            if(m_pStream.Position != (m_Start + m_Position)){
                m_pStream.Position = m_Start + m_Position;
            }
            int readedCount = m_pStream.Read(buffer,offset,Math.Min(count,(int)(this.Length - m_Position)));
            m_Position += readedCount;

            return readedCount;
        }

        #endregion

        #region override method Write

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="NotSupportedException">Is raised when this method is accessed.</exception>
        public override void Write(byte[] buffer,int offset,int count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }
 
            throw new NotSupportedException();
        }

        #endregion


        #region Properties Implementation

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

                return true;
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

                return true;
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

                return false;
            } 
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="Seek">Is raised when this property is accessed.</exception>
        public override long Length
        { 
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                return m_Length;
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

                return m_Position;
            } 

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }
                if(value < 0 || value > this.Length){
                    throw new ArgumentException("Property 'Position' value must be >= 0 and <= this.Length.");
                }

                m_Position = value;
            }
        }

        #endregion

    }
}
