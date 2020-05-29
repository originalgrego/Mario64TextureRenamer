using HashDepot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Mario64TextureRenamer {
    class Program {

        static uint FAST_CRC_CHECKING_INC_X = 13;
        static uint FAST_CRC_CHECKING_INC_Y = 11;
        static uint FAST_CRC_MIN_Y_INC = 2;
        static uint FAST_CRC_MIN_X_INC = 2;
        static uint FAST_CRC_MAX_X_INC = 7;
        static uint FAST_CRC_MAX_Y_INC = 3;

        public static uint riceCRC(UInt32[] data, UInt32 left, UInt32 top, UInt32 width, UInt32 height, UInt32 size, UInt32 pitchInBytes) {
            UInt32 dwAsmdwBytesPerLine;
            UInt32 dwAsmCRC;

            dwAsmCRC = 0;
            dwAsmdwBytesPerLine = (UInt32)(((int)width << (int)size) + 1) / 2;

            UInt32 realWidthInDWORD = dwAsmdwBytesPerLine >> 2;
            UInt32 xinc = realWidthInDWORD / FAST_CRC_CHECKING_INC_X;
            if (xinc < FAST_CRC_MIN_X_INC) {
                xinc = Math.Min(FAST_CRC_MIN_X_INC, width);
            }
            if (xinc > FAST_CRC_MAX_X_INC) {
                xinc = FAST_CRC_MAX_X_INC;
            }

            UInt32 yinc = height / FAST_CRC_CHECKING_INC_Y;
            if (yinc < FAST_CRC_MIN_Y_INC) {
                yinc = Math.Min(FAST_CRC_MIN_Y_INC, height);
            }
            if (yinc > FAST_CRC_MAX_Y_INC) {
                yinc = FAST_CRC_MAX_Y_INC;
            }

            UInt32 pitch = pitchInBytes >> 2;
            UInt32 pStart = 0;
            pStart += (top * pitch) + (UInt32)((((int)left << (int)size) + 1) >> 3);

            // The original assembly code had a bug in it (it incremented pStart by 'pitch' in bytes, not in dwords)
            // This C code implements the same algorithm as the ASM but without the bug
            UInt32 y = 0;
            while (y < height) {
                uint x = 0;
                while (x < realWidthInDWORD) {
                    dwAsmCRC = (dwAsmCRC << 4) + ((dwAsmCRC >> 28) & 15);
                    dwAsmCRC += data[pStart + x];
                    x += xinc;
                    dwAsmCRC += x;
                }
                dwAsmCRC ^= y;
                y += yinc;
                pStart += pitch;
            }
            return dwAsmCRC;
        }

        public static String getHex(int number, byte hexDigits) {
            return number.ToString("X" + hexDigits);
        }

        static void rescursiveDig(String directoryName, List<String> filesList) {
            IEnumerable<String> files = Directory.EnumerateFiles(directoryName);
            filesList.AddRange(files);

            IEnumerable<String> data = Directory.EnumerateDirectories(directoryName);
            foreach (String directory in data) {
                files = Directory.EnumerateFiles(directory);
                filesList.AddRange(files);

                rescursiveDig(directory, filesList);
            }
        }

        private static void doRename(string[] args) {
            List<String> images = new List<String>();
            rescursiveDig(args[1], images);

            List<String> rawDataFiles = new List<String>();
            rescursiveDig(args[2], rawDataFiles);

            foreach (String imageName in images) {
                if (imageName.Contains(".png")) {
                    int count = 0;
                    String sub = imageName.Substring(args[1].Length, imageName.Length - 5 - args[1].Length);
                    foreach (String rawFileName in rawDataFiles) {
                        if (!rawFileName.Contains(".c") && rawFileName.Contains(sub)) {
                            byte[] bytes = File.ReadAllBytes(rawFileName);
                            uint result = XXHash.Hash32(bytes);
                            Console.WriteLine("Found one " + count + " - " + sub + " crc " + getHex((int)result, 8) + rawFileName);
                            byte[] imageData = File.ReadAllBytes(imageName);
                            File.WriteAllBytes(args[3] + getHex((int)result, 8) + ".png", imageData);
                            count++;
                        }
                    }
                }
            }
        }

        private static void generateSplitConfig(string[] args) {
            List<String> images = new List<String>();
            rescursiveDig(args[1], images);

            List<Bitmap> splitFiles = new List<Bitmap>();
            List<String> splitFileNames = new List<String>();
            foreach (String imageFile in images) {
                if (imageFile.Contains(".png")) {
                    splitFiles.Add(new Bitmap(imageFile));
                    splitFileNames.Add(imageFile.Substring(imageFile.LastIndexOf("\\") + 1));
                }
            }

            String fullImageName = args[2];
            int slashIndex = fullImageName.LastIndexOf("\\") + 1;
            String subFullImageFileName = fullImageName.Substring(slashIndex, fullImageName.Length - slashIndex - 4);
            Bitmap fullImage = new Bitmap(args[2]);

            String splitConfig = "";

            for (int index = 0; index < splitFiles.Count; index ++) {
                if (!splitFileNames[index].Contains(subFullImageFileName)) {
                    continue;
                }

                Bitmap splitImage = splitFiles[index];

                bool handled = false;
                for (int x = 0; !handled && x < fullImage.Width; x ++) {
                    for (int y = 0; !handled && y < fullImage.Height; y++) {
                        bool found = true;
                        for (int dx = 0; found && dx < splitImage.Width; dx ++) {
                            for (int dy = 0; found && dy < splitImage.Height; dy++) {
                                int imageX = x + dx;
                                if (imageX >= fullImage.Width) {
                                    imageX -= fullImage.Width;
                                }
                                int imageY = y + dy;
                                if (imageY >= fullImage.Height) {
                                    imageY -= fullImage.Height;
                                }
                                found = fullImage.GetPixel(imageX, imageY) == splitImage.GetPixel(dx, dy);
                            }
                        }

                        if (found) {
                            handled = true;
                            Console.WriteLine("Found split file at " + x + ", " + y);
                            splitConfig += splitFileNames[index] + "," + x + "," + y + "\r\n";
                        }
                    }
                }
            }

            if (splitConfig != "") {
                File.WriteAllText(args[3], splitConfig);
            }
        }

        static void Main(string[] args) {
            if ("rename".Equals(args[0])) {
                doRename(args);
            } else if ("genSplitCfg".Equals(args[0])) {
                generateSplitConfig(args);
            } else if ("applySplitCfg".Equals(args[0])) {
                applySplitConfig(args);
            } else if ("recreateFromCfg".Equals(args[0])) {
                recreateImageFromConfig(args);
            } else if ("checkHashCol".Equals(args[0])) {
                checkHashCollisions(args);
            } else if ("genCRCtoName".Equals(args[0])) {
                generateCRCToNameFile(args);
            }

            Console.WriteLine("Done");
        }

        private static void generateCRCToNameFile(string[] args) {
            List<String> images = new List<String>();
            rescursiveDig(args[1], images);

            List<String> rawDataFiles = new List<String>();
            rescursiveDig(args[2], rawDataFiles);

            String resultString = "";
            foreach (String imageName in images) {
                if (imageName.Contains(".png")) {
                    String sub = imageName.Substring(args[1].Length, imageName.Length - 4 - args[1].Length);
                    foreach (String rawFileName in rawDataFiles) {
                        if (!rawFileName.Contains(".c") && rawFileName.Contains(sub)) {
                            Bitmap image = new Bitmap(imageName);
                            byte[] bytes = File.ReadAllBytes(rawFileName);
                            uint[] words = new uint[bytes.Length / 4];
                            for (int x = 0; x < bytes.Length; x += 4) {
                                   words[x / 4] = (uint)(bytes[x + 3] << 24 | bytes[x + 2] << 16 | bytes[x + 1] << 8 | bytes[x]);
                                //  words[x / 4] = (uint)(bytes[x] << 24 | bytes[x + 1] << 16 | bytes[x + 2] << 8 | bytes[x + 3]);
                            }
                            if (words.Length >= 0x100) {
                                uint result = riceCRC(words, 0, 0, (uint)image.Width, (uint)image.Height, 2, (uint)image.Width * 2);
                                // uint result = XXHash.Hash32(bytes);
                                resultString += getHex((int)result, 8) + "," + sub + ".png\r\n";
                            }
                        }
                    }
                }
            }

            File.WriteAllText("crc_to_name_map.txt", resultString);
        }

        private static void checkHashCollisions(string[] args) {
            List<String> images = new List<String>();
            rescursiveDig(args[1], images);

            int count = 0;
            List<int> crcs = new List<int>();
            List<int> hashes = new List<int>();
            foreach (String imageName in images) {
                if (imageName.Contains(".png")) {
                    int index = imageName.LastIndexOf("\\") + 1;
                    String crcString = imageName.Substring(index, imageName.Length - index - 4);
                    if (crcString.Length == 8) {
                        int crc = int.Parse(crcString, System.Globalization.NumberStyles.HexNumber);
                        int hash = (crc & 0x3fffff);
                        if (hashes.Contains(hash)) {
                            Console.WriteLine("Hash Collision " + count + "  - " + getHex(hash, 4) + " CRC 1" + getHex(crc, 8) + " CRC 2" + getHex(crcs[hashes.IndexOf(hash)], 8));
                            count++;
                        }
                        hashes.Add(hash);
                        crcs.Add(crc);
                    }
                }
            }
        }

        private static void recreateImageFromConfig(string[] args) {
            Bitmap fullImage = new Bitmap(args[1]);
            Bitmap result = new Bitmap(fullImage.Width, fullImage.Height);

            String splitDir = args[2].Substring(0, args[2].LastIndexOf("\\") + 1);
            String configData = File.ReadAllText(args[2]);
            String[] configLines = configData.Split(new[] { "\r\n" }, StringSplitOptions.None);
            foreach (String configLine in configLines) {
                if (configLine != "") {
                    String[] config = configLine.Split(new[] { "," }, StringSplitOptions.None);
                    Bitmap splitImage = new Bitmap(splitDir + config[0]);
                    if (config.Length == 3) {
                        int xStart = int.Parse(config[1]) * 8;
                        int yStart = int.Parse(config[2]) * 8;
                        for (int x = 0; x < splitImage.Width; x++) {
                            for (int y = 0; y < splitImage.Height; y++) {
                                result.SetPixel(xStart +  x, yStart + y, splitImage.GetPixel(x, y));
                            }
                        }
                    }
                }
            }

            result.Save("result.png");
        }

        private static void applySplitConfig(string[] args) {
            int originalSplitWidth = int.Parse(args[1]);
            int originalSplitHeight = int.Parse(args[2]);

            int newScale = int.Parse(args[3]);

            Bitmap fullImage = new Bitmap(args[4]);

            String configData = File.ReadAllText(args[5]);
            String[] configLines = configData.Split(new[] { "\r\n" }, StringSplitOptions.None);
            foreach (String configLine in configLines) {
                if (configLine != "" ) {
                    Bitmap result = new Bitmap(originalSplitWidth * newScale, originalSplitHeight * newScale);
                    String[] config = configLine.Split(new[] { "," }, StringSplitOptions.None);
                    if (config.Length == 3) {
                        int xStart = int.Parse(config[1]) * newScale;
                        int yStart = int.Parse(config[2]) * newScale;
                        for (int x = 0; x < result.Width; x++) {
                            for (int y = 0; y < result.Height; y++) {
                                int imageX = xStart + x;
                                if (imageX >= fullImage.Width) {
                                    imageX -= fullImage.Width;
                                }
                                int imageY = yStart + y;
                                if (imageY >= fullImage.Height) {
                                    imageY -= fullImage.Height;
                                }
                                result.SetPixel(x, y, fullImage.GetPixel(imageX, imageY));
                            }
                        }

                        result.Save(config[0]);
                    }
                }
            }
        }
    }
}
