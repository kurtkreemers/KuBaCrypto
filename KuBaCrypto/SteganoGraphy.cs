using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;



namespace KuBaCrypto
{
    class SteganoGraphy
    {
        public static Bitmap embedData(string dataPath, Bitmap bmp)
        {
            int bitmapWidth = bmp.Width - 1;
            int bitmapHeight = bmp.Height - 1;

            using (Stream dataFileStream = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
            {
                Int32 dataLength = (Int32)dataFileStream.Length;

                if (dataFileStream.Length >= 16777215)
                { //The message is too long
                    String exceptionMessage = "The message is too long, only 16777215 bytes are allowed.";
                    throw new Exception(exceptionMessage);
                }
                Color pixel;
                //Write length into first pixel
                Int32 colorValue = dataLength;
                int red = colorValue >> 16;
                colorValue -= red << 16;
                int green = colorValue >> 8;
                int blue = colorValue - (green << 8);
                pixel = Color.FromArgb(red, green, blue);
                bmp.SetPixel(0, 0, pixel);

                Int32 charIndex = 0;
                int charValue = 0;
                long pixelElementIndex = 0;
                int R = 0, G = 0, B = 0;

                // pass through the rows
                for (int i = 0; i < bitmapHeight; i++)
                {
                    // pass through each row
                    for (int j = 0; j < bitmapWidth; j++)
                    {
                        // holds the pixel that is currently being processed
                        pixel = bmp.GetPixel(j, i);

                        // now, clear the least significant bit (LSB) from each pixel element
                        R = pixel.R - pixel.R % 2;
                        G = pixel.G - pixel.G % 2;
                        B = pixel.B - pixel.B % 2;
                        //Don't overwrite the first pixel
                        if (pixel != bmp.GetPixel(0, 0))
                        {
                            // for each pixel, pass through its elements (RGB)
                            for (int n = 0; n < 3; n++)
                            {
                                // check if new 8 bits has been processed
                                if (pixelElementIndex % 8 == 0)
                                {
                                    
                                    // check if all characters has been hidden
                                    if (charIndex >= dataLength)
                                    {
                                        if(pixelElementIndex%3 != 0)
                                        bmp.SetPixel(j, i, Color.FromArgb(R, G, B));
                                        return bmp;
                                    }
                                    else
                                    {
                                        // move to the next character and process again
                                        charValue = dataFileStream.ReadByte();
                                        charIndex++;
                                    }
                                }
                                // check which pixel element has the turn to hide a bit in its LSB
                                switch (pixelElementIndex % 3)
                                {
                                    case 0:
                                        {
                                            // the rightmost bit in the character will be (charValue % 2)
                                            // to put this value instead of the LSB of the pixel element
                                            R += charValue % 2;
                                            // removes the added rightmost bit of the character
                                            charValue /= 2;

                                        } break;
                                    case 1:
                                        {
                                            G += charValue % 2;
                                            charValue /= 2;
                                        } break;
                                    case 2:
                                        {
                                            B += charValue % 2;
                                            charValue /= 2;
                                            bmp.SetPixel(j, i, Color.FromArgb(R, G, B));
                                        } break;
                                }
                                pixelElementIndex++;
                            }
                        }
                    }
                }
            }
            return bmp;
        }

        public static void extractData(Bitmap bmp, string dataPath)
        {
            Color pixel = bmp.GetPixel(0, 0);
            Int32 dataLength;
            int bitmapWidth = bmp.Width - 1;
            int bitmapHeight = bmp.Height - 1;
            using (Stream dataFileStream = new FileStream(dataPath, FileMode.CreateNew, FileAccess.Write))
            {
                //Get datalenght from pixel
                pixel = bmp.GetPixel(0, 0);
                dataLength = (pixel.R << 16) + (pixel.G << 8) + pixel.B;

                int colorUnitIndex = 0;
                int charValue = 0;
                int charIndex = 0;

                // pass through the rows
                for (int i = 0; i < bitmapHeight; i++)
                {
                    // pass through each row
                    for (int j = 0; j < bitmapWidth; j++)
                    {
                        pixel = bmp.GetPixel(j, i);
                        //Not the first pixel
                        if (pixel != bmp.GetPixel(0, 0))
                        {
                            // for each pixel, pass through its elements (RGB)
                            for (int n = 0; n < 3; n++)
                            {
                                switch (colorUnitIndex % 3)
                                {
                                    case 0:
                                        {
                                            // get the LSB from the pixel element (will be pixel.R % 2)
                                            // then add one bit to the right of the current character
                                            // this can be done by (charValue = charValue * 2)
                                            // replace the added bit (which value is by default 0) with
                                            // the LSB of the pixel element, simply by addition
                                            charValue = charValue * 2 + pixel.R % 2;
                                        } break;
                                    case 1:
                                        {
                                            charValue = charValue * 2 + pixel.G % 2;
                                        } break;
                                    case 2:
                                        {
                                            charValue = charValue * 2 + pixel.B % 2;
                                        } break;
                                }
                                colorUnitIndex++;
                                // if 8 bits has been added,
                                if (colorUnitIndex % 8 == 0)
                                {
                                    // reverse? of course, since each time the process occurs on the right
                                    charValue = reverseBits(charValue);
                                    byte foundByte = (byte)charValue;
                                    dataFileStream.WriteByte(foundByte);
                                    charIndex++;
                                    if (charIndex >= dataLength)
                                        return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static int reverseBits(int n)
        {
            int result = 0;

            for (int i = 0; i < 8; i++)
            {
                result = result * 2 + n % 2;

                n /= 2;
            }

            return result;
        }
        public static Int64 CountPixels(Bitmap bmp)
        {
            int rows, cols, row, col;
            Int64 count = 0;
            rows = bmp.Height;
            cols = bmp.Width;
            for (row = 0; row < rows; row++)
            {
                for (col = 0; col < cols; col++)
                {
                    count += 1;
                }
            }
            return count;
        }
    }

}
