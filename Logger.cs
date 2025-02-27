using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ForestForTheFlames
{
    internal class Logger
    {
        internal enum LEVEL
        {
            DEBUG,
            INFO,
            WARNING,
            ERROR,
            FATAL
        }

        public static readonly Logger Instance = new Logger();

        public static void Log(LEVEL level, string caller, object msg)
        {
            Console.WriteLine($"[{level}] {caller}: {msg}");
        }

        public static void Log(object msg)
        {
            Log(LEVEL.INFO, new StackTrace().GetFrame(1).GetMethod().DeclaringType.FullName + "::" +  new StackTrace().GetFrame(1).GetMethod().Name, msg);
        }

        public static void Look(object obj)
        {
            if (obj != null)
            {
                var fn = "";
                foreach (var f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    fn += $"[{f.FieldType.FullName}] {f.Name}: {f.GetValue(obj)}\n";
                }
                Log(LEVEL.DEBUG, new StackTrace().GetFrame(1).GetMethod().DeclaringType.FullName + "::" + new StackTrace().GetFrame(1).GetMethod().Name, fn);
            } else
            {
                Log("null @ Look");
            }
        }

        public static void Look(Type obj)
        {
            if (obj != null)
            {
                var fn = $"Field Dump of {obj.FullName}\n";
                foreach (var f in obj.GetRuntimeFields())
                {
                    if (f.IsStatic)
                    {
                        //Log($"get_{f.Name}");
                        // NativeFieldInfoPtr_
                        var m = obj.GetMethod($"get_{f.Name.Substring(19)}");
                        if (m != null)
                        {
                            try
                            {
                                fn += $"[{f.FieldType.FullName}] {f.Name}: {m.Invoke(obj, null)}\n";
                            }
                            catch
                            {
                                try
                                {

                                    // NativeMethodInfoPtr
                                    fn += $"[{f.FieldType.FullName}] {f.Name}: {m.Invoke(null, null)}\n";
                                }
                                catch { }
                            }
                        }

                        //fn += $"[{f.FieldType.FullName}] {f.Name}: {f.GetValue(obj)}\n";
                    }
                }
                Log(LEVEL.DEBUG, new StackTrace().GetFrame(1).GetMethod().DeclaringType.FullName + "::" + new StackTrace().GetFrame(1).GetMethod().Name, fn);
            }
            else
            {
                Log("null @ Look [Type]");
            }
        }
    }
}
