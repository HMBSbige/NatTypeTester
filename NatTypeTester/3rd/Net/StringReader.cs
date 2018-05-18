using System;
using System.Text;

namespace LumiSoft.Net
{
	/// <summary>
	/// String reader.
	/// </summary>
	public class StringReader
	{
        private string m_OriginalString = "";
		private string m_SourceString   = "";

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="source">Source string.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>source</b> is null.</exception>
		public StringReader(string source)
		{	
            if(source == null){
                throw new ArgumentNullException("source");
            }

            m_OriginalString = source;
			m_SourceString   = source;
		}


		#region method AppendString

        /// <summary>
		/// Appends specified string to SourceString.
		/// </summary>
		/// <param name="value">String value to append.</param>
		public void AppendString(string value)
		{
			m_SourceString += value;
		}

		#endregion


		#region method ReadToFirstChar

		/// <summary>
		/// Reads to first char, skips white-space(SP,VTAB,HTAB,CR,LF) from the beginning of source string.
		/// </summary>
        /// <returns>Returns white-space chars which was readed.</returns>
		public string ReadToFirstChar()
		{   
            int whiteSpaces = 0;
            for(int i=0;i<m_SourceString.Length;i++){
                if(char.IsWhiteSpace(m_SourceString[i])){
                    whiteSpaces++;
                }
                else{
                    break;
                }
            }
     
            string whiteSpaceChars = m_SourceString.Substring(0,whiteSpaces);
			m_SourceString = m_SourceString.Substring(whiteSpaces);

            return whiteSpaceChars;
		}

		#endregion

	//	public string ReadToDelimiter(char delimiter)
	//	{
	//	}

		#region method ReadSpecifiedLength

		/// <summary>
		/// Reads string with specified length. Throws exception if read length is bigger than source string length.
		/// </summary>
		/// <param name="length">Number of chars to read.</param>
		/// <returns></returns>
		public string ReadSpecifiedLength(int length)
		{
			if(m_SourceString.Length >= length){
				string retVal = m_SourceString.Substring(0,length);
				m_SourceString = m_SourceString.Substring(length);

				return retVal;
			}
			else{
				throw new Exception("Read length can't be bigger than source string !");
			}
		}

		#endregion

		#region method QuotedReadToDelimiter

        /// <summary>
		/// Reads string to specified delimiter or to end of underlying string. Notes: Delimiter in quoted string is skipped.
        /// Delimiter is removed by default.
		/// For example: delimiter = ',', text = '"aaaa,eee",qqqq' - then result is '"aaaa,eee"'.
		/// </summary>
		/// <param name="delimiter">Data delimiter.</param>
		/// <returns></returns>
		public string QuotedReadToDelimiter(char delimiter)
		{
            return QuotedReadToDelimiter(new char[]{delimiter});
        }

        /// <summary>
		/// Reads string to specified delimiter or to end of underlying string. Notes: Delimiters in quoted string is skipped.
        /// Delimiter is removed by default.
		/// For example: delimiter = ',', text = '"aaaa,eee",qqqq' - then result is '"aaaa,eee"'.
		/// </summary>
		/// <param name="delimiters">Data delimiters.</param>
		/// <returns></returns>
		public string QuotedReadToDelimiter(char[] delimiters)
		{
            return QuotedReadToDelimiter(delimiters,true);
        }

		/// <summary>
		/// Reads string to specified delimiter or to end of underlying string. Notes: Delimiters in quoted string is skipped. 
		/// For example: delimiter = ',', text = '"aaaa,eee",qqqq' - then result is '"aaaa,eee"'.
		/// </summary>
		/// <param name="delimiters">Data delimiters.</param>
        /// <param name="removeDelimiter">Specifies if delimiter is removed from underlying string.</param>
		/// <returns></returns>
		public string QuotedReadToDelimiter(char[] delimiters,bool removeDelimiter)
		{
			StringBuilder currentSplitBuffer = new StringBuilder(); // Holds active
			bool          inQuotedString     = false;               // Holds flag if position is quoted string or not
            bool          doEscape           = false;

			for(int i=0;i<m_SourceString.Length;i++){
				char c = m_SourceString[i];

                if(doEscape){
                    currentSplitBuffer.Append(c);
                    doEscape = false;
                }
                else if(c == '\\'){
                    currentSplitBuffer.Append(c);
                    doEscape = true;
                }
                else{
                    // Start/end quoted string area
				    if(c == '\"'){					
					    inQuotedString = !inQuotedString;
				    }
			
                    // See if char is delimiter
                    bool isDelimiter = false;
                    foreach(char delimiter in delimiters){
                        if(c == delimiter){
                            isDelimiter = true;
                            break;
                        }
                    }

				    // Current char is split char and it isn't in quoted string, do split
				    if(!inQuotedString && isDelimiter){
					    string retVal = currentSplitBuffer.ToString();
                                
					    // Remove readed string + delimiter from source string
                        if(removeDelimiter){
					        m_SourceString = m_SourceString.Substring(i + 1);
                        }
                        // Remove readed string
                        else{
                            m_SourceString = m_SourceString.Substring(i); 
                        }

					    return retVal;
				    }
				    else{
					    currentSplitBuffer.Append(c);
				    }
                }				
			}

			// If we reached so far then we are end of string, return it
			m_SourceString = "";
			return currentSplitBuffer.ToString();
		}

