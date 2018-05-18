using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// This class implements base64 encoder/decoder. Defined in RFC 4648.
    /// </summary>
    public class Base64Stream : Stream,IDisposable
    {
        #region BASE64_ENCODE_TABLE

        private readonly static byte[] BASE64_ENCODE_TABLE = new byte[]{
		    (byte)'A',(byte)'B',(byte)'C',(byte)'D',(byte)'E',(byte)'F',(byte)'G',(byte)'H',(byte)'I',(byte)'J',
            (byte)'K',(byte)'L',(byte)'M',(byte)'N',(byte)'O',(byte)'P',(byte)'Q',(byte)'R',(byte)'S',(byte)'T',
            (byte)'U',(byte)'V',(byte)'W',(byte)'X',(byte)'Y',(byte)'Z',(byte)'a',(byte)'b',(byte)'c',(byte)'d',
            (byte)'e',(byte)'f',(byte)'g',(byte)'h',(byte)'i',(byte)'j',(byte)'k',(byte)'l',(byte)'m',(byte)'n',
            (byte)'o',(byte)'p',(byte)'q',(byte)'r',(byte)'s',(byte)'t',(byte)'u',(byte)'v',(byte)'w',(byte)'x',
            (byte)'y',(byte)'z',(byte)'0',(byte)'1',(byte)'2',(byte)'3',(byte)'4',(byte)'5',(byte)'6',(byte)'7',
            (byte)'8',(byte)'9',(byte)'+',(byte)'/'
		};

        #endregion

        #region BASE64_DECODE_TABLE

        private readonly static short[] BASE64_DECODE_TABLE = new short[]{
            -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,  // 0 -    9
		    -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,  //10 -   19
		    -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,  //20 -   29
		    -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,  //30 -   39
		    -1,-1,-1,62,-1,-1,-1,63,52,53,  //40 -   49
		    54,55,56,57,58,59,60,61,-1,-1,  //50 -   59
		    -1,-1,-1,-1,-1, 0, 1, 2, 3, 4,  //60 -   69
		    5, 6, 7, 8, 9,10,11,12,13,14,   //70 -   79
		    15,16,17,18,19,20,21,22,23,24,  //80 -   89
		    25,-1,-1,-1,-1,-1,-1,26,27,28,  //90 -   99
		    29,30,31,32,33,34,35,36,37,38,  //100 - 109
		    39,40,41,42,43,44,45,46,47,48,  //110 - 119
		    49,50,51,-1,-1,-1,-1,-1         //120 - 127
        };

        #endregion

        private bool        m_IsDisposed         = false;
        private bool        m_IsFinished         = false;
        private Stream      m_pStream            = null;
        private bool        m_IsOwner            = false;
        private bool        m_AddLineBreaks      = true;
        private FileAccess  m_AccessMode         = FileAccess.ReadWrite;
        private int         m_EncodeBufferOffset = 0;
        private int         m_OffsetInEncode3x8Block = 0;
        private byte[]      m_pEncode3x8Block    = new byte[3];
        private byte[]      m_pEncodeBuffer      = new byte[78];
        private byte[]      m_pDecodedBlock      = null;
        private int         m_DecodedBlockOffset = 0;
        private int         m_DecodedBlockCount  = 0;
        private Base64      m_pBase64            = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stream">Stream which to encode/decode.</param>
        /// <param name="owner">Specifies if Base64Stream is owner of <b>stream</b>.</param>
        /// <param name="addLineBreaks">Specifies if encoder inserts CRLF after each 76 bytes.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        public Base64Stream(Stream stream,bool owner,bool addLineBreaks) : this(stream,owner,addLineBreaks,FileAccess.ReadWrite)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stream">Stream which to encode/decode.</param>
        /// <param name="owner">Specifies if Base64Stream is owner of <b>stream</b>.</param>
        /// <param name="addLineBreaks">Specifies if encoder inserts CRLF after each 76 bytes.</param>
        /// <param name="access">This stream access mode.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        public Base64Stream(Stream stream,bool owner,bool addLineBreaks,FileAccess access)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            m_pStream       = stream;
            m_IsOwner       = owner;
            m_AddLineBreaks = addLineBreaks;
            m_AccessMode    = access;

            m_pDecodedBlock = new byte[8000];
            m_pBase64       = new Base64();
        }

        #region method Dispose

        /// <summary>
        /// Celans up any resources being used.
        /// </summary>
        public new void Dispose()
        {
            if(m_IsDisposed){
                return;
            }
            try{
                Finish();
            }
            catch{
            }
            m_IsDisposed = true;

            if(m_IsOwner){
                m_pStream.Close();
            }
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
                throw new ObjectDisposedException("Base64Stream");
            }
        }

        #endregion

        #region override method Seek

        /// <summary>
        /// Sets the position within the current stream. This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <b>origin</b> parameter.</param>
        /// <param name="origin">A value of type SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="NotSupportedException">Is raised when this method is accessed.</exception>
        public override long Seek(long offset,SeekOrigin origin)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("Base64Stream");
            }

            throw new NotSupportedException();
        }

        #endregion

        #region override method SetLength

        /// <summary>
        /// Sets the length of the current stream. This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="Seek">Is raised when this method is accessed.</exception>
        public override void SetLength(long value)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("Base64Stream");
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
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the arguments has out of valid range.</exception>
        /// <exception cref="NotSupportedException">Is raised when reading not supported.</exception>
        public override int Read(byte[] buffer,int offset,int count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("Base64Stream");
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
            if((m_AccessMode & FileAccess.Read) == 0){
                throw new NotSupportedException();
            }

            // We havn't any decoded data left, decode new data block.
            if((m_DecodedBlockCount - m_DecodedBlockOffset) == 0){
                byte[] readBuffer = new byte[m_pDecodedBlock.Length + 3];
                int readedCount = m_pStream.Read(readBuffer,0,readBuffer.Length - 3);
                // We reached end of stream, no more data.
                if(readedCount == 0){
                    return 0;
                }

                // Decode block must contain only integral 4-byte base64 blocks.
                // Count base64 chars.
                int base64Count = 0;
                for(int i=0;i<readedCount;i++){
                    byte b = readBuffer[i];
                    if(b == '=' || BASE64_DECODE_TABLE[b] != -1){
                        base64Count++;
                    }
                }
                // Read while last block is full 4-byte base64 block.
                while((base64Count % 4) != 0){
                    int b = m_pStream.ReadByte();
                    // End of stream reached.
                    if(b == -1){
                        break;
                    }
                    else if(b == '=' || BASE64_DECODE_TABLE[b] != -1){
                        readBuffer[readedCount++] = (byte)b;
                        base64Count++;
                    }
                }

                // Decode block.
                m_DecodedBlockCount  = m_pBase64.Decode(readBuffer,0,readedCount,m_pDecodedBlock,0,true);
                m_DecodedBlockOffset = 0;
            }

            int available   = m_DecodedBlockCount - m_DecodedBlockOffset;
            int countToCopy = Math.Min(count,available);
            Array.Copy(m_pDecodedBlock,m_DecodedBlockOffset,buffer,offset,countToCopy);
            m_DecodedBlockOffset += countToCopy;

            return countToCopy;
        }

        #endregion

        #region override method Write

        /// <summary>
        /// Encodes a sequence of bytes, writes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this.Finish has been called and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="NotSupportedException">Is raised when reading not supported.</exception>
        public override void Write(byte[] buffer,int offset,int count)        
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsFinished){
                throw new InvalidOperationException("Stream is marked as finished by calling Finish method.");
            }
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0 || offset > buffer.Length){
                throw new ArgumentException("Invalid argument 'offset' value.");
            }
            if(count < 0 || count > (buffer.Length - offset)){
                throw new ArgumentException("Invalid argument 'count' value.");
            } 
            if((m_AccessMode & FileAccess.Write) == 0){
                throw new NotSupportedException();
            }           

            /* RFC 4648.
			
				Base64 is processed from left to right by 4 6-bit byte block, 4 6-bit byte block 
				are converted to 3 8-bit bytes.
				If base64 4 byte block doesn't have 3 8-bit bytes, missing bytes are marked with =. 
							
				Value Encoding  Value Encoding  Value Encoding  Value Encoding
					0 A            17 R            34 i            51 z
					1 B            18 S            35 j            52 0
					2 C            19 T            36 k            53 1
					3 D            20 U            37 l            54 2
					4 E            21 V            38 m            55 3
					5 F            22 W            39 n            56 4
					6 G            23 X            40 o            57 5
					7 H            24 Y            41 p            58 6
					8 I            25 Z            42 q            59 7
					9 J            26 a            43 r            60 8
					10 K           27 b            44 s            61 9
					11 L           28 c            45 t            62 +
					12 M           29 d            46 u            63 /
					13 N           30 e            47 v
					14 O           31 f            48 w         (pad) =
					15 P           32 g            49 x
					16 Q           33 h            50 y
					
				NOTE: 4 base64 6-bit bytes = 3 8-bit bytes				
					// |    6-bit    |    6-bit    |    6-bit    |    6-bit    |
					// | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 |
					// |    8-bit         |    8-bit        |    8-bit         |
			*/

            int encodeBufSize = m_pEncodeBuffer.Length;

            // Process all bytes.
            for(int i=0;i<count;i++){
                m_pEncode3x8Block[m_OffsetInEncode3x8Block++] = buffer[offset + i];

                // 3x8-bit encode block is full, encode it.
                if(m_OffsetInEncode3x8Block == 3){
                    m_pEncodeBuffer[m_EncodeBufferOffset++] = BASE64_ENCODE_TABLE[ m_pEncode3x8Block[0] >> 2];
                    m_pEncodeBuffer[m_EncodeBufferOffset++] = BASE64_ENCODE_TABLE[(m_pEncode3x8Block[0] & 0x03) << 4 | m_pEncode3x8Block[1] >> 4];
                    m_pEncodeBuffer[m_EncodeBufferOffset++] = BASE64_ENCODE_TABLE[(m_pEncode3x8Block[1] & 0x0F) << 2 | m_pEncode3x8Block[2] >> 6];
                    m_pEncodeBuffer[m_EncodeBufferOffset++] = BASE64_ENCODE_TABLE[(m_pEncode3x8Block[2] & 0x3F)];
                    
                    // Encode buffer is full, write buffer to underlaying stream (we reserved 2 bytes for CRLF).
                    if(m_EncodeBufferOffset >= (encodeBufSize - 2)){
                        if(m_AddLineBreaks){
                            m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)'\r';
                            m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)'\n';
                        }

                        m_pStream.Write(m_pEncodeBuffer,0,m_EncodeBufferOffset);
                        m_EncodeBufferOffset = 0;
                    }

                    m_OffsetInEncode3x8Block = 0;
                }
            }
        }

        #endregion


        #region method Finish

        /// <summary>
        /// Completes encoding. Call this method if all data has written and no more data. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public void Finish()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsFinished){
                return;
            }
            m_IsFinished = true;
            
            // PADD left-over, if any. Write encode buffer to underlaying stream.
            if(m_OffsetInEncode3x8Block == 1){
                m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)BASE64_ENCODE_TABLE[m_pEncode3x8Block[0] >> 2];
                m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)BASE64_ENCODE_TABLE[(m_pEncode3x8Block[0] & 0x03) << 4];
                m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)'=';
                m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)'=';
            }
            else if(m_OffsetInEncode3x8Block == 2){
                m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)BASE64_ENCODE_TABLE[ m_pEncode3x8Block[0] >> 2];
                m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)BASE64_ENCODE_TABLE[(m_pEncode3x8Block[0] & 0x03) << 4 | m_pEncode3x8Block[1] >> 4];
                m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)BASE64_ENCODE_TABLE[(m_pEncode3x8Block[1] & 0x0F) << 2];
                m_pEncodeBuffer[m_EncodeBufferOffset++] = (byte)'=';
            }

            if(m_EncodeBufferOffset > 0){
                m_pStream.Write(m_pEncodeBuffer,0,m_EncodeBufferOffset);
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if this object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get{ return m_IsDisposed; }
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

                return false;
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
        /// Gets the length in bytes of the stream.  This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="NotSupportedException">Is raised when this property is accessed.</exception>
        public override long Length
        { 
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                throw new NotSupportedException();
            } 
        }

        /// <summary>
        /// Gets or sets the position within the current stream. This method is not supported and always throws a NotSupportedException.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="NotSupportedException">Is raised when this property is accessed.</exception>
        public override long Position
        { 
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                throw new NotSupportedException();
            } 

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("SmartStream");
                }

                throw new NotSupportedException();
            }
        }

        #endregion
  
    }
}
