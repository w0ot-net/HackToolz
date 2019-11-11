using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace UnlockAssembly {
    class Program {
        static void Main(string[] args) {

            string libDir = @"C:\Users\admin\Desktop\shared\pwn2own\Genesis\DataContractSerializerExploitTest\lib2";
            string destDir = @"C:\Users\admin\Desktop\shared\pwn2own\Genesis\DataContractSerializerExploitTest\lib";
            var files = Directory.EnumerateFiles(libDir, "*.dll");

            // target assembly to unlock
            //var files = Directory.EnumerateFiles(libDir, "IcoConfigCommon.dll");

            if (!Directory.Exists(destDir)) {
                Directory.CreateDirectory(destDir);
            }

            foreach (string file in files) {
                Console.WriteLine(string.Format("[+] Analyzing {0}", file));
                using (ModuleDefMD mod = ModuleDefMD.Load(file)) {
                    foreach (var type in mod.GetTypes()) {
                        Console.WriteLine("Type: " + type);
                        if (type.IsNotPublic && !type.Attributes.HasFlag(TypeAttributes.Abstract)) {
                            type.Attributes |= TypeAttributes.Public;
                            foreach (var method in type.Methods) {
                                Console.WriteLine("Method: " + method.Name);
                                //foreach (var test in type.HasMethods()) {
                                    Console.WriteLine(type.HasMethods);
                                //}
                                if (type.FullName.ToLower().Contains("topologyserviceclient")) {
                                    if (method.Attributes.HasFlag(MethodAttributes.Private)) {
                                        method.Attributes ^= MethodAttributes.Private;
                                        method.Attributes |= MethodAttributes.Public;
                                    }
                                    if (method.Attributes.HasFlag(MethodAttributes.PrivateScope)) {
                                        method.Attributes ^= MethodAttributes.PrivateScope;
                                    }
                                }
                                method.Attributes |= MethodAttributes.Public;
                            }
                            foreach (var field in type.Fields) {
                                Console.WriteLine("Field: " + field.Name);
                                field.Attributes |= FieldAttributes.Public;
                            }
                            foreach (var nest in type.NestedTypes) {
                                Console.WriteLine("Nested Type: " + nest.Name);
                                nest.Attributes |= TypeAttributes.Public;
                            }

                        }
                    }

                    string destFile = Path.Combine(destDir, Path.GetFileName(file));
                    mod.Write(destFile);
                    Console.WriteLine(string.Format("[+] Outputting to: {0}", destFile));
                }
            }
        }
    }
}