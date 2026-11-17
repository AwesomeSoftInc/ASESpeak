using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace ASESpeak;

#pragma warning disable CA1401
#pragma warning disable CA1707
#pragma warning disable CA1720
#pragma warning disable IDE1006

#if WINDOWS
using wchar_t = System.UInt16;
#else
using wchar_t = System.UInt32;
#endif

public static class ESpeakSys
{
    private const string Lib = "espeak-ng";

    /// <summary>
    /// The directory the native espeak-ng library (and, from this project's build, espeak-ng-data) is loaded from.
    /// Defaults to the directory ASESpeak.dll was loaded from. Set it before the first eSpeak call to override.
    /// </summary>
    public static string LibraryDirectory { get; set; } = DetectLibraryDirectory();

    static ESpeakSys()
    {
        NativeLibrary.SetDllImportResolver(typeof(ESpeakSys).Assembly, ResolveLibrary);
    }

    private static IntPtr ResolveLibrary(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (name != Lib)
            return IntPtr.Zero;
        string file = OperatingSystem.IsWindows() ? "espeak-ng.dll"
                    : OperatingSystem.IsMacOS() ? "libespeak-ng.dylib"
                    : "libespeak-ng.so";
        return NativeLibrary.TryLoad(Path.Combine(LibraryDirectory, file), out IntPtr handle)
            ? handle
            : IntPtr.Zero;
    }

    private static string DetectLibraryDirectory()
    {
        Assembly asm = typeof(ESpeakSys).Assembly;
        if (!string.IsNullOrEmpty(asm.Location))
            return Path.GetDirectoryName(asm.Location)!;

        AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(asm);
        if (alc?.GetType().GetProperty("AssemblyLoadedPath")?.GetValue(alc) is string loadedPath && loadedPath.Length > 0)
            return Path.GetDirectoryName(loadedPath)!;

        return AppContext.BaseDirectory;
    }

    // values for 'value' in espeak_SetParameter(espeakRATE, value, 0), nominally in words-per-minute
    public const int espeakRATE_MINIMUM = 80;

    public const int espeakRATE_MAXIMUM = 450;

    public const int espeakRATE_NORMAL = 175;

