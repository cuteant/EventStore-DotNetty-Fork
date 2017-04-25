using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CuteAnt;
using CuteAnt.Buffers;
using CuteAnt.IO;

namespace EventStore.Common.Utils
{
  internal static class EncodingExtensions
  {
    #region --& GetStringAsync &--

    private const int MAX_BUFFER_SIZE = 1024 * 2;

    public static String GetStringWithBuffer(this Encoding encoding, byte[] bytes, BufferManager bufferManager = null)
    {
      if (bytes == null) { throw new ArgumentNullException(nameof(bytes)); }

      if (bytes.Length <= MAX_BUFFER_SIZE)
      {
        return encoding.GetString(bytes);
      }
      else
      {
        var ms = new MemoryStream(bytes);
        return GetStringWithBuffer(encoding, ms, bufferManager);
      }
    }

    public static String GetStringWithBuffer(this Encoding encoding, byte[] bytes, int index, int count, BufferManager bufferManager = null)
    {
      if (count <= MAX_BUFFER_SIZE)
      {
        return encoding.GetString(bytes, index, count);
      }
      else
      {
        var ms = new MemoryStream(bytes, index, count);
        return GetStringWithBuffer(encoding, ms, bufferManager);
      }
    }

    public static String GetStringWithBuffer(this Encoding encoding, Stream stream, BufferManager bufferManager = null)
    {
      if (stream == null) { throw new ArgumentNullException(nameof(stream)); }
      if (bufferManager == null) { bufferManager = BufferManager.GlobalManager; }

      using (var sw = new StringWriterX())
      {
        // Get a Decoder.
        var decoder = encoding.GetDecoder();

        // Guarantee the output buffer large enough to convert a few characters.
        var UseBufferSize = 64;
        if (UseBufferSize < encoding.GetMaxCharCount(10)) { UseBufferSize = encoding.GetMaxCharCount(10); }
        var chars = new Char[UseBufferSize];

        // Intentionally make the input byte buffer larger than the output character buffer so the 
        // conversion loop executes more than one cycle. 
        //var bytes = new Byte[UseBufferSize * 4];
        var bufferSize = UseBufferSize * 4;
        var bytes = bufferManager.TakeBuffer(bufferSize);

        Int32 bytesRead;
        do
        {
          // Read at most the number of bytes that will fit in the input buffer. The 
          // return value is the actual number of bytes read, or zero if no bytes remain. 
          bytesRead = stream.Read(bytes, 0, bufferSize);

          var completed = false;
          var byteIndex = 0;

          while (!completed)
          {
            // If this is the last input data, flush the decoder's internal buffer and state.
            var flush = (bytesRead == 0);
            decoder.Convert(bytes, byteIndex, bytesRead - byteIndex,
                            chars, 0, UseBufferSize, flush,
                            out int bytesUsed, out int charsUsed, out completed);

            // The conversion produced the number of characters indicated by charsUsed. Write that number
            // of characters to the output file.
            sw.Write(chars, 0, charsUsed);

            // Increment byteIndex to the next block of bytes in the input buffer, if any, to convert.
            byteIndex += bytesUsed;
          }
        }
        while (bytesRead != 0);

        bufferManager.ReturnBuffer(bytes);

        return sw.ToString();
      }
    }

