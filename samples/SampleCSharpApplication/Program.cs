﻿using org.daisy.jnet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SampleApplication {
    class Program {
        static void Main(string[] args) {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string workingDir = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path)) + Path.DirectorySeparatorChar;

            // Instantiate the JNI interface assembly
            JavaNativeInterface jni = new JavaNativeInterface();
            Dictionary<string, string> options = new Dictionary<string, string>();
            
            // Setting the class path to the jar that containes the classes to use
            options.Add("-Djava.class.path",
                workingDir + "target\\SampleJavaApplication-0.0.1-SNAPSHOT.jar");
            // If your jar need other jars as dependencies, you may need to add them in the classpath :
            // + ";" + workingDir + "target\\dependency.jar"); 

            // Load a new JVM
            jni.LoadVM(options, false);
            try {
                IntPtr SampleApplicationClass = IntPtr.Zero;
                IntPtr SampleApplicationObject = jni.InstantiateJavaObject("org/daisy/jnet/SampleApplication", out SampleApplicationClass);
                string testString = jni.CallMethod<string>(SampleApplicationClass, SampleApplicationObject, "getTestString", "()Ljava/lang/String;");
                Console.WriteLine(testString);

                SampleApplicationObject = jni.InstantiateJavaObject("org/daisy/jnet/SampleApplication", out SampleApplicationClass, "(Ljava/lang/String;)V","I'm a string sent from C#");
                testString = jni.CallMethod<string>(SampleApplicationClass, SampleApplicationObject, "getTestString", "()Ljava/lang/String;");
                Console.WriteLine(testString);
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }
    }
}