		#endregion
        
		#region method ReadWord

        /// <summary>
		/// Reads word from string. Returns null if no word is available.
		/// Word reading begins from first char, for example if SP"text", then space is trimmed.
		/// </summary>
		/// <returns></returns>
        public string ReadWord()
		{
            return ReadWord(true);
        }

        /// <summary>
		/// Reads word from string. Returns null if no word is available.
		/// Word reading begins from first char, for example if SP"text", then space is trimmed.
		/// </summary>
		/// <param name="unQuote">Specifies if quoted string word is unquoted.</param>
		/// <returns></returns>
		public string ReadWord(bool unQuote)
		{
            return ReadWord(unQuote,new char[]{' ',',',';','{','}','(',')','[',']','<','>','\r','\n'},false);
        }

		/// <summary>
		/// Reads word from string. Returns null if no word is available.
		/// Word reading begins from first char, for example if SP"text", then space is trimmed.
		/// </summary>
		/// <param name="unQuote">Specifies if quoted string word is unquoted.</param>
		/// <param name="wordTerminatorChars">Specifies chars what terminate word.</param>
		/// <param name="removeWordTerminator">Specifies if work terminator is removed.</param>
		/// <returns></returns>
		public string ReadWord(bool unQuote,char[] wordTerminatorChars,bool removeWordTerminator)
		{			
			// Always start word reading from first char.
			this.ReadToFirstChar();

            if(this.Available == 0){
				return null;
			}
            			            
			// quoted word can contain any char, " must be escaped with \
			// unqouted word can conatin any char except: SP VTAB HTAB,{}()[]<>

			if(m_SourceString.StartsWith("\"")){
                if(unQuote){
                    return TextUtils.UnQuoteString(QuotedReadToDelimiter(wordTerminatorChars,removeWordTerminator));
                }
                else{
                    return QuotedReadToDelimiter(wordTerminatorChars,removeWordTerminator);
                }				
			}
			else{
				int wordLength = 0;
				for(int i=0;i<m_SourceString.Length;i++){
					char c = m_SourceString[i];

                    bool isTerminator = false;
                    foreach(char terminator in wordTerminatorChars){
                        if(c == terminator){
                            isTerminator = true;
                            break;
                        }
                    }
                    if(isTerminator){
                        break;
                    }

					wordLength++;
				}
				
				string retVal = m_SourceString.Substring(0,wordLength);
                if(removeWordTerminator){
                    if(m_SourceString.Length >= wordLength + 1){
                        m_SourceString = m_SourceString.Substring(wordLength + 1);
                    }
                }
                else{
				    m_SourceString = m_SourceString.Substring(wordLength);
                }

				return retVal;
			}			
		}

		#endregion
	
		#region method ReadParenthesized

		/// <summary>
		/// Reads parenthesized value. Supports {},(),[],&lt;&gt; parenthesis. 
		/// Throws exception if there isn't parenthesized value or closing parenthesize is missing.
		/// </summary>
		/// <returns></returns>
		public string ReadParenthesized()
		{
            ReadToFirstChar();

			char startingChar = ' ';
			char closingChar  = ' ';

			if(m_SourceString.StartsWith("{")){
				startingChar = '{';
				closingChar = '}';
			}
			else if(m_SourceString.StartsWith("(")){
				startingChar = '(';
				closingChar = ')';
			}			
			else if(m_SourceString.StartsWith("[")){
				startingChar = '[';
				closingChar = ']';
			}						
			else if(m_SourceString.StartsWith("<")){
				startingChar = '<';
				closingChar = '>';
			}
			else{
				throw new Exception("No parenthesized value '" + m_SourceString + "' !");
			}

			bool inQuotedString = false; // Holds flag if position is quoted string or not
            bool skipNextChar   = false;

			int closingCharIndex = -1;
			int nestedStartingCharCounter = 0;
			for(int i=1;i<m_SourceString.Length;i++){
                // Skip this char.
                if(skipNextChar){
                    skipNextChar = false;
                }
                // We have char escape '\', skip next char.
                else if(m_SourceString[i] == '\\'){
                    skipNextChar = true;
                }
                // Start/end quoted string area
                else if(m_SourceString[i] == '\"'){
                    inQuotedString = !inQuotedString;
                }
				// We need to skip parenthesis in quoted string
				else if(!inQuotedString){
					// There is nested parenthesis
					if(m_SourceString[i] == startingChar){
						nestedStartingCharCounter++;
					}
					// Closing char
					else if(m_SourceString[i] == closingChar){
						// There isn't nested parenthesis closing chars left, this is closing char what we want
						if(nestedStartingCharCounter == 0){
							closingCharIndex = i;
							break;
						}
						// This is nested parenthesis closing char
						else{
							nestedStartingCharCounter--;
						}
					}
				}
			}

			if(closingCharIndex == -1){
				throw new Exception("There is no closing parenthesize for '" + m_SourceString + "' !");
			}
			else{
				string retVal = m_SourceString.Substring(1,closingCharIndex - 1);
				m_SourceString = m_SourceString.Substring(closingCharIndex + 1);

				return retVal;
			}
		}

