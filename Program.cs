using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ObjectLifeCycle
{
    class Program
    {
        //You can manage unmanaged resources by creating finalizers
        //This mechanism allows a type to clean up prior to garbage collection
        public class Finalizers {
            //Finalizer
            ~Finalizers() {
                Console.WriteLine("Finalizer method executed");
            } 

            //Working with unmanaged resources
            public void DeleteFile()
            {
                try
                {
                    //When working with File, you're going to need to free unmanage resources before you can delete file
                    //
                    // StreamWriter stream = File.CreateText("temp.dat");
                    // stream.Write("some data");
                    // Console.WriteLine("File created");
                    
                    //This will throw IOException because file is still open
                    
                    // File.Delete("temp.dat");
                    // Console.WriteLine("File deleted");

                    //To explicitly free resources: use IDisposable interface.
                    StreamWriter stream = File.CreateText("temp1.dat");
                    stream.Write("some data");
                    Console.WriteLine("File created...");
                    //Forces finalizers to be called(Can only be called when GC occurs)
                    GC.Collect();
                    //Frees unmanaged resources immediately
                    stream.Dispose();
                    File.Delete("temp1.dat");
                    Console.WriteLine("File deleted...");
                }
                catch(IOException e){
                    Console.WriteLine("IOException ws thrown when trying to delete file, fixing this problem....");
                    Console.WriteLine(e.Message);
                }
            }          
        }

        //Implementing Finalizers and IDisposable
        //The finalizer only calls Dispose passing false for disposing.
        //The extra Dispose method with the Boolean argument does the real work. 
        //This method checks if it’s being called in an explicit Dispose or if it’s being called from the finalizer:
           ////If the finalizer calls Dispose, you only release the unmanaged buffer. The Stream object also implements a finalizer and the garbage collector will take care of calling the finalizer of the FileStream instance. Because the order in which the garbage collector calls the finalizers is unpredictable you can’t call any methods on the FileStream.
           ////If Dispose is called explicitly, you also close the underlying FIleStream. It’s important to be defensive in coding this method and always check for any source of possible exceptions. It could be that Dispose is called multiple times and that shouldn’t cause any errors.
        //The regular Dispose method calls GC.SuppressFinalize(this) to make sure that the object is removed from the finalization list that the garbage collector is keeping track of. The instance has already cleaned up after itself, so it’s not necessary that the garbage collector call the finalizer.
        class UnmanagedWrapper : IDisposable
        {
            private IntPtr unmanagedBuffer;
            public FileStream Stream { get; private set; }
            
            public UnmanagedWrapper()
            {
                CreateBuffer();
                this.Stream = File.Open("temp.dat", FileMode.Create);
            }
            
            private void CreateBuffer()
            {
                byte[] data = new byte[1024];
                new Random().NextBytes(data);
                unmanagedBuffer = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, unmanagedBuffer, data.Length);
            }
            
            ~UnmanagedWrapper()
            {
                Dispose(false);
            }

            public void Close()
            {
                Dispose();
            }

            public void Dispose()
            {
                Dispose(true);
                System.GC.SuppressFinalize(this);
                Console.WriteLine("Stream Disposed...");
            }

            protected virtual void Dispose(bool disposing)
            {
                Marshal.FreeHGlobal(unmanagedBuffer);
                if (disposing)
                {
                    if (Stream != null)
                    {
                        Stream.Close();
                    }
                }
            }
        }

        //Weak References
        static WeakReference data;
        public static void WeakReferenceRun()
        {
            object result = GetData();
            Console.WriteLine("Weak references:");

            // GC.Collect(); Uncommenting this line will make data.Target null
            result = GetData();

            //Display populated list
            foreach(var num in (List<int>)result){
                Console.Write("{0}, ", num);
            }
        }

        //Checks if weakreference still contains data
        private static object GetData()
        {
            //if not
            //Data will be loaded and saved in the weakreference again
            if (data == null)
            {
                data = new WeakReference(LoadLargeList());
            }

            if (data.Target == null)
            {
                data.Target = LoadLargeList();
            }
            return data.Target;
        }

        //Populate and return large list of numbers
        public static List<int> LoadLargeList(){
            List<int> numbers = new List<int>();
            for (int i = 1; i < 100; i++)
            {
                numbers.Add(i);
            }
            return numbers;
        }


        //Common Language Runtime(CLR) stores items in 2 locations: Stack or Heap
        //Stack: Keeps track of what's executing in code
        //Heap: Keeps track of Objects
        //Stack is automatically cleared at end of method, CLR takes care of it, but
        //The heap is managed by the garbage collector
        //Garbage collctor works with a mark and compact algorithm
        //Mark: Phase of collection checks which items on heap are referenced by items, if collector finds "living" item on heap, it is marked.
        //Compact: Phase starts after Mark phase, when gc moves all living items together and frees memory for all other objects.
        //GC is clever, it automatically cleans items up when there isn't anymore space on heap.
        //When GC Starts, collects only Generation 0(Items that doesn't survive clean-up, the living items are promoted to new generation).
        static void Main(string[] args)
        {
            Console.WriteLine("Simple finalizer: Line 13");

            //Stream.Dispose, GC.Collect
            var f = new Finalizers();
            f.DeleteFile();

            //Implementing Finalizers and IDisposable
            UnmanagedWrapper uw = new UnmanagedWrapper();
            uw.Dispose();

            //Weak References
            WeakReferenceRun();
        }
    }
}
