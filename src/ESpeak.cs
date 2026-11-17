using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

#if WINDOWS
using wchar_t = System.UInt16;
#else
using wchar_t = System.UInt32;
#endif

namespace ASESpeak;

public static class ESpeak
{
    public enum Rate
    {
        Minimum = ESpeakSys.espeakRATE_MINIMUM,
        Maximum = ESpeakSys.espeakRATE_MAXIMUM,
        Normal = ESpeakSys.espeakRATE_NORMAL,
    };

    public enum EventType
    {
        ListTerminated = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_LIST_TERMINATED,
        Word = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_WORD,
        Sentence = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_SENTENCE,
        Mark = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_MARK,
        Play = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_PLAY,
        End = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_END,
        MsgTerminated = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_MSG_TERMINATED,
        Phoneme = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_PHONEME,
        SampleRate = ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_SAMPLERATE
    };

    /// <summary>
    /// An event passed to the synthesis callback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a message is supplied to espeak_synth, the request is buffered and espeak_synth returns.
    /// When the message is really processed, the callback function will be repeatedly called.
    /// </para>
    /// <para>
    /// In RETRIEVAL mode, the callback function supplies to the calling program the audio data and
    /// an event list terminated by 0 (LIST_TERMINATED).
    /// </para>
    /// <para>
    /// In PLAYBACK mode, the callback function is called as soon as an event happens.
    /// </para>
    /// <para>
    /// For example suppose that the following message is supplied to espeak_Synth: "hello, hello."
    /// </para>
    /// <para>
    /// Once processed in RETRIEVAL mode, it could lead to 3 calls of the callback function:
    /// </para>
    /// <list type="number">
    ///   <item><description>Block 1: &lt;audio data&gt; + List of events: SENTENCE + WORD + LIST_TERMINATED</description></item>
    ///   <item><description>Block 2: &lt;audio data&gt; + List of events: WORD + END + LIST_TERMINATED</description></item>
    ///   <item><description>Block 3: no audio data. List of events: MSG_TERMINATED + LIST_TERMINATED</description></item>
    /// </list>
    /// <para>
    /// Once processed in PLAYBACK mode, it could lead to 5 calls of the callback function:
    /// </para>
    /// <list type="number">
    ///   <item><description>SENTENCE</description></item>
    ///   <item><description>WORD (call when the sounds are actually played)</description></item>
    ///   <item><description>WORD</description></item>
    ///   <item><description>END (call when the end of sentence is actually played.)</description></item>
    ///   <item><description>MSG_TERMINATED</description></item>
    /// </list>
    /// <para>
    /// The MSG_TERMINATED event is the last event. It can inform the calling program to clear the user
    /// data related to the message. So if the synthesis must be stopped, the callback function is called
    /// for each pending message with the MSG_TERMINATED event.
    /// </para>
    /// <para>
    /// A MARK event indicates a &lt;mark&gt; element in the text.
    /// A PLAY event indicates an &lt;audio&gt; element in the text, for which the calling program should
    /// play the named sound file.
    /// </para>
    /// </remarks>
    public struct Event
    {
        ESpeakSys.espeak_EVENT inner;

        public EventType Type
        {
            readonly get { return (EventType)inner.type; }
            set { inner.type = (ESpeakSys.espeak_EVENT_TYPE)value; }
        }
        /// <summary>Message identifier (or 0 for key or character).</summary>
        public uint UniqueIdentifier
        {
            readonly get { return inner.unique_identifier; }
            set { inner.unique_identifier = value; }
        }
        /// <summary>The number of characters from the start of the text.</summary>
        public int TextPosition
        {
            readonly get { return inner.text_position; }
            set { inner.text_position = value; }
        }
        /// <summary>Word length, in characters (for <see cref="EventType.Word"/>).</summary>
        public int Length
        {
            readonly get { return inner.length; }
            set { inner.length = value; }
        }
        /// <summary>The time in mS within the generated speech output data.</summary>
        public int AudioPosition
        {
            readonly get { return inner.audio_position; }
            set { inner.audio_position = value; }
        }
        /// <summary>Sample id (internal use).</summary>
        public int Sample
        {
            readonly get { return inner.sample; }
            set { inner.sample = value; }
        }
        /// <summary>Pointer supplied by the calling program.</summary>
        public IntPtr user_data
        {
            readonly get { return inner.user_data; }
            set { inner.user_data = value; }
        }
        // public espeak_EVENT_ID id;
    }

