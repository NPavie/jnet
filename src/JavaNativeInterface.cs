﻿////////////////////////////////////////////////////////////////////////////////////////////////  
//  An excellent resource for the JNI library is on the website 
// Java Native Interface: Programmer's Guide and Specification by Sheng Liang 
//  http://docs.oracle.com/javase/7/docs/technotes/guides/jni/
// for a list of all the functions 
// http://download.oracle.com/javase/6/docs/technotes/guides/jni/spec/functions.html
////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace org.daisy.jnet {
    public unsafe class JavaNativeInterface : IDisposable {
        private static string __jvmDllPath = "";
        private static string __javaVersion = "";


        // Possible registry keys that indicate the current installed JRE
        private static readonly string[] JAVA_REGISTRY_KEYS = {
            @"HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft\Java Runtime Environment",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft\JRE",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft\Java Development Kit",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft\JDK"
        };

        // Classe pointer found by class names
        private readonly Dictionary<string, IntPtr> usedClasses = new Dictionary<string, IntPtr>();

        // Methods IDs list by class pointer (to be used to limit jni find calls)
        private readonly Dictionary<IntPtr, List<IntPtr>> classMethodIDs = new Dictionary<IntPtr, List<IntPtr>>();

        // Currently instantiated object, for futur disposal
        private readonly List<IntPtr> usedObject = new List<IntPtr>();

        private JavaVM jvm;
        private JNIEnv env;

        public bool AttachToCurrentJVMThread { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="forcedJVMDllPath"></param>
        public JavaNativeInterface(string forcedJVMDllPath = "") {
            if (forcedJVMDllPath.Length > 0) {
                JavaNativeInterface.__jvmDllPath = forcedJVMDllPath;
            } else if (JavaNativeInterface.__jvmDllPath.Length == 0) {
                // Search for a java runtime near the current assembly
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string assemblyDir = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path)) + Path.DirectorySeparatorChar;
                string[] searchResult = Directory.GetFiles(assemblyDir, "jvm.dll", SearchOption.AllDirectories);
                if (searchResult.Length > 0) {
                    JavaNativeInterface.__jvmDllPath = searchResult[0];
                } else {
                    string envJavaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
                    if (envJavaHome.Length > 0) {
                        searchResult = Directory.GetFiles(envJavaHome, "jvm.dll", SearchOption.AllDirectories);
                        if (searchResult.Length > 0) {
                            JavaNativeInterface.__jvmDllPath = searchResult[0];
                        }
                    } else {
                        foreach (string key in JavaNativeInterface.JAVA_REGISTRY_KEYS) {
                            string javaVersion = (string)Registry.GetValue(key, "CurrentVersion", null);
                            if (javaVersion == null) continue;
                            else {
                                JavaNativeInterface.__javaVersion = javaVersion;
                                string javaKey = Path.Combine(key, javaVersion);
                                string javaHomeKey = (string)Registry.GetValue(javaKey, "JavaHome", null);
                                if (javaHomeKey == null) continue;
                                else {
                                    searchResult = Directory.GetFiles(javaHomeKey, "jvm.dll", SearchOption.AllDirectories);
                                    if (searchResult.Length > 0) {
                                        JavaNativeInterface.__jvmDllPath = searchResult[0];
                                        break;
                                    } else continue;

                                }
                            }
                        }
                        if (JavaNativeInterface.__jvmDllPath.Length == 0) { // no runtime found in registry keys 
                            throw new Exception("No Java runtime available to launch a JVM, please install java or contact your IT administrator.");
                        }
                    }
                }
                Console.WriteLine("Using " + JavaNativeInterface.__jvmDllPath);
                JavaVM.loadAssembly(JavaNativeInterface.__jvmDllPath);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="AddToExistingJVM"></param>
        /// <param name="targetVersion">Specified the version</param>
        public void LoadVM(Dictionary<string, string> options, bool AddToExistingJVM = false, JNIVersion targetVersion = JNIVersion.JNI_VERSION_10) {

            // Set the directory to the location of the JVM.dll. 
            // This will ensure that the API call JNI_CreateJavaVM will work
            // TODO : maybe remove this after changing DLLImport by Assembly dynamic loading
            // This probably break pathes to ressources
            // Directory.SetCurrentDirectory(Path.GetDirectoryName(JavaNativeInterface.__jvmDllPath));

            var args = new JavaVMInitArgs();

            args.version = (int)targetVersion;

            args.ignoreUnrecognized = JavaVM.BooleanToByte(true); // True

            if (options.Count > 0) {
                args.nOptions = options.Count;
                var opt = new JavaVMOption[options.Count];
                int i = 0;
                foreach (KeyValuePair<string, string> kvp in options) {
                    string optionString = kvp.Key.ToString() + "=" + kvp.Value.ToString();
                    opt[i++].optionString = Marshal.StringToHGlobalAnsi(optionString);
                }
                fixed (JavaVMOption* a = &opt[0]) {
                    // prevents the garbage collector from relocating the opt variable as this is used in unmanaged code that the gc does not know about
                    args.options = a;
                }
            }

            if (!AttachToCurrentJVMThread) {
                IntPtr environment;
                IntPtr javaVirtualMachine;
                int result = JavaVM.JNI_CreateJavaVM(out javaVirtualMachine, out environment, &args);
                if (result != JNIReturnValue.JNI_OK) {
                    throw new Exception("Cannot create JVM " + result.ToString());
                }

                jvm = new JavaVM(javaVirtualMachine);
                env = new JNIEnv(environment);
            } else AttachToCurrentJVM(args);
        }

        private void AttachToCurrentJVM(JavaVMInitArgs args) {
            // This is only required if you want to reuse the same instance of the JVM
            // This is especially useful if you are using JNI in a webservice. see page 89 of the
            // Java Native Interface: Programmer's Guide and Specification by Sheng Liang
            if (AttachToCurrentJVMThread) {
                int nVMs;

                IntPtr javaVirtualMachine;
                int res = JavaVM.JNI_GetCreatedJavaVMs(out javaVirtualMachine, 1, out nVMs);
                if (res != JNIReturnValue.JNI_OK) {
                    throw new Exception("JNI_GetCreatedJavaVMs failed (" + res.ToString() + ")");
                }
                if (nVMs > 0) {
                    jvm = new JavaVM(javaVirtualMachine);
                    res = jvm.AttachCurrentThread(out env, args);
                    if (res != JNIReturnValue.JNI_OK) {
                        throw new Exception("AttachCurrentThread failed (" + res.ToString() + ")");
                    }
                }
            }
        }

        /// <summary>
        /// Create an instance of a java object
        /// TODO : add the possibility to call an object constructor with arguments
        /// </summary>
        /// <param name="ClassName">name of the class to instantiate</param>
        /// <param name="javaClass">an empty java class pointer that will set if the class is found</param>
        /// 
        /// <returns>A pointer to a java object in memory if the constructor could be called without error</returns>
        public IntPtr InstantiateJavaObject(string ClassName, out IntPtr javaClass, string signature = "()V", params object[] args) {
            // retrieve class pointer to create and object
            if (!usedClasses.ContainsKey(ClassName)) {
                try {
                    javaClass = env.FindClass(ClassName);
                    usedClasses.Add(ClassName, javaClass);
                } catch {
                    throw new Exception(env.CatchJavaException());
                }
            } else {
                javaClass = usedClasses[ClassName];
            }

            try {
                IntPtr methodId = env.GetMethodID(javaClass, "<init>", signature);
                IntPtr javaObject = env.NewObject(javaClass, methodId, ParseParameters(javaClass, signature, args));
                // Store for disposal
                usedObject.Add(javaObject);
                return javaObject;

            } catch {
                throw new Exception(env.CatchJavaException());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="javaClass">pointer of the javaclass of the object</param>
        /// <param name="javaObject">pointer of the object to used as caller</param>
        /// <param name="methodName">method name</param>
        /// <param name="sig">Method JNI signature (see javap -s javaClass.class )</param>
        /// <param name="param">parameters to used with </param>
        /// <exception cref="Exception">throws back any Java exception found during the method call</exception>
        public void CallVoidMethod(IntPtr javaClass, IntPtr javaObject, string methodName, string sig, params object[] param ) {
            try {
                IntPtr methodId = env.GetMethodID(javaClass, methodName, sig);
                env.CallVoidMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
            } catch {
                throw new Exception(env.CatchJavaException());
            }
        }

        private JValue[] ParseParameters(IntPtr javaClass, string sig, params object[] param) {
            JValue[] retval = new JValue[param.Length];

            int startIndex = sig.IndexOf('(') + 1;

            for (int i = 0; i < param.Length; i++) {
                string paramSig = "";
                if (sig.Substring(startIndex, 1) == "[")
                    paramSig = sig.Substring(startIndex++, 1);

                if (sig.Substring(startIndex, 1) == "L") {
                    paramSig = paramSig + sig.Substring(startIndex, sig.IndexOf(';', startIndex) - startIndex);
                    startIndex++; // skip past ;
                } else
                    paramSig = paramSig + sig.Substring(startIndex, 1);

                startIndex = startIndex + (paramSig.Length - (paramSig.IndexOf("[", StringComparison.Ordinal) + 1));

                if (param[i] is string) {
                    if (!paramSig.Equals("Ljava/lang/String")) {
                        throw new Exception("Signature (" + paramSig + ") does not match parameter value (" + param[i].GetType().ToString() + ").");
                    }
                    retval[i] = new JValue() { l = env.NewString(param[i].ToString(), param[i].ToString().Length) };
                } else if (param[i] == null) {
                    retval[i] = new JValue(); // Just leave as default value
                } else if (paramSig.StartsWith("[")) {
                    retval[i] = ProcessArrayType(javaClass, paramSig, param[i]);
                } else {
                    retval[i] = new JValue();
                    FieldInfo paramField = retval[i].GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).AsQueryable().FirstOrDefault(a => a.Name.ToUpper().Equals(paramSig));
                    if ((paramField != null) && ((param[i].GetType() == paramField.FieldType) || ((paramField.FieldType == typeof(bool)) && (param[i] is byte)))) {
                        paramField.SetValueDirect(__makeref(retval[i]), paramField.FieldType == typeof(bool)  // this is an undocumented feature to set struct fields via reflection
                                                      ? JavaVM.BooleanToByte((bool)param[i])
                                                      : param[i]);
                    } else throw new Exception("Signature (" + paramSig + ") does not match parameter value (" + param[i].GetType().ToString() + ").");
                }
            }
            return retval;
        }

        private JValue ProcessArrayType(IntPtr javaClass, string paramSig, object param) {
            IntPtr arrPointer;
            if (paramSig.Equals("[I"))
                arrPointer = env.NewIntArray(((Array)param).Length, javaClass);
            else if (paramSig.Equals("[J"))
                arrPointer = env.NewLongArray(((Array)param).Length, javaClass);
            else if (paramSig.Equals("[C"))
                arrPointer = env.NewCharArray(((Array)param).Length, javaClass);
            else if (paramSig.Equals("[B"))
                arrPointer = env.NewByteArray(((Array)param).Length, javaClass);
            else if (paramSig.Equals("[S"))
                arrPointer = env.NewShortArray(((Array)param).Length, javaClass);
            else if (paramSig.Equals("[D"))
                arrPointer = env.NewDoubleArray(((Array)param).Length, javaClass);
            else if (paramSig.Equals("[F"))
                arrPointer = env.NewFloatArray(((Array)param).Length, javaClass);
            else if (paramSig.Contains("[Ljava/lang/String")) {
                IntPtr jclass = env.FindClass("Ljava/lang/String;");
                try {
                    arrPointer = env.NewObjectArray(((Array)param).Length, jclass, IntPtr.Zero);
                } finally {
                    env.DeleteLocalRef(jclass);
                }

            } else if (paramSig.Contains("[Ljava/lang/"))
                arrPointer = env.NewObjectArray(((Array)param).Length, javaClass, (IntPtr)param);
            else {
                throw new Exception("Signature (" + paramSig + ") does not match parameter value (" +
                                   param.GetType().ToString() + "). All arrays types should be defined as objects because I do not have enough time to defines every possible array type");
            }

            if (paramSig.Contains("[Ljava/lang/")) {
                for (int j = 0; j < ((Array)param).Length; j++) {
                    object obj = ((Array)param).GetValue(j);

                    if (paramSig.Contains("[Ljava/lang/String")) {
                        IntPtr str = env.NewString(obj.ToString(), obj.ToString().Length);
                        env.SetObjectArrayElement(arrPointer, j, str);
                    } else
                        env.SetObjectArrayElement(arrPointer, j, (IntPtr)obj);
                }
            } else
                env.PackPrimitiveArray<int>((int[])param, arrPointer);

            return new JValue() { l = arrPointer };
        }

        /// <summary>
        /// Java object method callers
        /// </summary>
        /// <typeparam name="T">expect return type</typeparam>
        /// <param name="javaClass">Java class pointer</param>
        /// <param name="javaObject">Java Object pointer</param>
        /// <param name="methodName">Name of the method to call</param>
        /// <param name="sig">Method's JNI signature</param>
        /// <param name="param">Paramters of the method call</param>
        /// <returns></returns>
        public T CallMethod<T>(IntPtr javaClass, IntPtr javaObject, string methodName, string sig, params object[] param) {
            IntPtr methodId = env.GetMethodID(javaClass, methodName, sig);
            try {
                if (typeof(T) == typeof(byte)) {
                    // Call the byte method 
                    byte res = env.CallByteMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res;
                } else if (typeof(T) == typeof(bool)) {
                    // Call the boolean method 
                    bool res = env.CallBooleanMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res;
                }
                if (typeof(T) == typeof(char)) {
                    // Call the char method 
                    char res = env.CallCharMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res;
                } else if (typeof(T) == typeof(short)) {
                    // Call the short method 
                    short res = env.CallShortMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res;
                } else if (typeof(T) == typeof(int)) {
                    // Call the int method               
                    int res = env.CallIntMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res;
                } else if (typeof(T) == typeof(long)) {
                    // Call the long method 
                    long res = env.CallLongMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res;
                } else if (typeof(T) == typeof(float)) {
                    // Call the float method 
                    float res = env.CallFloatMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res;
                } else if (typeof(T) == typeof(double)) {
                    // Call the double method 
                    double res = env.CallDoubleMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res; // need to fix this
                } else if (typeof(T) == typeof(string)) {
                    // Call the string method 
                    IntPtr jstr = env.CallObjectMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));

                    string res = env.JStringToString(jstr);
                    env.DeleteLocalRef(jstr);
                    return (T)(object)res;
                } else if (typeof(T) == typeof(byte[])) {
                    // Call the byte method
                    IntPtr jobj = env.CallStaticObjectMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    if (jobj == IntPtr.Zero) {
                        return default(T);
                    }
                    byte[] res = env.JStringToByte(jobj);
                    env.DeleteLocalRef(jobj);
                    return (T)(object)res;
                } else if (typeof(T) == typeof(string[])) {
                    // Call the string array method
                    IntPtr jobj = env.CallObjectMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    if (jobj == IntPtr.Zero) {
                        return default(T);
                    }

                    IntPtr[] objArray = env.GetObjectArray(jobj);
                    string[] res = new string[objArray.Length];

                    for (int i = 0; i < objArray.Length; i++) {
                        res[i] = env.JStringToString(objArray[i]);
                    }

                    env.DeleteLocalRef(jobj);
                    return (T)(object)res;
                } else if (typeof(T) == typeof(int[])) {
                    // Call the int array method
                    IntPtr jobj = env.CallObjectMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    if (jobj == IntPtr.Zero) {
                        return default(T);
                    }
                    int[] res = env.GetIntArray(jobj);
                    env.DeleteLocalRef(jobj);
                    return (T)(object)res;
                } else if (typeof(T) == typeof(IntPtr)) {
                    // Call the object method and deal with whatever comes back in the call code 
                    IntPtr res = env.CallObjectMethod(javaObject, methodId, ParseParameters(javaClass, sig, param));
                    return (T)(object)res;
                }
                return default(T);
            } catch {
                throw new Exception(env.CatchJavaException());
            }
        }

        public string JavaVersion() {
            int majorVersion = env.GetMajorVersion();
            int minorVersion = env.GetMinorVersion();
            return majorVersion.ToString() + "." + minorVersion.ToString();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~JavaNativeInterface() {
            Dispose(false);
        }



        protected virtual void Dispose(bool disposing) {
            // free native resources if there are any.
            foreach (KeyValuePair<string, IntPtr> javaClass in usedClasses) {
                if (javaClass.Value != IntPtr.Zero) {
                    env.DeleteGlobalRef(javaClass.Value);
                    usedClasses[javaClass.Key] = IntPtr.Zero;
                }
            }
            for (int i = 0, end = usedObject.Count; i < end; ++i) {
                IntPtr javaObject = usedObject[i];
                if (javaObject != IntPtr.Zero) {
                    env.DeleteLocalRef(javaObject);
                    usedObject[i] = IntPtr.Zero;
                }
            }

            if (disposing) {
                // free managed resources
                if (jvm != null) {
                    jvm.Dispose();
                    jvm = null;
                }

                if (env != null) {
                    env.Dispose();
                    env = null;
                }
            }
        }
    }
}