    /// <summary>Gets String from the stream.</summary>
    /// <param name="encoding">The text encoding to decode the stream.</param>
    /// <param name="stream">stream</param>
    /// <param name="bufferManager">bufferManager</param>
    /// <returns>the decoded String</returns>
    public static async Task<String> GetStringAsync(this Encoding encoding, Stream stream, BufferManager bufferManager = null)
    {
      if (stream == null) { throw new ArgumentNullException(nameof(stream)); }
      if (bufferManager == null) { bufferManager = BufferManager.GlobalManager; }

      using (var sw = new StringWriterX())
      {
        // Get a Decoder.
        var decoder = encoding.GetDecoder();

        // Guarantee the output buffer large enough to convert a few characters.
        var UseBufferSize = 64;
        if (UseBufferSize < encoding.GetMaxCharCount(10)) { UseBufferSize = encoding.GetMaxCharCount(10); }
        var chars = new Char[UseBufferSize];

        // Intentionally make the input byte buffer larger than the output character buffer so the 
        // conversion loop executes more than one cycle. 
        //var bytes = new Byte[UseBufferSize * 4];
        var bufferSize = UseBufferSize * 4;
        var bytes = bufferManager.TakeBuffer(bufferSize);

        Int32 bytesRead;
        do
        {
          // Read at most the number of bytes that will fit in the input buffer. The 
          // return value is the actual number of bytes read, or zero if no bytes remain. 
          bytesRead = await stream.ReadAsync(bytes, 0, bufferSize);

          var completed = false;
          var byteIndex = 0;

          while (!completed)
          {
            // If this is the last input data, flush the decoder's internal buffer and state.
            var flush = (bytesRead == 0);
            decoder.Convert(bytes, byteIndex, bytesRead - byteIndex,
                            chars, 0, UseBufferSize, flush,
                            out int bytesUsed, out int charsUsed, out completed);

            // The conversion produced the number of characters indicated by charsUsed. Write that number
            // of characters to the output file.
            await sw.WriteAsync(chars, 0, charsUsed);

            // Increment byteIndex to the next block of bytes in the input buffer, if any, to convert.
            byteIndex += bytesUsed;
          }
        }
        while (bytesRead != 0);

        bufferManager.ReturnBuffer(bytes);

        return sw.ToString();
      }
    }

    /// <summary>Gets String from the stream.</summary>
    /// <param name="encoding">The text encoding to decode the stream.</param>
    /// <param name="stream">stream</param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <param name="bufferManager">bufferManager</param>
    /// <returns>the decoded String</returns>
    public static async Task<String> GetStringAsync(this Encoding encoding, Stream stream, Int64 offset, Int64 count, BufferManager bufferManager = null)
    {
      if (stream == null) { throw new ArgumentNullException(nameof(stream)); }
      if (offset < 0L) { throw new ArgumentOutOfRangeException("Offset points before the start of the stream."); }
      if (offset > stream.Length) { throw new ArgumentOutOfRangeException("Offset points beyond the end of the stream."); }

      if (bufferManager == null) { bufferManager = BufferManager.GlobalManager; }

      using (var sw = new StringWriterX())
      {
        //stream.Seek(offset, SeekOrigin.Begin);
        stream.Position = offset;
        var max = offset + count;
        if (max > stream.Length) { max = stream.Length; }

        // Get a Decoder.
        var decoder = encoding.GetDecoder();

        // Guarantee the output buffer large enough to convert a few characters.
        var UseBufferSize = 64;
        if (UseBufferSize < encoding.GetMaxCharCount(10)) { UseBufferSize = encoding.GetMaxCharCount(10); }
        var chars = new Char[UseBufferSize];

        // Intentionally make the input byte buffer larger than the output character buffer so the 
        // conversion loop executes more than one cycle. 
        var bufferSize = UseBufferSize * 4;
        //var bytes = new Byte[bufferSize];
        var bytes = bufferManager.TakeBuffer(bufferSize);
        var total = offset;

        while (true)
        {
          var bytesRead = bufferSize;
          if (total >= max) { break; }

          // 最后一次读取大小不同
          if (bytesRead > max - total) { bytesRead = (Int32)(max - total); }

          // Read at most the number of bytes that will fit in the input buffer. The 
          // return value is the actual number of bytes read, or zero if no bytes remain. 
          bytesRead = await stream.ReadAsync(bytes, 0, bytesRead);
          if (bytesRead <= 0) { break; }
          total += bytesRead;

          var completed = false;
          var byteIndex = 0;

          while (!completed)
          {
            // If this is the last input data, flush the decoder's internal buffer and state.
            var flush = (bytesRead == 0);
            decoder.Convert(bytes, byteIndex, bytesRead - byteIndex,
                            chars, 0, UseBufferSize, flush,
                            out int bytesUsed, out int charsUsed, out completed);

            // The conversion produced the number of characters indicated by charsUsed. Write that number
            // of characters to the output file.
            await sw.WriteAsync(chars, 0, charsUsed);

            // Increment byteIndex to the next block of bytes in the input buffer, if any, to convert.
            byteIndex += bytesUsed;
          }
        }

        bufferManager.ReturnBuffer(bytes);

        return sw.ToString();
      }
    }

