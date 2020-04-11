﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LiveSplit.OriWotW.Il2Cpp;
namespace LiveSplit.OriWotW {
    public enum PointerVersion {
        All,
        V2
    }
    public enum AutoDeref {
        None,
        Single,
        Double
    }
    public class ProgramPointer {
        private int lastID;
        private DateTime lastTry;
        private IFindPointer currentFinder;
        public IntPtr Pointer { get; private set; }
        public IFindPointer[] Finders { get; private set; }
        public string AsmName { get; private set; }

        public ProgramPointer(params IFindPointer[] finders) : this(string.Empty, finders) { }
        public ProgramPointer(string asmName, params IFindPointer[] finders) {
            AsmName = asmName;
            Finders = finders;
            lastID = -1;
            lastTry = DateTime.MinValue;
        }

        public T Read<T>(Process program, params int[] offsets) where T : unmanaged {
            GetPointer(program);
            return program.Read<T>(Pointer, offsets);
        }
        public string Read(Process program, params int[] offsets) {
            GetPointer(program);
            return program.ReadString(Pointer, offsets);
        }
        public byte[] ReadBytes(Process program, int length, params int[] offsets) {
            GetPointer(program);
            return program.Read(Pointer, length, offsets);
        }
        public void Write<T>(Process program, T value, params int[] offsets) where T : unmanaged {
            GetPointer(program);
            program.Write<T>(Pointer, value, offsets);
        }
        public void Write(Process program, byte[] value, params int[] offsets) {
            GetPointer(program);
            program.Write(Pointer, value, offsets);
        }
        public IntPtr GetPointer(Process program) {
            if (program == null) {
                Pointer = IntPtr.Zero;
                lastID = -1;
                return Pointer;
            } else if (program.Id != lastID) {
                Pointer = IntPtr.Zero;
                lastID = program.Id;
            } else if (Pointer != IntPtr.Zero && !currentFinder.VerifyPointer(program)) {
                Pointer = IntPtr.Zero;
            }

            if (Pointer == IntPtr.Zero && DateTime.Now > lastTry) {
                lastTry = DateTime.Now.AddSeconds(1);

                for (int i = 0; i < Finders.Length; i++) {
                    IFindPointer finder = Finders[i];
                    if (finder.Version == PointerVersion.All || finder.Version == MemoryManager.Version) {
                        try {
                            Pointer = finder.FindPointer(program, AsmName);
                            if (Pointer != IntPtr.Zero || finder.FoundBaseAddress()) {
                                currentFinder = finder;
                                break;
                            }
                        } catch { }
                    }
                }
            }
            return Pointer;
        }
        public static IntPtr DerefPointer(Process program, IntPtr pointer, AutoDeref autoDeref) {
            if (pointer != IntPtr.Zero) {
                if (autoDeref != AutoDeref.None) {
                    pointer = program.Read<IntPtr>(pointer);
                    if (autoDeref == AutoDeref.Double) {
                        pointer = program.Read<IntPtr>(pointer);
                    }
                }
            }
            return pointer;
        }
        public static Tuple<IntPtr, IntPtr> GetAddressRange(Process program, string asmName) {
            Module64 module = program.Module64(asmName);
            if (module != null) {
                return new Tuple<IntPtr, IntPtr>(module.BaseAddress, module.BaseAddress + module.MemorySize);
            }
            return new Tuple<IntPtr, IntPtr>(IntPtr.Zero, IntPtr.Zero);
        }
    }
    public interface IFindPointer {
        IntPtr FindPointer(Process program, string asmName);
        bool FoundBaseAddress();
        bool VerifyPointer(Process program);
        PointerVersion Version { get; }
    }
    public class FindIl2CppOffset {
        private static int lastPID;
        private static Dictionary<string, int> offsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static int GetOffset(Process program, string fullname) {
            if (lastPID != program.Id) {
                lastPID = program.Id;
                offsets.Clear();
            }
            if (FindIl2Cpp.Decompiler == null) { return 0; }

            int offset;
            if (offsets.TryGetValue(fullname, out offset)) {
                return offset;
            }
            offset = FindIl2Cpp.Decompiler.GetFieldOffset(fullname);
            offsets.Add(fullname, offset);
            return offset;
        }
    }
    //Will only work for version 24.1 PE files. Structures need changed per version in the Il2Cpp files
    public class FindIl2Cpp : IFindPointer {
        public static Il2CppDecompiler Decompiler;
        public PointerVersion Version { get; private set; }
        private AutoDeref AutoDeref;
        private string FullName;
        private int Offset;
        private IntPtr BasePtr;
        public FindIl2Cpp(PointerVersion version, AutoDeref autoDeref, string fullName, int offset) {
            Version = version;
            AutoDeref = autoDeref;
            FullName = fullName;
            Offset = offset;
            BasePtr = IntPtr.Zero;
        }
        public bool FoundBaseAddress() {
            return BasePtr != IntPtr.Zero;
        }
        public static bool InitializeIl2Cpp(Process program) {
            string programPath = Path.GetDirectoryName(program.MainModule.FileName);
            string metaFile = Path.Combine(programPath, @"oriwotw_Data\il2cpp_data\Metadata\global-metadata.dat");
            string ilFile = Path.Combine(programPath, @"GameAssembly.dll");
            if (!File.Exists(metaFile) || !File.Exists(ilFile)) { return false; }

            byte[] metaDataBytes = File.ReadAllBytes(metaFile);
            byte[] il2CppBytes = File.ReadAllBytes(ilFile);
            Metadata metaData;
            Il2CppData il2Cpp;
            Il2CppReader.Init(il2CppBytes, metaDataBytes, out metaData, out il2Cpp);
            Il2CppExecutor executor = new Il2CppExecutor(metaData, il2Cpp);
            Decompiler = new Il2CppDecompiler(executor);
            return true;
        }
        public bool VerifyPointer(Process program) {
            return true;
        }
        public IntPtr FindPointer(Process program, string asmName) {
            if (Decompiler == null) { return IntPtr.Zero; }
            return ProgramPointer.DerefPointer(program, GetPointer(program, asmName), AutoDeref);
        }
        private IntPtr GetPointer(Process program, string asmName) {
            if (BasePtr == IntPtr.Zero) {
                ulong rva = Decompiler.GetRVA(FullName);
                if (string.IsNullOrEmpty(asmName)) {
                    BasePtr = program.MainModule.BaseAddress + (int)rva + Offset;
                } else {
                    Tuple<IntPtr, IntPtr> range = ProgramPointer.GetAddressRange(program, asmName);
                    BasePtr = range.Item1 + (int)rva + Offset;
                }
            }

            int offset = 0;
            if (AutoDeref != AutoDeref.None) {
                offset = program.Read<int>(BasePtr) + 4;
            }
            return BasePtr + offset;
        }
    }
    public class FindPointerSignature : IFindPointer {
        public PointerVersion Version { get; private set; }
        private AutoDeref AutoDeref;
        private string Signature;
        private IntPtr BasePtr;
        private MemorySearcher Searcher;
        private DateTime LastVerified;
        private int[] Relative;

