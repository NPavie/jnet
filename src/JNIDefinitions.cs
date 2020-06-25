using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace org.daisy.jnet {

    /// <summary>
    /// JNI Version to pass to the interface.
    /// Possible values :<br/>
    /// JNI_VERSION_1_1, (possibly not supported)<br/>
    /// JNI_VERSION_1_2,<br/>
    /// JNI_VERSION_1_4,<br/>
    /// JNI_VERSION_1_6,<br/>
    /// JNI_VERSION_1_8,<br/>
    /// JNI_VERSION_9,  <br/>
    /// JNI_VERSION_10 (dafault targeted version) <br/>
    /// </summary>
    public enum JNIVersion : int {
        JNI_VERSION_1_1 = 0x00010001,
        JNI_VERSION_1_2 = 0x00010002,
        JNI_VERSION_1_4 = 0x00010004,
        JNI_VERSION_1_6 = 0x00010006,
        JNI_VERSION_1_8 = 0x00010008,
        JNI_VERSION_9 = 0x00090000,
        JNI_VERSION_10 = 0x000a0000
}

    public struct JNIBooleanValue
    {
        // JBoolean Constant
        public const byte JNI_TRUE = 1;   // True (wouldn't it be fun to set JNI_TRUE = 0 that would give someone hrs of fun of debugging)
        public const byte JNI_FALSE = 0;  // False
    }

    public struct JNIReturnValue
    {
        // possible return values for JNI functions
        public const int JNI_OK = 0;  // success
        public const int JNI_ERR = -1; // unknown error
        public const int JNI_EDETACHED = -2; // thread detached from the VM
        public const int JNI_EVERSION = -3; // JNI version error
        public const int JNI_ENOMEM = -4; // not enough memory 
        public const int JNI_EEXIST = -5; // VM already created 
        public const int JNI_EINVAL = -6; // invalid arguments 

        public const int JNI_ENOJava = 101; // local error if the DLL can not be found

        // used in ReleaseScalarArrayElement
        public const int JNI_COMMIT = 1;
        public const int JNI_ABORT = 2;

    }

    // Invocation API
    [StructLayout(LayoutKind.Sequential), NativeCppClass]
    public struct JavaVMOption
    {
        public IntPtr optionString;
        public IntPtr extraInfo;
    }

    [StructLayout(LayoutKind.Sequential), NativeCppClass]
    public unsafe struct JavaVMInitArgs
    {
        public int version;
        public int nOptions;
        public JavaVMOption* options;
        public byte ignoreUnrecognized;
    }

    public unsafe struct JavaVMAttachArgs {
        public int version;
        public IntPtr name; // char*
        public IntPtr group; // jobject
    }

    // You can't have reference types (jObject etc). So I have dropped the jObject type and replaced it with a IntPtr
    [StructLayout(LayoutKind.Explicit)]
    public struct JValue
    {
        [FieldOffset(0)]
        public byte z;
        [FieldOffset(0)]
        public byte b;
        [FieldOffset(0)]
        public char c;
        [FieldOffset(0)]
        public short s;
        [FieldOffset(0)]
        public int i;
        [FieldOffset(0)]
        public long j;
        [FieldOffset(0)]
        public float f;
        [FieldOffset(0)]
        public double d;
        [FieldOffset(0)]
        public IntPtr l;
    }

    // JDK 1.6
    public enum jobjectRefType {
        JNIInvalidRefType = 0,
        JNILocalRefType = 1,
        JNIGlobalRefType = 2,
        JNIWeakGlobalRefType = 3
    }
}



