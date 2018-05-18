using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// This class represents auto switching memory/temp-file stream.
    /// </summary>
    public class MemoryStreamEx : Stream
    {
        private bool   m_IsDisposed = false;
        private Stream m_pStream    = null;
        private int    m_MaxMemSize = 32000;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="memSize">Maximum bytes store to memory, before switching over temporary file.</param>
        public MemoryStreamEx(int memSize)
        {
            m_MaxMemSize = memSize;

            m_pStream = new MemoryStream();
        }

        /// <summary>
        /// Destructor - Just incase user won't call dispose.
        /// </summary>
        ~MemoryStreamEx()
        {
            Dispose();
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
            if(m_pStream != null){
                m_pStream.Close();
            }
            m_pStream = null;

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

            m_pStream.SetLength(value);
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
        public override int Read(byte[] buffer,int offset,int count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            
            return m_pStream.Read(buffer,offset,count);
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
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        public override void Write(byte[] buffer,int offset,int count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("SmartStream");
            }
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }

            // We need switch to temporary file.
            if(m_pStream is MemoryStream && (m_pStream.Position + count) > m_MaxMemSize){
                FileStream fs = new FileStream(Path.GetTempPath() + "ls-" + Guid.NewGuid().ToString().Replace("-","") + ".tmp",FileMode.Create,FileAccess.ReadWrite,FileShare.Read,32000,FileOptions.DeleteOnClose);

                m_pStream.Position = 0;
                Net_Utils.StreamCopy(m_pStream,fs,8000);
                m_pStream.Close();
                m_pStream = fs;
            }
 
            m_pStream.Write(buffer,offset,count);
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

                return true;
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
                if(value < 0 || value > this.Length){
                    throw new ArgumentException("Property 'Position' value must be >= 0 and <= this.Length.");
                }

                m_pStream.Position = value;
            }
        }

        #endregion
    }
}
