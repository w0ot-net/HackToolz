using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace GetAllReferences {
    class Program {

        static void Main(string[] args) {
            string initialFile = "";
            try {
                initialFile = args[0];
            }
            catch (Exception e) {
                Console.WriteLine(string.Format("usage: {0} <file-path>", Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)));
                return;
            }
            if (!File.Exists(initialFile)) {
                Console.WriteLine(string.Format("[-] Failed to locate {0}", initialFile));
            }
            string libDir = Directory.GetParent(initialFile).ToString() + @"\";
            string file = "";
            int counter = 0;
            List<string> locatableRefs = new List<string>();
            List<string> refs = new List<string> {
                Path.GetFileNameWithoutExtension(initialFile)
            };

            do {

                string assemblyName = refs.ElementAt(counter);

                // if we're dealing with the initial assembly we don't know if it's an exe or dll
                // so we can't assume it's a dll
                if (assemblyName.Equals(Path.GetFileNameWithoutExtension(initialFile))) {
                    file = libDir + @"\" + assemblyName + Path.GetExtension(initialFile);
                }
                else {
                    file = libDir + @"\" + assemblyName + ".dll";
                }

                if (File.Exists(file)) {
                    foreach (var reference in GetRefs(file)) {
                        if (!refs.Contains(reference)) { // don't add refs we already analyzed
                            refs.Add(reference);
                        }
                    }
                }

                counter++;

            } while (counter < refs.Count()); // checking to see if we added new references

            refs.Sort();
            
            foreach (var reference in refs) {
                string path = "";
                if (reference.Equals(Path.GetFileNameWithoutExtension(initialFile))) {
                    path = libDir + @"\" + reference + Path.GetExtension(initialFile);
                }
                else {
                    path = libDir + @"\" + reference + ".dll";
                }
                if (!File.Exists(path)) {
                    Console.WriteLine(string.Format("[-] Failed to locate {0}", path));
                }
                else {
                    locatableRefs.Add(Path.GetFullPath(path));
                }
            }

            NativeMethods.OpenFolderAndSelectFiles(libDir, locatableRefs.ToArray());

        }

        public static List<string> GetRefs(string file) {
            List<string> refs = new List<string>();
            Console.WriteLine(string.Format("[+] Analyzing {0}", file));
            try {
                using (ModuleDefMD mod = ModuleDefMD.Load(file)) {
                    foreach (var reference in mod.GetAssemblyRefs()) {
                        refs.Add(reference.Name);
                    }
                }
            }
            catch (IOException e) {
                Console.WriteLine(string.Format("[-] Failed to analyze {0}, file not found", file));
            }
            catch (BadImageFormatException e) {
                Console.WriteLine(string.Format("[-] Failed to analyze {0}, bad image format", file));
            }
            
            return refs;
        }
    }


    //https://stackoverflow.com/questions/9355/programmatically-select-multiple-files-in-windows-explorer
    static class NativeMethods {
        [DllImport("shell32.dll", ExactSpelling = true)]
        public static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder,
            uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
            uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ILCreateFromPath([MarshalAs(UnmanagedType.LPTStr)] string pszPath);

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        public interface IShellLinkW {
            [PreserveSig]
            int GetPath(StringBuilder pszFile, int cch, [In, Out] ref WIN32_FIND_DATAW pfd, uint fFlags);

            [PreserveSig]
            int GetIDList([Out] out IntPtr ppidl);

            [PreserveSig]
            int SetIDList([In] ref IntPtr pidl);

            [PreserveSig]
            int GetDescription(StringBuilder pszName, int cch);

            [PreserveSig]
            int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            [PreserveSig]
            int GetWorkingDirectory(StringBuilder pszDir, int cch);

            [PreserveSig]
            int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            [PreserveSig]
            int GetArguments(StringBuilder pszArgs, int cch);

            [PreserveSig]
            int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            [PreserveSig]
            int GetHotkey([Out] out ushort pwHotkey);

            [PreserveSig]
            int SetHotkey(ushort wHotkey);

            [PreserveSig]
            int GetShowCmd([Out] out int piShowCmd);

            [PreserveSig]
            int SetShowCmd(int iShowCmd);

            [PreserveSig]
            int GetIconLocation(StringBuilder pszIconPath, int cch, [Out] out int piIcon);

            [PreserveSig]
            int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

            [PreserveSig]
            int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

            [PreserveSig]
            int Resolve(IntPtr hwnd, uint fFlags);

            [PreserveSig]
            int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode), BestFitMapping(false)]
        public struct WIN32_FIND_DATAW {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        public static void OpenFolderAndSelectFiles(string folder, params string[] filesToSelect) {
            IntPtr dir = ILCreateFromPath(folder);

            var filesToSelectIntPtrs = new IntPtr[filesToSelect.Length];
            for (int i = 0; i < filesToSelect.Length; i++) {
                filesToSelectIntPtrs[i] = ILCreateFromPath(filesToSelect[i]);
            }

            SHOpenFolderAndSelectItems(dir, (uint)filesToSelect.Length, filesToSelectIntPtrs, 0);
            ReleaseComObject(dir);
            ReleaseComObject(filesToSelectIntPtrs);
        }

        private static void ReleaseComObject(params object[] comObjs) {
            foreach (object obj in comObjs) {
                if (obj != null && Marshal.IsComObject(obj))
                    Marshal.ReleaseComObject(obj);
            }
        }
    }
}