    public enum PositionType
    {
        Character = ESpeakSys.espeak_POSITION_TYPE.POS_CHARACTER,
        Word = ESpeakSys.espeak_POSITION_TYPE.POS_WORD,
        Sentence = ESpeakSys.espeak_POSITION_TYPE.POS_SENTENCE,
    }
    public enum AudioOutput
    {
#if ESPEAK_USE_LIBPCAUDIO
        /// <summary>Plays the audio data, supplies events to the calling program. Requires espeak-ng built with libpcaudio.</summary>
        Playback = ESpeakSys.espeak_AUDIO_OUTPUT.AUDIO_OUTPUT_PLAYBACK,
#endif
        Retrieval = ESpeakSys.espeak_AUDIO_OUTPUT.AUDIO_OUTPUT_RETRIEVAL,
        Synchronous = ESpeakSys.espeak_AUDIO_OUTPUT.AUDIO_OUTPUT_SYNCHRONOUS,
#if ESPEAK_USE_LIBPCAUDIO
        /// <summary>Synchronous playback. Requires espeak-ng built with libpcaudio.</summary>
        SynthPlayback = ESpeakSys.espeak_AUDIO_OUTPUT.AUDIO_OUTPUT_SYNCH_PLAYBACK,
#endif
    }

    public enum Error
    {
        OK = ESpeakSys.espeak_ERROR.EE_OK,
        InternalError = ESpeakSys.espeak_ERROR.EE_INTERNAL_ERROR,
        BufferFull = ESpeakSys.espeak_ERROR.EE_BUFFER_FULL,
        NotFound = ESpeakSys.espeak_ERROR.EE_NOT_FOUND
    }

    [Flags]
    public enum InitializeFlags
    {
        /// <summary>Allow <see cref="EventType.Phoneme"/> events.</summary>
        PhonemeEvents = ESpeakSys.espeakINITIALIZE_PHONEME_EVENTS,
        /// <summary><see cref="EventType.Phoneme"/> events give IPA phoneme names, not eSpeak phoneme names.</summary>
        PhonemeIPA = ESpeakSys.espeakINITIALIZE_PHONEME_IPA,
        /// <summary>Don't exit if espeak_data is not found (used for --help).</summary>
        DontExit = ESpeakSys.espeakINITIALIZE_DONT_EXIT
    };

    /// <summary>
    /// Must be called before any synthesis functions are called.
    /// </summary>
    /// <param name="output">The audio data can either be played by eSpeak or passed back by the SynthCallback function.</param>
    /// <param name="buflength">
    /// The length in mS of sound buffers passed to the SynthCallback function.
    /// Value=0 gives a default of 60mS.
    /// This parameter is only used for <see cref="AudioOutput.Retrieval"/> and <see cref="AudioOutput.Synchronous"/> modes.
    /// </param>
    /// <param name="path">
    /// The directory which contains the espeak-ng-data directory, or <see langword="null"/> for the espeak-ng-data
    /// copied next to the library by this project's build (<see cref="ESpeakSys.LibraryDirectory"/>), falling back
    /// to eSpeak's compiled-in default location if it isn't there.
    /// </param>
    /// <param name="flags">
    /// A combination of <see cref="InitializeFlags"/>. <see cref="InitializeFlags.DontExit"/> is always added,
    /// since otherwise eSpeak calls exit() (killing the whole process) if espeak-ng-data cannot be loaded.
    /// </param>
    /// <returns>
    /// Sample rate in Hz. Note that eSpeak does not report a failure to load espeak-ng-data here: it still
    /// returns the sample rate. Check the result of selecting a voice (e.g. <see cref="SetVoiceByName"/>) instead.
    /// </returns>
    public static int Initialize(AudioOutput output, int buflength, String? path, InitializeFlags flags)
    {
        if (path is null && Directory.Exists(Path.Combine(ESpeakSys.LibraryDirectory, "espeak-ng-data")))
            path = ESpeakSys.LibraryDirectory;
        byte[]? buf = path is null ? null : ToUtf8z(path);
        unsafe
        {
            fixed (byte* p = buf)

                return ESpeakSys.espeak_Initialize((ESpeakSys.espeak_AUDIO_OUTPUT)output, buflength, p,
                    /* C# cannot handle espeak calling exit(1), thus we Always have to add the DontExit flag */
                    (int)(flags | InitializeFlags.DontExit)
                );
        }
    }

    /// <summary>
    /// Called when a buffer of speech sound data has been produced.
    /// </summary>
    /// <param name="wav">The speech sound data which has been produced. NULL indicates that the synthesis has been completed.</param>
    /// <param name="events">
    /// An array of espeak_EVENT items which indicate word and sentence events, and
    /// also the occurrence of &lt;mark&gt; and &lt;audio&gt; elements within the text. The list of
    /// events is terminated by an event of type = 0.
    /// </param>
    /// <returns>0 = continue synthesis, 1 = abort synthesis.</returns>
    public delegate bool SynthCallback(short[] wav, Event[] events);

