# JNET - JNI Bindings and minimal bridge for .net

Code originally designed by Simon ANGHOLOR under CPOL licence.
see https://www.codeproject.com/Articles/245622/Using-the-Java-Native-Interface-in-Csharp for the original code base.

This project intent to provide a DLL to allow the use of a very minimal overlay to JNI functions in .net component, thus allowing to interact with a JVM to simply execute java functions and/or object following JNI convention.

It is NOT intended to create proxy classes like jni4net does : 
it is mostly exposing jvm functions to allow the use of JNI interface in c#, like you would use the jni.h header and the jvm.lib and dll files in C++ to run jni calls in C++.

## Updating for newer version of the JNI support

We updated the original code from Simon ANGHOLOR to handle the AdoptOpenJDK 11.
This project is currently targetting the following oracle jni spec : https://docs.oracle.com/en/java/javase/11/docs/specs/jni/

## Using the lib an the tools

Building of the library has been tested under visual studio 2019 on a Windows 10 x64 with AdoptOpenJDK globally installed.

## Useful informations

### Building a minimal java runtime

### Retrieve JNI signature