    public enum espeak_EVENT_TYPE
    {
        espeakEVENT_LIST_TERMINATED = 0, // Retrieval mode: terminates the event list.
        espeakEVENT_WORD = 1,            // Start of word
        espeakEVENT_SENTENCE = 2,        // Start of sentence
        espeakEVENT_MARK = 3,            // Mark
        espeakEVENT_PLAY = 4,            // Audio element
        espeakEVENT_END = 5,             // End of sentence or clause
        espeakEVENT_MSG_TERMINATED = 6,  // End of message
        espeakEVENT_PHONEME = 7,         // Phoneme, if enabled in espeak_Initialize()
        espeakEVENT_SAMPLERATE = 8       // Set sample rate
    };

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct espeak_EVENT_ID
    {
        // used for WORD and SENTENCE events.
        [FieldOffset(0)] public int number;

        // used for MARK and PLAY events.  UTF8 string
        [FieldOffset(0)] public IntPtr name;

        // used for phoneme names (UTF8). Terminated by a zero byte unless the name needs the full 8 bytes.
        [FieldOffset(0)] public fixed byte @string[8];

    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct espeak_EVENT
    {
        public espeak_EVENT_TYPE type;

        // message identifier (or 0 for key or character)
        public uint unique_identifier;

        // the number of characters from the start of the text
        public int text_position;

        // word length, in characters (for espeakEVENT_WORD)
        public int length;

        // the time in mS within the generated speech output data
        public int audio_position;

        // sample id (internal use)
        public int sample;

        public IntPtr user_data;      // pointer supplied by the calling program
        public espeak_EVENT_ID id;

    };

    public enum espeak_POSITION_TYPE
    {
        POS_CHARACTER = 1,
        POS_WORD,
        POS_SENTENCE
    };

    public enum espeak_AUDIO_OUTPUT
    {
        AUDIO_OUTPUT_PLAYBACK,
        AUDIO_OUTPUT_RETRIEVAL,
        AUDIO_OUTPUT_SYNCHRONOUS,
        AUDIO_OUTPUT_SYNCH_PLAYBACK
    };

    public enum espeak_ERROR
    {
        EE_OK = 0,
        EE_INTERNAL_ERROR = -1,
        EE_BUFFER_FULL = 1,
        EE_NOT_FOUND = 2
    };

    public const int espeakINITIALIZE_PHONEME_EVENTS = 0x0001;

    public const int espeakINITIALIZE_PHONEME_IPA = 0x0002;

    public const int espeakINITIALIZE_DONT_EXIT = 0x8000;

    [DllImport(Lib)] public static extern unsafe int espeak_Initialize(espeak_AUDIO_OUTPUT output, int buflength, byte* path, int options);

    [DllImport(Lib)] public static extern unsafe void espeak_SetSynthCallback(delegate* unmanaged[Cdecl]<short*, int, espeak_EVENT*, int> SynthCallback);

    [DllImport(Lib)] public static extern unsafe void espeak_SetUriCallback(delegate* unmanaged[Cdecl]<int, byte*, byte*, int> UriCallback);

    [DllImport(Lib)] public static extern unsafe void espeak_SetPhonemeCallback(delegate* unmanaged[Cdecl]<byte*, int> PhonemeCallback);

    public const int espeakCHARS_AUTO = 0;

    public const int espeakCHARS_UTF8 = 1;

    public const int espeakCHARS_8BIT = 2;

    public const int espeakCHARS_WCHAR = 3;

    public const int espeakCHARS_16BIT = 4;

    public const int espeakSSML = 0x10;

    public const int espeakPHONEMES = 0x100;

    public const int espeakENDPAUSE = 0x1000;

    public const int espeakKEEP_NAMEDATA = 0x2000;

    [DllImport(Lib)]
    public static extern unsafe espeak_ERROR espeak_Synth(void* text, nuint size, uint position, espeak_POSITION_TYPE position_type, uint end_position, uint flags, uint* unique_identifier, void* user_data);

    [DllImport(Lib)]
    public static extern unsafe espeak_ERROR espeak_Synth_Mark(void* text, nuint size, byte* index_mark, uint end_position, uint flags, uint* unique_identifier, void* user_data);

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_Key(byte* key_name);

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_Char(wchar_t character);

    public enum espeak_PARAMETER
    {
        espeakSILENCE = 0,
        espeakRATE = 1,
        espeakVOLUME = 2,
        espeakPITCH = 3,
        espeakRANGE = 4,
        espeakPUNCTUATION = 5,
        espeakCAPITALS = 6,
        espeakWORDGAP = 7,
        espeakOPTIONS = 8,   // reserved for misc. options.  not yet used
        espeakINTONATION = 9,
        espeakSSML_BREAK_MUL = 10,
        espeakRESERVED2 = 11,
        espeakEMPHASIS,
        espeakLINELENGTH,
        espeakVOICETYPE,  // internal, 1=mbrola
        N_SPEECH_PARAM
    };

    public enum espeak_PUNCT_TYPE
    {
        espeakPUNCT_NONE = 0,
        espeakPUNCT_ALL = 1,
        espeakPUNCT_SOME = 2
    };

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_SetParameter(espeak_PARAMETER parameter, int value, int relative);

    [DllImport(Lib)] public static extern unsafe int espeak_GetParameter(espeak_PARAMETER parameter, int current);

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_SetPunctuationList(wchar_t* punctlist);

    public const int espeakPHONEMES_SHOW = 0x01;

    public const int espeakPHONEMES_IPA = 0x02;

    public const int espeakPHONEMES_TRACE = 0x08;

    public const int espeakPHONEMES_MBROLA = 0x10;

    public const int espeakPHONEMES_TIE = 0x80;

    [DllImport(Lib)] public static extern unsafe void espeak_SetPhonemeTrace(int phonememode, IntPtr stream);

    [DllImport(Lib)] public static extern unsafe byte* espeak_TextToPhonemes(void** textptr, int textmode, int phonememode);

    [DllImport(Lib)] public static extern unsafe byte* espeak_TextToPhonemesWithTerminator(void** textptr, int textmode, int phonememode, int* terminator);

    [DllImport(Lib)] public static extern unsafe void espeak_CompileDictionary(byte* path, IntPtr log, int flags);

    // voice table
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct espeak_VOICE
    {
        public byte* name;      // a given name for this voice. UTF8 string.
        public byte* languages;       // list of pairs of (byte) priority + (string) language (and dialect qualifier)
        public byte* identifier;      // the filename for this voice within espeak-ng-data/voices
        public byte gender;  // 0=none 1=male, 2=female,
        public byte age;     // 0=not specified, or age in years
        public byte variant; // only used when passed as a parameter to espeak_SetVoiceByProperties
        public byte xx1;     // for internal use
        public int score;       // for internal use
        public void* spare;     // for internal use
    };

    [DllImport(Lib)] public static extern unsafe espeak_VOICE** espeak_ListVoices(espeak_VOICE* voice_spec);

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_SetVoiceByFile(byte* filename);

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_SetVoiceByName(byte* name);

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_SetVoiceByProperties(espeak_VOICE* voice_spec);

    [DllImport(Lib)] public static extern unsafe espeak_VOICE* espeak_GetCurrentVoice();

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_Cancel();

    [DllImport(Lib)] public static extern unsafe int espeak_IsPlaying();

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_Synchronize();

    [DllImport(Lib)] public static extern unsafe espeak_ERROR espeak_Terminate();

    [DllImport(Lib)] public static extern unsafe byte* espeak_Info(byte** path_data);

}
