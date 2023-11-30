using Libraries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mario_Picross_GFX
{
    class Program
    {
        public enum WriteArgs
        {
            action,
            newRomPath,
            uncompressedPath,
            compressedPath,
        }

        public enum DumpArgs
        {
            action,
            romPath,
            dumpPath,
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception($"Cannot have 0 arguments. {args[0]}");
            }

            var action = args[0];
            int requiredLength;

            switch (action)
            {
                case "Write":
                    Console.WriteLine($"Writing");

                    requiredLength = (int)Enum.GetValues(typeof(WriteArgs)).Cast<WriteArgs>().Max() + 1;
                    if (args.Length != requiredLength)
                    {
                        throw new Exception($"Required argument number: {requiredLength}. Received: {args.Length}");
                    }

                    var uncompressedPath = args[(int)WriteArgs.uncompressedPath];
                    var compressedPath = args[(int)WriteArgs.compressedPath];
                    var newRomPath = args[(int)WriteArgs.newRomPath];
                    PicrossWriteAllGeneral(newRomPath, uncompressedPath, compressedPath);
                    break;
                case "Dump":
                    Console.WriteLine($"Dumping");

                    requiredLength = (int)Enum.GetValues(typeof(DumpArgs)).Cast<DumpArgs>().Max() + 1;
                    if (args.Length != requiredLength)
                    {
                        throw new Exception($"Required argument number: {requiredLength}. Received: {args.Length}");
                    }

                    var romPath = args[(int)DumpArgs.romPath];
                    var dumpPath = args[(int)DumpArgs.dumpPath];
                    PicrossDumpAllGeneral(romPath, dumpPath);


                    break;
                default:
                    throw new Exception($"Invalid first parameter: {action}");
            }

            Console.WriteLine($"Finished successfully.");
        }

        private static void WritePointer(ref byte[] romUnheader, uint pointer, uint romLocation)
        {
            var pointerFormat = new string[] { "F4", "(B2)", "00", "F4", "(B0)", "(B1)" };

            var snesAddr = AddressHiRom.PcToSnes(pointer, out bool _);
            var newPointer = PointerManager.CreateAddressCustomFormat(snesAddr, pointerFormat);
            Array.Copy(newPointer, 0, romUnheader, romLocation, newPointer.Length);
        }

        private static void WritePointerNormal(ref byte[] romUnheader, uint pointer, uint romLocation)
        {
            var pointerFormat = new string[] { "(B0)", "(B1)", "(B2)" };

            var snesAddr = AddressHiRom.PcToSnes(pointer, out bool _);
            var newPointer = PointerManager.CreateAddressCustomFormat(snesAddr, pointerFormat);
            Array.Copy(newPointer, 0, romUnheader, romLocation, newPointer.Length);
        }

        private static void PicrossWriteAllGeneral(string newRomPath, string uncompressedPath, string compressedPath)
        {
            var aaa = new DirectoryInfo(uncompressedPath).GetFiles("*.bin");
            foreach (var file in new DirectoryInfo(uncompressedPath).GetFiles("*.bin").OrderBy(f => f.Name).ToArray())
            {
                byte[] outy = Master.Compress(File.ReadAllBytes(file.FullName), CompressionType.LZ1);
                File.WriteAllBytes(Path.Combine(compressedPath, file.Name), outy);
            }

            int startAddress = 0x00048800;
            byte[] romBytes = File.ReadAllBytes(newRomPath);

            List<uint> pointers = new();

            foreach (var file in new DirectoryInfo(compressedPath).GetFiles("*.bin").OrderBy(f => f.Name).ToArray())
            {
                byte[] compressFile = File.ReadAllBytes(file.FullName);

                byte[] sizeHeader = BitConverter.GetBytes((ushort)compressFile.Length);

                compressFile = sizeHeader.Concat(compressFile).ToArray();

                Array.Copy(compressFile, 0, romBytes, startAddress, compressFile.Length);
                pointers.Add((uint)startAddress);
                Console.WriteLine($"File no: {Path.GetFileName(file.FullName),20} Address: 0x{startAddress:X2}");

                startAddress += compressFile.Length;
            }

            var pointerFormat = new string[] { "F4", "(B2)", "00", "F4", "(B0)", "(B1)" };
            // Update embedded pointer in-game

            // Titlescreen
            WritePointer(ref romBytes, pointers[18], 0x2287);
            WritePointer(ref romBytes, pointers[19], 0x22C6);
            WritePointer(ref romBytes, pointers[16], 0x2305);

            // Save Menu
            WritePointer(ref romBytes, pointers[14], 0x2A14);
            WritePointer(ref romBytes, pointers[13], 0x2A53);
            WritePointer(ref romBytes, pointers[15], 0x2A92);

            // Game select
            WritePointer(ref romBytes, pointers[10], 0x35EF);
            WritePointer(ref romBytes, pointers[11], 0x362E);
            WritePointer(ref romBytes, pointers[12], 0x366D);
            WritePointer(ref romBytes, pointers[09], 0x36ac);

            // Tutorial
            WritePointer(ref romBytes, pointers[02], 0xB61D);
            WritePointer(ref romBytes, pointers[03], 0xb65c);
            WritePointer(ref romBytes, pointers[03], 0xB702);
            WritePointer(ref romBytes, pointers[31], 0xB71D);

            // Puzzle screen
            WritePointer(ref romBytes, pointers[08], 0xA721);
            WritePointer(ref romBytes, pointers[07], 0xA760);
            WritePointer(ref romBytes, pointers[06], 0xa79f);

            // Puzzle (Mario)
            WritePointer(ref romBytes, pointers[02], 0x5094);
            WritePointer(ref romBytes, pointers[05], 0x50d3);
            WritePointer(ref romBytes, pointers[03], 0x5112);

            // Puzzle (Wario)
            WritePointer(ref romBytes, pointers[04], 0x5156);

            // Puzzle complete
            WritePointer(ref romBytes, pointers[30], 0x7f33);

            // Mario Milestone (Mario Level #1)
            WritePointer(ref romBytes, pointers[28], 0x9E35);
            WritePointerNormal(ref romBytes, pointers[20], 0xE0EF);
            WritePointerNormal(ref romBytes, pointers[20], 0xE0FB);

            // Wario Milestone (Mario Level #1)
            WritePointer(ref romBytes, pointers[29], 0x9EA5);
            WritePointerNormal(ref romBytes, pointers[22], 0xE0FF);

            // Wario Milestone (Wario Level X)
            WritePointerNormal(ref romBytes, pointers[23], 0xE103);

            // Wario Milestone (Wario Special stage unlock)
            WritePointerNormal(ref romBytes, pointers[24], 0xE107);

            // Game over
            WritePointer(ref romBytes, pointers[31], 0x8C6A);

            // Save Message (Mario)
            WritePointer(ref romBytes, pointers[28], 0x9E35);
            WritePointerNormal(ref romBytes, pointers[21], 0x0000E0F3);
            WritePointerNormal(ref romBytes, pointers[21], 0x0000E0F7);

            // Save Message (Wario)
            WritePointerNormal(ref romBytes, pointers[25], 0xE10B);

            // Resume saved game (Mario)
            WritePointer(ref romBytes, pointers[02], 0x5514);
            WritePointer(ref romBytes, pointers[05], 0x5553);
            WritePointer(ref romBytes, pointers[03], 0x5592);

            // Resume saved game (Mario)
            WritePointer(ref romBytes, pointers[04], 0x55D6);

            // Credits
            WritePointer(ref romBytes, pointers[02], 0x22198);
            WritePointer(ref romBytes, pointers[03], 0x221D7);

            // Wario EX Mode
            WritePointer(ref romBytes, pointers[17], 0x2487);
            WritePointer(ref romBytes, pointers[19], 0x24C6);
            WritePointer(ref romBytes, pointers[16], 0x2505);

            // Region lockout
            WritePointer(ref romBytes, pointers[02], 0x26651);
            WritePointer(ref romBytes, pointers[00], 0x26612);

            // Anti-piracy
            WritePointer(ref romBytes, pointers[01], 0x269C6);
            WritePointer(ref romBytes, pointers[02], 0x26A05);

            // Erase Screen
            WritePointer(ref romBytes, pointers[15], 0x3311);

            // Unused Save Screen GFX
            WritePointer(ref romBytes, pointers[02], 0x436D);
            WritePointer(ref romBytes, pointers[27], 0x43AC);

            File.WriteAllBytes(newRomPath, romBytes);
        }

        private static void PicrossDumpAllGeneral(string romPath, string outputPath)
        {
            byte[] romUnheader = File.ReadAllBytes(romPath);

            // Total size: 0x19DCB (105,931)
            uint[] Addresses = new uint[]
            {
                0x00048800, // size: 0x4BD  (1,213)
                0x00048cbd, // size: 0x191E (6,430)
                0x0004A5DB, // size: 0x259D (9,629)
                0x0004cb78,
                0x0004e640,
                0x0004eadc,
                0x0004f250,
                0x00050a0e,
                0x00050a8f,
                0x00050c4a,
                0x000510B5,
                0x00051215,
                0x000522e4,
                0x00053bba,
                0x0005455a,
                0x000546aa,
                0x000559b6,
                0x0005637b,
                0x00057339,
                0x000585b1, // size: 0x12DE (4,830)
                0x0005988f,
                0x0005aacc,
                0x0005b522,
                0x0005c374,
                0x0005d29f,
                0x0005e361,
                0x00060103,
                0x00060340,
                0x000608f9,
                0x000611b6,
                0x00061a29,
                0x000625cb
            };

            for (int i = 0; i < Addresses.Length; i++)
            {
                File.WriteAllBytes(Path.Combine(outputPath, $"{i:00}.output.bin"), Master.Decompress(romUnheader, Addresses[i], CompressionType.MarioPicross));
            }
        }
    }
}