    private static SynthCallback? synthCallback = null;
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe int SynthTrampoline(short* wav, int n, ESpeakSys.espeak_EVENT* ev)
    {
        SynthCallback? cb = synthCallback;
        if (cb is null)
            return 0;
        try
        {
            int count = 0;
            while (ev[count].type != ESpeakSys.espeak_EVENT_TYPE.espeakEVENT_LIST_TERMINATED) count++;

            var samples = wav is null ? default : new ReadOnlySpan<short>(wav, n);
            var events = new ReadOnlySpan<Event>(ev, count);
            return cb(samples.ToArray(), events.ToArray()) ? 0 : 1;
        }
        catch (Exception e)
        {
            SetPending(e);
            return 1;
        }
    }

    /// <summary>
    /// Must be called before any synthesis functions are called.
    /// This specifies a function in the calling program which is called when a buffer of
    /// speech sound data has been produced.
    /// </summary>
    /// <param name="cb">The callback; see <see cref="SynthCallback"/>.</param>
    public static void SetSynthCallback(SynthCallback cb)
    {
        synthCallback = cb;
        unsafe { ESpeakSys.espeak_SetSynthCallback((synthCallback != null) ? &SynthTrampoline : null); }
    }

    /// <summary>
    /// Called when an &lt;audio&gt; element is encountered.
    /// </summary>
    /// <param name="type">Type of callback event. Currently only 1 = &lt;audio&gt; element.</param>
    /// <param name="uri">The "src" attribute from the &lt;audio&gt; element.</param>
    /// <param name="b">The "xml:base" attribute (if any) from the &lt;speak&gt; element.</param>
    /// <returns>
    /// 1 = don't play the sound, but speak the text alternative.
    /// 0 = place a PLAY event in the event list at the point where the &lt;audio&gt; element
    /// occurs. The calling program can then play the sound at that point.
    /// </returns>
    public delegate int UriCallback(int type, string uri, string? b);

    private static UriCallback? uriCallback = null;
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe int UriTrampoline(int type, byte* uri, byte* b)
    {
        UriCallback? cb = uriCallback;
        if (cb is null)
            return 1;
        try
        {
            string uriStr = Marshal.PtrToStringUTF8((IntPtr)uri) ?? "";
            string? baseStr = Marshal.PtrToStringUTF8((IntPtr)b);
            return cb(type, uriStr, baseStr);
        }
        catch (Exception e)
        {
            SetPending(e);
            return 1;
        }
    }

    /// <summary>
    /// This function may be called before synthesis functions are used, in order to deal with
    /// &lt;audio&gt; tags. It specifies a callback function which is called when an &lt;audio&gt; element is
    /// encountered and allows the calling program to indicate whether the sound file which
    /// is specified in the &lt;audio&gt; element is available and is to be played.
    /// </summary>
    /// <param name="callback">The callback; see <see cref="UriCallback"/>.</param>
    public static void SetUriCallback(UriCallback? callback)
    {
        uriCallback = callback;
        unsafe { ESpeakSys.espeak_SetUriCallback((uriCallback != null) ? &UriTrampoline : null); }
    }

    /// <summary>
    /// Called with the phonemes of each clause as it is translated.
    /// </summary>
    /// <param name="phonemes">The phonemes for the clause.</param>
    public delegate void PhonemeCallback(string phonemes);

    private static PhonemeCallback? s_phoneme = null;
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe int PhonemeTrampoline(byte* phonemes)
    {
        PhonemeCallback? cb = s_phoneme;
        try
        {
            cb?.Invoke(Marshal.PtrToStringUTF8((IntPtr)phonemes) ?? "");
        }
        catch (Exception e)
        {
            SetPending(e);
        }
        return 0;
    }

    /// <summary>
    /// Specifies a function which is called with the phonemes of each clause as it is translated.
    /// </summary>
    /// <param name="callback">The callback, or <see langword="null"/> to remove it; see <see cref="PhonemeCallback"/>.</param>
    public static void SetPhonemeCallback(PhonemeCallback? callback)
    {
        s_phoneme = callback;
        unsafe { ESpeakSys.espeak_SetPhonemeCallback((s_phoneme != null) ? &PhonemeTrampoline : null); }
    }


    // /********************/
    // /*    Synthesis     */
    // /********************/


    public enum Chars
    {
        Auto = ESpeakSys.espeakCHARS_AUTO,
        UTF8 = ESpeakSys.espeakCHARS_UTF8,
        EightBit = ESpeakSys.espeakCHARS_8BIT,
        WChar = ESpeakSys.espeakCHARS_WCHAR,
        SixteenBit = ESpeakSys.espeakCHARS_16BIT
    };