		#endregion
        
        #region method ReadToEnd

        /// <summary>
        /// Reads all remaining string, returns null if no chars left to read.
        /// </summary>
        /// <returns></returns>
        public string ReadToEnd()
        {
            if(this.Available == 0){
				return null;
			}

            string retVal = m_SourceString;
            m_SourceString = "";

            return retVal;
        }

        #endregion

        #region method RemoveFromEnd

        /// <summary>
        /// Removes specified count of chars from the end of the source string.
        /// </summary>
        /// <param name="count">Char count.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void RemoveFromEnd(int count)
        {
            if(count < 0){
                throw new ArgumentException("Argument 'count' value must be >= 0.","count");
            }

            m_SourceString = m_SourceString.Substring(0,m_SourceString.Length - count);
        }

        #endregion


        #region method StartsWith

        /// <summary>
		/// Gets if source string starts with specified value. Compare is case-sensitive.
		/// </summary>
		/// <param name="value">Start string value.</param>
		/// <returns>Returns true if source string starts with specified value.</returns>
		public bool StartsWith(string value)
		{
			return m_SourceString.StartsWith(value);
		}
		
		/// <summary>
		/// Gets if source string starts with specified value.
		/// </summary>
		/// <param name="value">Start string value.</param>
		/// <param name="case_sensitive">Specifies if compare is case-sensitive.</param>
		/// <returns>Returns true if source string starts with specified value.</returns>
		public bool StartsWith(string value,bool case_sensitive)
		{
			if(case_sensitive){
				return m_SourceString.StartsWith(value);
			}
			else{
				return m_SourceString.ToLower().StartsWith(value.ToLower());
			}
		}

		#endregion

        #region method EndsWith

        /// <summary>
		/// Gets if source string ends with specified value. Compare is case-sensitive.
		/// </summary>
		/// <param name="value">Start string value.</param>
		/// <returns>Returns true if source string ends with specified value.</returns>
		public bool EndsWith(string value)
		{
			return m_SourceString.EndsWith(value);
		}
		
		/// <summary>
		/// Gets if source string ends with specified value.
		/// </summary>
		/// <param name="value">Start string value.</param>
		/// <param name="case_sensitive">Specifies if compare is case-sensitive.</param>
		/// <returns>Returns true if source string ends with specified value.</returns>
		public bool EndsWith(string value,bool case_sensitive)
		{
			if(case_sensitive){
				return m_SourceString.EndsWith(value);
			}
			else{
				return m_SourceString.EndsWith(value,StringComparison.InvariantCultureIgnoreCase);
			}
		}

		#endregion

        #region method StartsWithWord

        /// <summary>
        /// Gets if current source string starts with word. For example if source string starts with
        /// whiter space or parenthesize, this method returns false.
        /// </summary>
        /// <returns></returns>
        public bool StartsWithWord()
        {
            if(m_SourceString.Length == 0){
                return false;
            }
                        
            if(char.IsWhiteSpace(m_SourceString[0])){
                return false;
            }
            if(char.IsSeparator(m_SourceString[0])){
                return false;
            }
            char[] wordTerminators = new char[]{' ',',',';','{','}','(',')','[',']','<','>','\r','\n'};
            foreach(char c in wordTerminators){
                if(c == m_SourceString[0]){
                    return false;
                }
            }

            return true;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
		/// Gets how many chars are available for reading.
		/// </summary>
		public long Available
		{
			get{ return m_SourceString.Length; }
		}

        /// <summary>
        /// Gets original string passed to class constructor.
        /// </summary>
        public string OriginalString
        {
            get{ return m_OriginalString; }
        }

		/// <summary>
		/// Gets currently remaining string.
		/// </summary>
		public string SourceString
		{
			get{ return m_SourceString; }
		}

        /// <summary>
        /// Gets position in original string.
        /// </summary>
        public int Position
        {
            get{ return m_OriginalString.Length - m_SourceString.Length; }
        }

		#endregion

	}
}
