using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using Image = System.Windows.Controls.Image;

namespace DrawingApp
{
    class Layer
    {
        public readonly System.Windows.Controls.Image img = new Image();
        public readonly WriteableBitmap BMap;
        private byte[] Pixels;
        private int bpr;
        private List<int> ignoreList = new List<int>();

        // Get a pixel's value.
        public System.Windows.Media.Color GetPixelColor(int x, int y)
        {
            int index = y * bpr + x * 4;
            Byte b = Pixels[index++];
            Byte g = Pixels[index++];
            Byte r = Pixels[index++];
            Byte a = Pixels[index];
            return System.Windows.Media.Color.FromArgb(a, r, g, b);
        }

        // returns the pixels's starting index
        public int GetPixelIndex(int x, int y)
        {
            int index = y * bpr + x * 4;
            return index;
        }

        // Constructor
        public Layer(WriteableBitmap wrBmp) {
            if (wrBmp != null)
            {
                BMap = wrBmp;
                img.Source = BMap;
                int width = (int)wrBmp.Width;
                int height = (int)wrBmp.Height;
                Pixels = new byte[width * height * 4];
                bpr = width * 4;
                wrBmp.CopyPixels(Pixels, bpr, 0);
            }
        }

        // Converts Mouse location on the image to the corresponding location on the bitmap
        public System.Windows.Point ToBmpLocation(System.Windows.Point mPos, int width, int height)
        {
            int x = (int)Math.Round(BMap.PixelWidth * (mPos.X / width));
            int y = (int)Math.Round(BMap.PixelHeight * (mPos.Y / height));
            return new System.Windows.Point(x, y);
        }

        // returns pixels within a range from the cursor location
        private List<System.Windows.Point> GetPointsInCircle(System.Windows.Point center, int range)
        {
            List<System.Windows.Point> indices = new List<System.Windows.Point>();
            int radSqr = (range / 2) * (range / 2);
            // loop through all pixels within a rectangular area around the center point
            for (int x = ((int)(center.X - range)); x < (BMap.Width - (BMap.Width - (center.X + range))); x++)
            {
                for (int y = ((int)(center.Y - range)); y < (BMap.Height - (BMap.Height - (center.Y + range))); y++)
                {
                    double dx = x - center.X;
                    double dy = y - center.Y;
                    double distanceSquared = dx * dx + dy * dy;
                    // if in location add it to list of pixels to be painted on
                    if (distanceSquared <= radSqr)
                    {
                        indices.Add(new System.Windows.Point(x,y));
                    }
                }
            }
            return indices;
        }

        // Mix pixel color with the brush color based on the blend mode
        private System.Windows.Media.Color MixColors(int index, System.Windows.Media.Color c, String blendMode)
        {
            System.Windows.Media.Color finalColor;
            double alphaByte = (double)Pixels[index + 3] / 255;
            double cTempAlpha = (double)c.A / 255;
            double totalAlpha = alphaByte + cTempAlpha;
            double cAlpha;
            if (totalAlpha > 1)
            {
                cAlpha = 1;
            }
            else
            {
                cAlpha = alphaByte + cTempAlpha;
            }
            if (blendMode == "Normal") // Tries to blend the pixel and brush my math is wonky I had to mess around with it a lot
            {
                // if either the brush has very low transparency or the pixel being drawn on is almost transparent, just paint the solid brush color onto the pixel
                if ((c.A >= 250) || (Pixels[index + 3] < 10))
                {
                    return c;
                }
                Byte cRed = (byte)((Pixels[index + 2] * alphaByte / totalAlpha) + (c.R * (1- (alphaByte / totalAlpha))));
                Byte cGreen = (byte)((Pixels[index + 1] * alphaByte / totalAlpha) + (c.G * (1 - (alphaByte / totalAlpha))));
                Byte cBlue = (byte)((Pixels[index] * alphaByte / totalAlpha) + (c.B * (1 - (alphaByte / totalAlpha))));

                finalColor = System.Windows.Media.Color.FromArgb((byte)(cAlpha * 255), cRed, cGreen, cBlue);
            }else if (blendMode == "Eraser") // is eraser, opacity decides strength
            {
                Byte cRed = 0;
                Byte cGreen = 0;
                Byte cBlue = 0;
                // So Alpha doesnt go below 0 if it does it starts back at 255
                if (((int)Pixels[index + 3] - (int)c.A) < 10)
                {
                    cAlpha = 0;
                }
                else
                {
                    cAlpha = (byte)(Pixels[index + 3] - c.A);
                    cRed = Pixels[index + 2];
                    cGreen = Pixels[index + 1];
                    cBlue = Pixels[index];
                }
                finalColor = System.Windows.Media.Color.FromArgb((byte)cAlpha, cRed, cGreen, cBlue);
            }else if (blendMode == "Lighten") // Compares colors and returns the lighter color
            {
                double pHSP = CalculateHSP(Pixels[index + 2], Pixels[index + 1], Pixels[index]);
                double bHSP = CalculateHSP(c.R, c.G, c.B);
                if (pHSP > bHSP)
                {
                    finalColor = System.Windows.Media.Color.FromArgb((byte)(cAlpha * 255), Pixels[index + 2], Pixels[index + 1], Pixels[index]);
                }
                else
                {
                    finalColor = System.Windows.Media.Color.FromArgb((byte)(cAlpha * 255), c.R, c.G, c.B);
                }
            }
            else if (blendMode == "Darken") // Compares colors and returns the darker color
            {
                double pHSP = CalculateHSP(Pixels[index + 2], Pixels[index + 1], Pixels[index]);
                double bHSP = CalculateHSP(c.R, c.G, c.B);
                if (pHSP > bHSP)
                {
                    finalColor = System.Windows.Media.Color.FromArgb((byte)(cAlpha * 255), c.R, c.G, c.B);
                }
                else
                {
                    finalColor = System.Windows.Media.Color.FromArgb((byte)(cAlpha * 255), Pixels[index + 2], Pixels[index + 1], Pixels[index]);
                }
            }
            return finalColor;
        }

        // HSP equation from http://alienryderflex.com/hsp.html
        private double CalculateHSP(Byte r, Byte g, Byte b)
        {
            double hsp = Math.Sqrt((0.299 * (r^2)) + (0.587 * (g^2)) + (0.114 * (b^2)));
            return hsp;
        }

        // Draw and Erase!!
        public void Draw(System.Windows.Point center, int range, String blendMode, System.Windows.Media.Color c)
        {
            List<System.Windows.Point> points = GetPointsInCircle(center, range);
            foreach(System.Windows.Point p in points)
            {
                int index = GetPixelIndex((int)p.X, (int)p.Y);
                if (ignoreList.Contains(index))
                {
                    continue;
                }
                if (index >= 0 && index + 3 < Pixels.Length)
                {
                    ignoreList.Add(index);
                    System.Windows.Media.Color finalColor;
                    finalColor = MixColors(index, c, blendMode);
                    Pixels[index++] = finalColor.B;
                    Pixels[index++] = finalColor.G;
                    Pixels[index++] = finalColor.R;
                    Pixels[index] = finalColor.A;
                }
            }
            BMap.WritePixels(new Int32Rect(0, 0, BMap.PixelWidth, BMap.PixelHeight), Pixels, bpr, 0);
        }

        // Save the bitmap as a png
        public void SaveBitmap(string path)
        {
            if (path != string.Empty && BMap !=null)
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(BMap));
                    encoder.Save(fs);
                }
            }
        }

        // User has lifted mouse press and it is a new pass
        public void NewPass()
        {
            ignoreList.Clear();
        }
    }
}
