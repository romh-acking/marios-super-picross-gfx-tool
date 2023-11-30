using System;
using System.Collections.Generic;
using System.Linq;

namespace Libraries.Compression
{
    public class MarioPicross : ICompressor, IDecompressor
    {
        private enum CommandEnum
        {
            Copy = 0,
            WriteByte = 1
        }

        public byte[] Decompress(byte[] CompressedData, uint Start)
        {
            uint Position = Start;

            List<byte> Output = new();
            int ByteSize = (CompressedData[Position + 1] << 8) + CompressedData[Position];
            Position += 2;

            int Commands = 0;
            ushort Y = 0x0fee;

            byte[] Buffer = new byte[0x1000];

            while (Output.Count < ByteSize)
            {
                Commands >>= 0x1;

                if ((Commands & 0x0100) == 0)
                {
                    Commands = CompressedData[Position++] | 0xFF00;
                }

                switch ((CommandEnum)(Commands & 0x1))
                {
                    case CommandEnum.WriteByte:
                        Output.Add(Buffer[Y++] = CompressedData[Position++]);
                        Y &= 0x0FFF;
                        break;

                    /*
                     * Command Format:
                     * 
                     * aaaa aaaa    bbbb zzzz
                     * 
                     * location = bbbbaaaaaaaa 
                     * size     = zzzz
                     */
                    case CommandEnum.Copy:
                        int BytesLeft = (CompressedData[Position] & 0x0f) + 3;
                        int i = ((CompressedData[Position + 1] << 8) + CompressedData[Position]) >> 4;

                        Position += 2;

                        do
                        {
                            Output.Add(Buffer[Y++] = Buffer[i++]);
                            Y &= 0x0FFF;
                            i &= 0x0FFF;
                        }
                        while (--BytesLeft != 0);

                        break;
                    default:
                        throw new Exception();
                }
            }

            return Output.ToArray();
        }

        // Hack way of compressing.
        public byte[] Compress(byte[] Uncompressed)
        {
            //SortedDictionary<byte[], uint> Dict = new(new ByteComparer());

            List<byte> Output = new();
            List<byte> TempOutput = new();

            uint Position = 0;
            byte Command = 0x0;
            int CommandIndex = 0x0;

            while (Position < Uncompressed.Length)
            {
                if (CommandIndex == 8 * 4)
                {
                    Output.Add(Command);
                    Output = Output.Concat(TempOutput).ToList();
                    TempOutput = new List<byte>();
                    Command = 0;
                    CommandIndex = 0;
                }
                else
                {
                    Command <<= 1;
                    CommandIndex++;
                }

                if (true)
                {
                    TempOutput.Add(Uncompressed[Position++]);
                    Command |= (byte)CommandEnum.WriteByte;
                }
            }

            byte[] SizeHeader = new byte[] { (byte)((Output.Count & 0xFF00) << 8), (byte)(Output.Count & 0xFF) };
            return SizeHeader.Concat(Output).ToArray();
        }
    }
}