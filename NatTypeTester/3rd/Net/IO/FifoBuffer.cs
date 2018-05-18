using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// Implements FIFO(first in - first out) buffer.
    /// </summary>
    public class FifoBuffer
    {
        private object m_pLock       = new object();
        private byte[] m_pBuffer     = null;
        private int    m_ReadOffset  = 0;
        private int    m_WriteOffset = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="maxSize">Maximum number of bytes can buffer in FIFO.</param>
        /// <exception cref="ArgumentException">Is raised when </exception>
        public FifoBuffer(int maxSize)
        {
            if(maxSize < 1){
                throw new ArgumentException("Argument 'maxSize' value must be >= 1.");
            }

            m_pBuffer = new byte[maxSize];
        }


        #region method Read

        /// <summary>
        /// Reads up to specified count of bytes from the FIFO buffer.
        /// </summary>
        /// <param name="buffer">Buffer where to store data.</param>
        /// <param name="offset">Index in the buffer.</param>
        /// <param name="count">Maximum number of bytes to read.</param>
        /// <returns>Returns number of bytes readed. Returns 0 if no data in the buffer.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the arguments has out of allowed range.</exception>
        public int Read(byte[] buffer,int offset,int count)
        {
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

            lock(m_pLock){
                int countToRead = Math.Min(count,m_WriteOffset - m_ReadOffset);
                if(countToRead > 0){
                    Array.Copy(m_pBuffer,m_ReadOffset,buffer,offset,countToRead);
                    m_ReadOffset += countToRead;
                }

                return countToRead;
            }
        }

        #endregion

        #region method Write

        /// <summary>
        /// Writes specified number of bytes to the FIFO buffer.
        /// </summary>
        /// <param name="buffer">Data buffer.</param>
        /// <param name="offset">Index in the buffer.</param>
        /// <param name="count">Number of bytes to wrtite.</param>
        /// <param name="ignoreBufferFull">If true, disables excption raising when FIFO full.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the arguments has out of allowed range.</exception>
        /// <exception cref="DataSizeExceededException">Is raised when ignoreBufferFull = false and FIFO buffer has no room to store data.</exception>
        public void Write(byte[] buffer,int offset,int count,bool ignoreBufferFull)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }        
            if(offset < 0){
                throw new ArgumentOutOfRangeException("offset","Argument 'offset' value must be >= 0.");
            }
            if(count < 0 || (count + offset) > buffer.Length){
                throw new ArgumentOutOfRangeException("count");
            }

            lock(m_pLock){
                int freeSpace = m_pBuffer.Length - m_WriteOffset;

                // We don't have enough room to store data.
                if(freeSpace < count){
                    TrimStart();

                    // Recalculate free space.
                    freeSpace = m_pBuffer.Length - m_WriteOffset;

                    // After trim we can store data.
                    if(freeSpace >= count){
                        Array.Copy(buffer,offset,m_pBuffer,m_WriteOffset,count);
                        m_WriteOffset += count;
                    }
                    // We have not enough space.
                    else{
                        if(!ignoreBufferFull){
                            throw new DataSizeExceededException();
                        }
                    }
                }
                // Store data to buffer.
                else{
                    Array.Copy(buffer,offset,m_pBuffer,m_WriteOffset,count);
                    m_WriteOffset += count;
                }
            }
        }

        #endregion

        #region method Clear

        /// <summary>
        /// Clears buffer data.
        /// </summary>
        public void Clear()
        {
            lock(m_pLock){
                m_ReadOffset  = 0;
                m_WriteOffset = 0;
            }
        }

        #endregion


        #region method TrimStart

        /// <summary>
        /// Removes unused space from the buffer beginning.
        /// </summary>
        private void TrimStart()
        {            
            if(m_ReadOffset > 0){
                byte[] buffer = new byte[this.Available];
                Array.Copy(m_pBuffer,m_ReadOffset,buffer,0,buffer.Length);
                Array.Copy(buffer,m_pBuffer,buffer.Length);
                m_ReadOffset  = 0;
                m_WriteOffset = buffer.Length;
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets maximum number of bytes can buffer in FIFO.
        /// </summary>
        public int MaxSize
        {
            get{ return m_pBuffer.Length; }
        }

        /// <summary>
        /// Gets number of bytes avialable in FIFO.
        /// </summary>
        public int Available
        {
            get{ return m_WriteOffset - m_ReadOffset; }
        }

        #endregion
    }
}
