using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// This class implements read,write or read-write access stream.
    /// </summary>
    public class ReadWriteControlledStream : Stream
    {
        private Stream     m_pStream    = null;
        private FileAccess m_AccessMode = FileAccess.ReadWrite;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stream">Source stream.</param>
        /// <param name="access">This stream access mode.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        public ReadWriteControlledStream(Stream stream,FileAccess access)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            m_pStream    = stream;
            m_AccessMode = access;
        }


        #region override method Flush

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public override void Flush()
        {
            m_pStream.Flush();
        }

        #endregion

        #region override method Seek

        /// <summary>
        /// Sets the position within the current stream. This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <b>origin</b> parameter.</param>
        /// <param name="origin">A value of type SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset,SeekOrigin origin)
        {
            return m_pStream.Seek(offset,origin);
        }

        #endregion

        #region override method SetLength

        /// <summary>
        /// Sets the length of the current stream. This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
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
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="NotSupportedException">Is raised when reading not supported.</exception>
        public override int Read(byte[] buffer,int offset,int count)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0 || offset > buffer.Length){
                throw new ArgumentException("Invalid argument 'offset' value.");
            }
            if(offset + count > buffer.Length){
                throw new ArgumentException("Invalid argument 'count' value.");
            }
            if((m_AccessMode & FileAccess.Read) == 0){
                throw new NotSupportedException();
            }

            return m_pStream.Read(buffer,offset,count);
        }

        #endregion

        #region override method Write

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="NotSupportedException">Is raised when reading not supported.</exception>
        public override void Write(byte[] buffer,int offset,int count)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0 || offset > buffer.Length){
                throw new ArgumentException("Invalid argument 'offset' value.");
            }
            if(offset + count > buffer.Length){
                throw new ArgumentException("Invalid argument 'count' value.");
            }
            if((m_AccessMode & FileAccess.Write) == 0){
                throw new NotSupportedException();
            }

            m_pStream.Write(buffer,offset,count);
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override bool CanRead
        { 
            get{ return (m_AccessMode & FileAccess.Read) != 0; } 
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override bool CanSeek
        { 
            get{ return m_pStream.CanSeek; } 
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override bool CanWrite
        { 
            get{ return (m_AccessMode & FileAccess.Write) != 0; } 
        }

        /// <summary>
        /// Gets the length in bytes of the stream.  This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override long Length
        { 
            get{ return m_pStream.Length; } 
        }

        /// <summary>
        /// Gets or sets the position within the current stream. This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override long Position
        { 
            get{ return m_pStream.Position; } 

            set{ m_pStream.Position = value; }
        }

        #endregion
    }
}
