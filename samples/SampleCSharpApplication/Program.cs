using org.daisy.jnet;
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
                // For class that are in a package within a jar, don't forget to use the path to the class : 
                IntPtr SampleApplicationObject = jni.InstantiateJavaObject("org/daisy/jnet/SampleApplication", out SampleApplicationClass);

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }
    }
}