    #endregion

    #region --& GetBytes &--

    private const int c_maxCharBufferSize = 1024 * 80;

    /// <summary>Encodes a set of characters from the specified character array into the specified byte array</summary>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="chars">The character array containing the set of characters to encode.</param>
    /// <param name="bufferManager">The buffer manager.</param>
    /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
    public static ArraySegmentWrapper<Byte> GetBytes(this Encoding encoding, Char[] chars, BufferManager bufferManager)
    {
      return GetBytes(encoding, chars, 0, chars.Length, bufferManager);
    }

    /// <summary>Encodes a set of characters from the specified character array into the specified byte array</summary>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="chars">The character array containing the set of characters to encode.</param>
    /// <param name="charIndex">The index of the first character to encode.</param>
    /// <param name="charCount">The number of characters to encode.</param>
    /// <param name="bufferManager">The buffer manager.</param>
    /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
    public static ArraySegmentWrapper<Byte> GetBytes(this Encoding encoding, Char[] chars, Int32 charIndex, Int32 charCount, BufferManager bufferManager)
    {
      if (charCount < 0) { throw new ArgumentOutOfRangeException(nameof(charCount), "Value must be non-negative."); }
      if (chars == null || !chars.Any() || encoding == null) { return ArraySegmentWrapper<Byte>.Empty; }

      var bufferSize = encoding.GetMaxByteCount(charCount);
      if (bufferSize > c_maxCharBufferSize)
      {
        bufferSize = encoding.GetByteCount(chars, charIndex, charCount);
      }
      var buffer = bufferManager.TakeBuffer(bufferSize);
      try
      {
        var bytesCount = encoding.GetBytes(chars, charIndex, charCount, buffer, 0);
        return new ArraySegmentWrapper<Byte>(buffer, 0, bytesCount);
      }
      catch (Exception ex)
      {
        bufferManager.ReturnBuffer(buffer);
        throw ex;
      }
    }

    /// <summary>Encodes a set of characters from the specified string into the specified byte array</summary>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="s">The string containing the set of characters to encode.</param>
    /// <param name="bufferManager">The buffer manager.</param>
    /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
    public static ArraySegmentWrapper<Byte> GetBytes(this Encoding encoding, String s, BufferManager bufferManager)
    {
      return GetBytes(encoding, s, 0, s.Length, bufferManager);
    }

    /// <summary>Encodes a set of characters from the specified string into the specified byte array</summary>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="s">The string containing the set of characters to encode.</param>
    /// <param name="charIndex">The index of the first character to encode.</param>
    /// <param name="charCount">The number of characters to encode.</param>
    /// <param name="bufferManager">The buffer manager.</param>
    /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
    public static ArraySegmentWrapper<Byte> GetBytes(this Encoding encoding, String s, Int32 charIndex, Int32 charCount, BufferManager bufferManager)
    {
      if (charCount < 0) { throw new ArgumentOutOfRangeException(nameof(charCount), "Value must be non-negative."); }
      if (String.IsNullOrEmpty(s) || encoding == null) { return ArraySegmentWrapper<Byte>.Empty; }

      var bufferSize = encoding.GetMaxByteCount(charCount);
      if (bufferSize > c_maxCharBufferSize)
      {
        bufferSize = encoding.GetByteCount(s.ToCharArray(), charIndex, charCount);
      }
      var buffer = bufferManager.TakeBuffer(bufferSize);
      try
      {
        var bytesCount = encoding.GetBytes(s, charIndex, charCount, buffer, 0);
        return new ArraySegmentWrapper<Byte>(buffer, 0, bytesCount);
      }
      catch (Exception ex)
      {
        bufferManager.ReturnBuffer(buffer);
        throw ex;
      }
    }

    #endregion
  }
}