    [Flags]
    public enum SynthFlags
    {
        None = 0,
        /// <summary>Elements within &lt; &gt; are treated as SSML elements, or if not recognised are ignored.</summary>
        SSML = ESpeakSys.espeakSSML,
        /// <summary>Text within [[ ]] is treated as phonemes codes (in espeak's Kirshenbaum encoding).</summary>
        Phonemes = ESpeakSys.espeakPHONEMES,
        /// <summary>If set then a sentence pause is added at the end of the text. If not set then this pause is suppressed.</summary>
        EndPause = ESpeakSys.espeakENDPAUSE,
        KeepNameData = ESpeakSys.espeakKEEP_NAMEDATA,
    };

    /// <summary>
    /// Synthesize speech for the specified text. The speech sound data is passed to the calling
    /// program in buffers by means of the callback function specified by <see cref="SetSynthCallback"/>.
    /// The command is asynchronous: it is internally buffered and returns as soon as possible.
    /// If <see cref="Initialize"/> was previously called with AudioOutput.Playback as argument, the sound
    /// data are played by eSpeak.
    /// </summary>
    /// <param name="text">The text to be spoken.</param>
    /// <param name="uniqueIdentifier">
    /// Receives a message identifier number.
    /// eSpeak includes this number in <see cref="Event"/>s which are the result of this call.
    /// </param>
    /// <param name="flags">These may be OR'd together; see <see cref="SynthFlags"/>.</param>
    /// <param name="position">
    /// The position in the text where speaking starts. Zero indicates speak from the
    /// start of the text.
    /// </param>
    /// <param name="positionType">
    /// Determines whether <paramref name="position"/> is a number of characters, words, or sentences.
    /// </param>
    /// <param name="endPosition">
    /// If set, this gives a character position at which speaking will stop. A value
    /// of zero indicates no end position.
    /// </param>
    /// <param name="userData">
    /// A pointer (or <see cref="IntPtr.Zero"/>) which will be passed to the callback function in
    /// <see cref="Event"/>s.
    /// </param>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.BufferFull"/>: the command can not be buffered; you may try after a while to call the function again.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error Synth(string text, out uint uniqueIdentifier, SynthFlags flags = SynthFlags.None,
        uint position = 0, PositionType positionType = PositionType.Character, uint endPosition = 0, IntPtr userData = default)
    {
        byte[] buf = ToUtf8z(text);
        unsafe
        {
            uint id = 0;
            ESpeakSys.espeak_ERROR err;
            fixed (byte* p = buf)
                err = ESpeakSys.espeak_Synth(p, (nuint)buf.Length, position, (ESpeakSys.espeak_POSITION_TYPE)positionType,
                    endPosition, (uint)Chars.UTF8 | (uint)flags, &id, (void*)userData);
            uniqueIdentifier = id;
            ThrowPending();
            return (Error)err;
        }
    }

    /// <summary>
    /// Synthesize speech for the specified text. Similar to <see cref="Synth"/>
    /// but the start position is specified by the name of a &lt;mark&gt; element in the text.
    /// </summary>
    /// <param name="text">The text to be spoken.</param>
    /// <param name="indexMark">
    /// The "name" attribute of a &lt;mark&gt; element within the text which specified the
    /// point at which synthesis starts.
    /// </param>
    /// <param name="uniqueIdentifier">
    /// Receives a message identifier number.
    /// eSpeak includes this number in <see cref="Event"/>s which are the result of this call.
    /// </param>
    /// <param name="flags">These may be OR'd together; see <see cref="SynthFlags"/>.</param>
    /// <param name="endPosition">
    /// If set, this gives a character position at which speaking will stop. A value
    /// of zero indicates no end position.
    /// </param>
    /// <param name="userData">
    /// A pointer (or <see cref="IntPtr.Zero"/>) which will be passed to the callback function in
    /// <see cref="Event"/>s.
    /// </param>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.BufferFull"/>: the command can not be buffered; you may try after a while to call the function again.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error SynthMark(string text, string indexMark, out uint uniqueIdentifier, SynthFlags flags = SynthFlags.None,
        uint endPosition = 0, IntPtr userData = default)
    {
        byte[] textBuf = ToUtf8z(text);
        byte[] markBuf = ToUtf8z(indexMark);
        unsafe
        {
            uint id = 0;
            ESpeakSys.espeak_ERROR err;
            fixed (byte* t = textBuf)
            fixed (byte* m = markBuf)
                err = ESpeakSys.espeak_Synth_Mark(t, (nuint)textBuf.Length, m, endPosition,
                    (uint)Chars.UTF8 | (uint)flags, &id, (void*)userData);
            uniqueIdentifier = id;
            ThrowPending();
            return (Error)err;
        }
    }

    /// <summary>
    /// Speak the name of a keyboard key.
    /// If <paramref name="keyName"/> is a single character, it speaks the name of the character.
    /// Otherwise, it speaks <paramref name="keyName"/> as a text string.
    /// </summary>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.BufferFull"/>: the command can not be buffered; you may try after a while to call the function again.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error Key(string keyName)
    {
        byte[] buf = ToUtf8z(keyName);
        unsafe
        {
            Error err;
            fixed (byte* p = buf)
                err = (Error)ESpeakSys.espeak_Key(p);
            ThrowPending();
            return err;
        }
    }

    /// <summary>
    /// Speak the name of the given character.
    /// </summary>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.BufferFull"/>: the command can not be buffered; you may try after a while to call the function again.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error Char(char character)
    {
        Error err = (Error)ESpeakSys.espeak_Char(character);
        ThrowPending();
        return err;
    }

    // /***********************/
    // /*  Speech Parameters  */
    // /***********************/

    public enum Parameter
    {
        /// <summary>Speaking speed in words per minute. Values 80 to 450 (see <see cref="ESpeak.Rate"/>).</summary>
        Rate = ESpeakSys.espeak_PARAMETER.espeakRATE,
        /// <summary>
        /// Volume in range 0-200 or more.
        /// 0 = silence, 100 = normal full volume, greater values may produce amplitude compression or distortion.
        /// </summary>
        Volume = ESpeakSys.espeak_PARAMETER.espeakVOLUME,
        /// <summary>Base pitch, range 0-100. 50 = normal.</summary>
        Pitch = ESpeakSys.espeak_PARAMETER.espeakPITCH,
        /// <summary>Pitch range, range 0-100. 0 = monotone, 50 = normal.</summary>
        Range = ESpeakSys.espeak_PARAMETER.espeakRANGE,
        /// <summary>
        /// Which punctuation characters to announce: a <see cref="PunctuationType"/> value.
        /// See <see cref="SetPunctuationList"/> to specify which characters are announced.
        /// </summary>
        Punctuation = ESpeakSys.espeak_PARAMETER.espeakPUNCTUATION,
        /// <summary>
        /// Announce capital letters by:
        /// 0 = none, 1 = sound icon, 2 = spelling,
        /// 3 or higher, by raising pitch. This value gives the amount in Hz by which the pitch
        /// of a word is raised to indicate it has a capital letter.
        /// </summary>
        Capitals = ESpeakSys.espeak_PARAMETER.espeakCAPITALS,
        /// <summary>Pause between words, units of 10mS (at the default speed).</summary>
        WordGap = ESpeakSys.espeak_PARAMETER.espeakWORDGAP,
        Intonation = ESpeakSys.espeak_PARAMETER.espeakINTONATION,
        SsmlBreakMul = ESpeakSys.espeak_PARAMETER.espeakSSML_BREAK_MUL,
    };

    public enum PunctuationType
    {
        None = ESpeakSys.espeak_PUNCT_TYPE.espeakPUNCT_NONE,
        All = ESpeakSys.espeak_PUNCT_TYPE.espeakPUNCT_ALL,
        Some = ESpeakSys.espeak_PUNCT_TYPE.espeakPUNCT_SOME
    };

    /// <summary>
    /// Sets the value of the specified parameter.
    /// </summary>
    /// <param name="parameter">The parameter to set; see <see cref="Parameter"/> for the meaning of each value.</param>
    /// <param name="value">The new value of the parameter.</param>
    /// <param name="relative">
    /// <see langword="false"/> = sets the absolute value of the parameter.
    /// <see langword="true"/> = sets a relative value of the parameter.
    /// </param>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.BufferFull"/>: the command can not be buffered; you may try after a while to call the function again.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error SetParameter(Parameter parameter, int value, bool relative = false)
    {
        return (Error)ESpeakSys.espeak_SetParameter((ESpeakSys.espeak_PARAMETER)parameter, value, relative ? 1 : 0);
    }

    /// <summary>
    /// Gets the value of the specified parameter.
    /// </summary>
    /// <param name="parameter">The parameter to get.</param>
    /// <param name="current">
    /// <see langword="false"/> = returns the default value of the specified parameter.
    /// <see langword="true"/> = returns the current value of the specified parameter, as set by <see cref="SetParameter"/>.
    /// </param>
    public static int GetParameter(Parameter parameter, bool current = true)
    {
        return ESpeakSys.espeak_GetParameter((ESpeakSys.espeak_PARAMETER)parameter, current ? 1 : 0);
    }

    /// <summary>
    /// Specifies a list of punctuation characters whose names are to be spoken when the
    /// value of the <see cref="Parameter.Punctuation"/> parameter is set to <see cref="PunctuationType.Some"/>.
    /// </summary>
    /// <param name="punctlist">The punctuation characters.</param>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.BufferFull"/>: the command can not be buffered; you may try after a while to call the function again.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error SetPunctuationList(string punctlist)
    {
        var list = new List<wchar_t>(punctlist.Length + 1);
#if WINDOWS
        foreach (char c in punctlist) list.Add(c);
#else
        foreach (Rune r in punctlist.EnumerateRunes()) list.Add((wchar_t)r.Value);
#endif
        list.Add(0);
        wchar_t[] buf = list.ToArray();
        unsafe
        {
            fixed (wchar_t* p = buf)
                return (Error)ESpeakSys.espeak_SetPunctuationList(p);
        }
    }

    [Flags]
    public enum PhonemeMode
    {
        /// <summary>No phoneme output (default).</summary>
        None = 0,
        /// <summary>Output the translated phoneme symbols for the text.</summary>
        Show = ESpeakSys.espeakPHONEMES_SHOW,
        /// <summary>Produce IPA phoneme names rather than ascii.</summary>
        IPA = ESpeakSys.espeakPHONEMES_IPA,
        /// <summary>Output a trace of how the translation was done (showing the matching rules and list entries).</summary>
        Trace = ESpeakSys.espeakPHONEMES_TRACE,
        /// <summary>Produce pho data for mbrola.</summary>
        Mbrola = ESpeakSys.espeakPHONEMES_MBROLA,
        /// <summary>Use the separator character as a tie within multi-letter phoneme names, rather than between phoneme names.</summary>
        Tie = ESpeakSys.espeakPHONEMES_TIE,
    };

    private static int PhonemeModeBits(PhonemeMode mode, char separator) => (int)mode | (separator << 8);

    /// <summary>
    /// Controls the output of phoneme symbols for the text.
    /// </summary>
    /// <param name="mode">See <see cref="PhonemeMode"/>.</param>
    /// <param name="separator">Separator character between phoneme names (or the tie, with <see cref="PhonemeMode.Tie"/>), or '\0' for none.</param>
    /// <param name="stream">Output stream (FILE*) for the phoneme symbols (and trace). If <see cref="IntPtr.Zero"/> then it uses stdout.</param>
    public static void SetPhonemeTrace(PhonemeMode mode, char separator = '\0', IntPtr stream = default)
    {
        ESpeakSys.espeak_SetPhonemeTrace(PhonemeModeBits(mode, separator), stream);
    }

    /// <summary>
    /// Translates text into phonemes. Call <see cref="SetVoiceByName"/> first, to select a language.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="mode">
    /// <see cref="PhonemeMode.IPA"/> = International Phonetic Alphabet, otherwise eSpeak's ascii phoneme names.
    /// <see cref="PhonemeMode.Tie"/> = use <paramref name="separator"/> as a tie within multi-letter phonemes names.
    /// </param>
    /// <param name="separator">Separator character between phoneme names, or '\0' for none.</param>
    /// <returns>
    /// The phonemes for each clause of the text: up to the end of a sentence, or comma,
    /// semicolon, colon, or similar punctuation.
    /// </returns>
    public static string[] TextToPhonemes(string text, PhonemeMode mode = PhonemeMode.None, char separator = '\0')
    {
        var result = new List<string>();
        byte[] buf = ToUtf8z(text);
        unsafe
        {
            fixed (byte* p = buf)
            {
                void* ptr = p;
                while (ptr != null)
                {
                    byte* phonemes = ESpeakSys.espeak_TextToPhonemes(&ptr, (int)Chars.UTF8, PhonemeModeBits(mode, separator));
                    result.Add(Marshal.PtrToStringUTF8((IntPtr)phonemes) ?? "");
                }
            }
        }
        return result.ToArray();
    }

    /// <summary>
    /// Version of <see cref="TextToPhonemes"/> that also returns the clause terminator (e.g., CLAUSE_INTONATION_FULL_STOP).
    /// </summary>
    /// <inheritdoc cref="TextToPhonemes"/>
    public static (string Phonemes, int Terminator)[] TextToPhonemesWithTerminator(string text, PhonemeMode mode = PhonemeMode.None, char separator = '\0')
    {
        var result = new List<(string, int)>();
        byte[] buf = ToUtf8z(text);
        unsafe
        {
            fixed (byte* p = buf)
            {
                void* ptr = p;
                while (ptr != null)
                {
                    int terminator = 0;
                    byte* phonemes = ESpeakSys.espeak_TextToPhonemesWithTerminator(&ptr, (int)Chars.UTF8, PhonemeModeBits(mode, separator), &terminator);
                    result.Add((Marshal.PtrToStringUTF8((IntPtr)phonemes) ?? "", terminator));
                }
            }
        }
        return result.ToArray();
    }

    /// <summary>
    /// Compile pronunciation dictionary for a language which corresponds to the currently
    /// selected voice. The required voice should be selected before calling this function.
    /// </summary>
    /// <param name="path">
    /// The directory which contains the language's '_rules' and '_list' files.
    /// 'path' should end with a path separator character ('/').
    /// </param>
    /// <param name="includeSourceLines">
    /// Include source line information for debug purposes (This is displayed with the
    /// -X command line option).
    /// </param>
    /// <param name="log">Stream (FILE*) for error reports and statistics information. If <see cref="IntPtr.Zero"/> then stderr will be used.</param>
    public static void CompileDictionary(string path, bool includeSourceLines = false, IntPtr log = default)
    {
        byte[] buf = ToUtf8z(path);
        unsafe
        {
            fixed (byte* p = buf)
                ESpeakSys.espeak_CompileDictionary(p, log, includeSourceLines ? 1 : 0);
        }
    }


    // /***********************/
    // /*   Voice Selection   */
    // /***********************/


    public enum Gender
    {
        None = 0,
        Male = 1,
        Female = 2
    };

    /// <summary>
    /// A voice, as returned by <see cref="ListVoices"/> and <see cref="GetCurrentVoice"/>.
    /// </summary>
    public sealed class Voice
    {
        /// <summary>A given name for this voice.</summary>
        public string Name { get; init; } = "";
        /// <summary>
        /// The languages (with optional dialect qualifier, eg. "en-uk") for which this voice may be used.
        /// A low priority number indicates a more preferred voice for that language, a higher number
        /// indicates a less preferred voice.
        /// </summary>
        public IReadOnlyList<(int Priority, string Language)> Languages { get; init; } = [];
        /// <summary>The filename for this voice within espeak-ng-data/voices.</summary>
        public string Identifier { get; init; } = "";
        public Gender Gender { get; init; }
        /// <summary>0 = not specified, or age in years.</summary>
        public int Age { get; init; }
        public int Variant { get; init; }
    }

    /// <summary>
    /// Criteria used to select a voice, for <see cref="ListVoices"/> and <see cref="SetVoiceByProperties"/>.
    /// </summary>
    public sealed class VoiceProperties
    {
        /// <summary><see langword="null"/>, or a voice name.</summary>
        public string? Name { get; set; }
        /// <summary><see langword="null"/>, or a single language string (with optional dialect), eg. "en-uk", or "en".</summary>
        public string? Language { get; set; }
        public Gender Gender { get; set; }
        /// <summary>0 = not specified, or an age in years.</summary>
        public byte Age { get; set; }
        /// <summary>
        /// After a list of candidates is produced, scored and sorted, "variant" is used to index
        /// that list and choose a voice.
        /// variant=0 takes the top voice (i.e. best match). variant=1 takes the next voice, etc.
        /// </summary>
        public byte Variant { get; set; }
    }

    /// <summary>
    /// Reads the voice files from espeak-ng-data/voices and returns the available voices.
    /// </summary>
    /// <param name="spec">
    /// If <see langword="null"/> then all voices are listed.
    /// If given, then only the voices which are compatible with <paramref name="spec"/>
    /// are listed, and they are listed in preference order.
    /// </param>
    public static Voice[] ListVoices(VoiceProperties? spec = null)
    {
        byte[]? name = spec?.Name is null ? null : ToUtf8z(spec.Name);
        byte[]? lang = spec?.Language is null ? null : ToUtf8z(spec.Language);
        var voices = new List<Voice>();
        unsafe
        {
            fixed (byte* n = name)
            fixed (byte* l = lang)
            {
                ESpeakSys.espeak_VOICE v = spec is null ? default : new ESpeakSys.espeak_VOICE
                {
                    name = n,
                    languages = l,
                    gender = (byte)spec.Gender,
                    age = spec.Age,
                    variant = spec.Variant
                };
                ESpeakSys.espeak_VOICE** list = ESpeakSys.espeak_ListVoices(spec is null ? null : &v);
                for (int i = 0; list != null && list[i] != null; i++)
                    voices.Add(ToVoice(list[i]));
            }
        }
        return voices.ToArray();
    }

    /// <summary>
    /// Loads a voice given the file path. Language is not considered.
    /// </summary>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.BufferFull"/>: the command can not be buffered; you may try after a while to call the function again.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error SetVoiceByFile(string filename)
    {
        byte[] buf = ToUtf8z(filename);
        unsafe
        {
            fixed (byte* p = buf)
                return (Error)ESpeakSys.espeak_SetVoiceByFile(p);
        }
    }

    /// <summary>
    /// Searches for a voice with a matching "name" field. Language is not considered.
    /// </summary>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.BufferFull"/>: the command can not be buffered; you may try after a while to call the function again.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error SetVoiceByName(string name)
    {
        byte[] buf = ToUtf8z(name);
        unsafe
        {
            fixed (byte* p = buf)
                return (Error)ESpeakSys.espeak_SetVoiceByName(p);
        }
    }

    /// <summary>
    /// Selects a voice using <paramref name="spec"/> as the selection criteria.
    /// </summary>
    /// <param name="spec">See <see cref="VoiceProperties"/>.</param>
    public static Error SetVoiceByProperties(VoiceProperties spec)
    {
        byte[]? name = spec.Name is null ? null : ToUtf8z(spec.Name);
        byte[]? lang = spec.Language is null ? null : ToUtf8z(spec.Language);
        unsafe
        {
            fixed (byte* n = name)
            fixed (byte* l = lang)
            {
                var v = new ESpeakSys.espeak_VOICE
                {
                    name = n,
                    languages = l,
                    gender = (byte)spec.Gender,
                    age = spec.Age,
                    variant = spec.Variant
                };
                return (Error)ESpeakSys.espeak_SetVoiceByProperties(&v);
            }
        }
    }

    /// <summary>
    /// Returns the voice data for the currently selected voice.
    /// This is not affected by temporary voice changes caused by SSML elements such as &lt;voice&gt; and &lt;s&gt;.
    /// </summary>
    public static Voice? GetCurrentVoice()
    {
        unsafe
        {
            ESpeakSys.espeak_VOICE* v = ESpeakSys.espeak_GetCurrentVoice();
            return v == null ? null : ToVoice(v);
        }
    }

    /// <summary>
    /// Stop immediately synthesis and audio output of the current text. When this
    /// function returns, the audio output is fully stopped and the synthesizer is ready to
    /// synthesize a new message.
    /// </summary>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error Cancel()
    {
        Error err = (Error)ESpeakSys.espeak_Cancel();
        ThrowPending();
        return err;
    }

    /// <summary>
    /// Returns whether audio is being played.
    /// </summary>
    public static bool IsPlaying()
    {
        return ESpeakSys.espeak_IsPlaying() != 0;
    }

    /// <summary>
    /// This function returns when all data have been spoken.
    /// </summary>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error Synchronize()
    {
        Error err = (Error)ESpeakSys.espeak_Synchronize();
        ThrowPending();
        return err;
    }

    /// <summary>
    /// Last function to be called.
    /// </summary>
    /// <returns>
    /// <see cref="Error.OK"/>: operation achieved.
    /// <see cref="Error.InternalError"/>.
    /// </returns>
    public static Error Terminate()
    {
        return (Error)ESpeakSys.espeak_Terminate();
    }

    /// <summary>
    /// Returns the version number string.
    /// </summary>
    /// <param name="dataPath">Returns the path to espeak_data.</param>
    public static string Info(out string? dataPath)
    {
        unsafe
        {
            byte* path = null;
            byte* version = ESpeakSys.espeak_Info(&path);
            dataPath = Marshal.PtrToStringUTF8((IntPtr)path);
            return Marshal.PtrToStringUTF8((IntPtr)version) ?? "";
        }
    }


    private static ExceptionDispatchInfo? pendingException = null;

    private static void SetPending(Exception e) =>
        Interlocked.CompareExchange(ref pendingException, ExceptionDispatchInfo.Capture(e), null);

    private static void ThrowPending() =>
        Interlocked.Exchange(ref pendingException, null)?.Throw();

    private static byte[] ToUtf8z(string s)
    {
        byte[] buf = new byte[Encoding.UTF8.GetByteCount(s) + 1];
        Encoding.UTF8.GetBytes(s, buf);
        return buf;
    }

    private static unsafe Voice ToVoice(ESpeakSys.espeak_VOICE* v)
    {
        var languages = new List<(int, string)>();
        byte* p = v->languages;
        if (p != null)
        {
            while (*p != 0)
            {
                int priority = *p++;
                languages.Add((priority, Marshal.PtrToStringUTF8((IntPtr)p) ?? ""));
                while (*p != 0) p++;
                p++;
            }
        }

        return new Voice
        {
            Name = Marshal.PtrToStringUTF8((IntPtr)v->name) ?? "",
            Languages = languages,
            Identifier = Marshal.PtrToStringUTF8((IntPtr)v->identifier) ?? "",
            Gender = (Gender)v->gender,
            Age = v->age,
            Variant = v->variant,
        };
    }
}
