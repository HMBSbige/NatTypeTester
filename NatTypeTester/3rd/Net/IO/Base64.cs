using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// This class implements base64 encoder/decoder.  Defined in RFC 4648.
    /// </summary>
    public class Base64
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

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Base64()
        {
        }


        #region method Encode

        /// <summary>
        /// Encodes bytes.
        /// </summary>
        /// <param name="buffer">Data buffer.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes available in the buffer.</param>
        /// <param name="last">Last data block.</param>
        /// <returns>Returns encoded data.</returns>
        public byte[] Encode(byte[] buffer,int offset,int count,bool last)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region method Decode

        /// <summary>
        /// Decodes specified base64 string.
        /// </summary>
        /// <param name="value">Base64 string.</param>
        /// <param name="ignoreNonBase64Chars">If true all invalid base64 chars ignored. If false, FormatException is raised.</param>
        /// <returns>Returns decoded data.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        /// <exception cref="FormatException">Is raised when <b>value</b> contains invalid base64 data.</exception>
        public byte[] Decode(string value,bool ignoreNonBase64Chars)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            byte[] encBuffer = Encoding.ASCII.GetBytes(value);
            byte[] buffer    = new byte[encBuffer.Length];

            int decodedCount = Decode(encBuffer,0,encBuffer.Length,buffer,0,ignoreNonBase64Chars);
            byte[] retVal = new byte[decodedCount];
            Array.Copy(buffer,retVal,decodedCount);

            return retVal;
        }

        /// <summary>
        /// Decodes specified base64 data.
        /// </summary>
        /// <param name="data">Base64 encoded data buffer.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes available in the buffer.</param>
        /// <param name="ignoreNonBase64Chars">If true all invalid base64 chars ignored. If false, FormatException is raised.</param>
        /// <returns>Returns decoded data.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>data</b> is null reference.</exception>
        /// <exception cref="FormatException">Is raised when <b>value</b> contains invalid base64 data.</exception>
        public byte[] Decode(byte[] data,int offset,int count,bool ignoreNonBase64Chars)
        {
            if(data == null){
                throw new ArgumentNullException("data");
            }

            byte[] buffer = new byte[data.Length];

            int decodedCount = Decode(data,offset,count,buffer,0,ignoreNonBase64Chars);
            byte[] retVal = new byte[decodedCount];
            Array.Copy(buffer,retVal,decodedCount);

            return retVal;
        }

        /// <summary>
        /// Decodes base64 encoded bytes.
        /// </summary>
        /// <param name="encBuffer">Base64 encoded data buffer.</param>
        /// <param name="encOffset">Offset in the encBuffer.</param>
        /// <param name="encCount">Number of bytes available in the encBuffer.</param>
        /// <param name="buffer">Buffer where to decode data.</param>
        /// <param name="offset">Offset int the buffer.</param>
        /// <param name="ignoreNonBase64Chars">If true all invalid base64 chars ignored. If false, FormatException is raised.</param>
        /// <returns>Returns number of bytes decoded.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>encBuffer</b> or <b>encBuffer</b> is null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the arguments has out of valid range.</exception>
        /// <exception cref="FormatException">Is raised when <b>encBuffer</b> contains invalid base64 data.</exception>
        public int Decode(byte[] encBuffer,int encOffset,int encCount,byte[] buffer,int offset,bool ignoreNonBase64Chars)
        {
            if(encBuffer == null){
                throw new ArgumentNullException("encBuffer");
            }           
            if(encOffset < 0){
                throw new ArgumentOutOfRangeException("encOffset","Argument 'encOffset' value must be >= 0.");
            }
            if(encCount < 0){
                throw new ArgumentOutOfRangeException("encCount","Argument 'encCount' value must be >= 0.");
            }
            if(encOffset + encCount > encBuffer.Length){
                throw new ArgumentOutOfRangeException("encCount","Argument 'count' is bigger than than argument 'encBuffer'.");
            }
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0 || offset >= buffer.Length){
                throw new ArgumentOutOfRangeException("offset");
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

            int    decodeOffset  = encOffset;
            int    decodedOffset = 0;
            byte[] base64Block   = new byte[4];

            // Decode while we have data.
            while((decodeOffset - encOffset) < encCount){
                // Read 4-byte base64 block.
                int offsetInBlock = 0;
                while(offsetInBlock < 4){
                    // Check that we won't exceed buffer data.
                    if((decodeOffset - encOffset) >= encCount){
                        if(offsetInBlock == 0){
                            break;
                        }
                        // Incomplete 4-byte base64 data block.
                        else{
                            throw new FormatException("Invalid incomplete base64 4-char block");
                        }
                    }

                    // Read byte.
                    short b = encBuffer[decodeOffset++];
             
                    // Pad char.
                    if(b == '='){
                        // Padding may appear only in last two chars of 4-char block.
                        // ab==
                        // abc=
                        if(offsetInBlock < 2){
                            throw new FormatException("Invalid base64 padding.");
                        }
                                                                        
                        // Skip next padding char.
                        if(offsetInBlock == 2){
                            decodeOffset++;
                        }
                        
                        break;
                    }
                    // Non-base64 char.
                    else if(b > 127 || BASE64_DECODE_TABLE[b] == -1){
                        if(!ignoreNonBase64Chars){
                            throw new FormatException("Invalid base64 char '" + b + "'.");
                        }
                        // Igonre that char.
                        //else{
                    }
                    // Base64 char.
                    else{
                        base64Block[offsetInBlock++] = (byte)BASE64_DECODE_TABLE[b];
                    }
                }

                // Decode base64 block.
                if(offsetInBlock > 1){
                    buffer[decodedOffset++] = (byte)((base64Block[0] << 2) | (base64Block[1] >> 4));
                }
                if(offsetInBlock > 2){
                    buffer[decodedOffset++] = (byte)(((base64Block[1] & 0xF) << 4) | (base64Block[2] >> 2));
                }
                if(offsetInBlock > 3){
                    buffer[decodedOffset++] = (byte)(((base64Block[2] & 0x3) << 6) | base64Block[3]);
                }
            }

            return decodedOffset;
        }

        #endregion
    }
}