        public FindPointerSignature(PointerVersion version, AutoDeref autoDeref, string signature, params int[] relative) {
            Version = version;
            AutoDeref = autoDeref;
            Signature = signature;
            BasePtr = IntPtr.Zero;
            Searcher = new MemorySearcher();
            LastVerified = DateTime.MaxValue;
            Relative = relative;
        }

        public bool FoundBaseAddress() {
            return BasePtr != IntPtr.Zero;
        }
        public bool VerifyPointer(Process program) {
            DateTime now = DateTime.Now;
            if (now > LastVerified) {
                bool isValid = Searcher.VerifySignature(program, BasePtr, Signature);
                LastVerified = now.AddSeconds(5);
                if (!isValid) {
                    BasePtr = IntPtr.Zero;
                    return false;
                }
            }
            return true;
        }
        public IntPtr FindPointer(Process program, string asmName) {
            return ProgramPointer.DerefPointer(program, GetPointer(program, asmName), AutoDeref);
        }
        private IntPtr GetPointer(Process program, string asmName) {
            if (string.IsNullOrEmpty(asmName)) {
                Searcher.MemoryFilter = delegate (MemInfo info) {
                    return (info.State & 0x1000) != 0 && (info.Protect & 0x40) != 0 && (info.Protect & 0x100) == 0;
                };
            } else {
                Tuple<IntPtr, IntPtr> range = ProgramPointer.GetAddressRange(program, asmName);
                Searcher.MemoryFilter = delegate (MemInfo info) {
                    return (ulong)info.BaseAddress >= (ulong)range.Item1 && (ulong)info.BaseAddress <= (ulong)range.Item2 && (info.State & 0x1000) != 0 && (info.Protect & 0x20) != 0 && (info.Protect & 0x100) == 0;
                };
            }

            BasePtr = Searcher.FindSignature(program, Signature);
            if (BasePtr != IntPtr.Zero) {
                LastVerified = DateTime.Now.AddSeconds(5);
                int offset = CalculateRelative(program);
                return BasePtr + offset;
            }
            return BasePtr;
        }
        private int CalculateRelative(Process program) {
            int maxIndex = Relative.Length - 1;
            if (Relative == null || maxIndex < 0) { return 0; }

            int offset = 0;
            for (int i = 0; i < maxIndex; i++) {
                offset += Relative[i];
                offset += program.Read<int>(BasePtr + offset) + 4;
            }
            return offset + Relative[maxIndex];
        }
    }
    public class FindOffset : IFindPointer {
        private int[] Offsets;
        private IntPtr BasePtr;
        public PointerVersion Version { get; private set; }

        public FindOffset(PointerVersion version, params int[] offsets) {
            Version = version;
            Offsets = offsets;
        }

        public bool FoundBaseAddress() {
            return BasePtr != IntPtr.Zero;
        }
        public bool VerifyPointer(Process program) {
            return true;
        }
        public IntPtr FindPointer(Process program, string asmName) {
            if (string.IsNullOrEmpty(asmName)) {
                BasePtr = program.MainModule.BaseAddress;
            } else {
                Tuple<IntPtr, IntPtr> range = ProgramPointer.GetAddressRange(program, asmName);
                BasePtr = range.Item1;
            }

            return program.Read<IntPtr>(BasePtr, Offsets);
        }
    }
}