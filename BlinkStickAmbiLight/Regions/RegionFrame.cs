#region License
/*
*
* The MIT License (MIT)
*
* Copyright (c) 2017 René Kannegießer
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BlinkStickAmbiLight
{
	public class RegionFrame
	{
		public List<Region> regions;
		public int[] leds;
		
		/// <summary>
        /// Build the region frame
        /// </summary>
        /// <param name="top">Amount of top regions</param>
        /// <param name="bottom">Amount of top regions</param>
        /// <param name="left">Amount of left regions</param>
        /// <param name="right">Amount of right regions</param>
        /// <param name="screenwidth">Screen width</param>
        /// <param name="screenheight">Screen height</param>
        /// <param name="size">Size of a region</param>
        /// <param name="ledshift">shift start led to the left (-) or to the right (+)</param>
		public RegionFrame(int top, int bottom, int left, int right, int screenwidth, int screenheight, int size, int ledshift = 0)
		{
			regions = new List<Region>();
			ledshift *= -1;
			int ledsum = top + bottom + left + right;
			leds = new int[ledsum];
			int idcount = 0;
			int avg_top = 0;
			int avg_bottom = 0;
			int avg_left = 0;
			int avg_right = 0;
			int size_factor_top = 2;
			int size_factor_bottom = 2;
			int rest = 0;
            // Make those parameters
            int topLeftPadding = 120;
            int topRightPadding = 120;

            // Left parameters
            int leftTopPadding = 0;
            int leftBottomPadding = 150;
            int leftTopMargin = 60;
            int leftBottomMargin = 0;

            // Right parameters
            int rightTopPadding = 0;
            int rightBottomPadding = 150;
            int rightTopMargin = 60;
            int rightBottomMargin = 0;


            // Calculate size factor
            size_factor_top = top > 0 ? 1 : 0;
			size_factor_bottom = bottom > 0 ? 1 : 0;
			
			// Rest value (remainder)
            // TODO: Review that logic, consider dropping it altogether?
			int remainder_top = top == 0 ? 0 : screenwidth % top;
			int remainder_bottom = bottom == 0 ? 0 : screenwidth % bottom;
			int remainder_left = left == 0 ? 0 : (screenheight - (size * (size_factor_top + size_factor_bottom))) % left;
			int remainder_right = right == 0 ? 0 : (screenheight - (size * (size_factor_top + size_factor_bottom))) % right;
			
			// Calculate region sizes
			avg_top = top > 0 ? (screenwidth - topLeftPadding - topRightPadding)  / top : 0;
			avg_bottom = bottom > 0 ? screenwidth / bottom : 0;
			avg_left = left > 0 ? (screenheight - leftTopMargin - leftBottomMargin - leftTopPadding - leftBottomPadding) / left : 0;
			avg_right = right > 0 ? (screenheight - rightTopMargin - rightBottomMargin - rightTopPadding - rightBottomPadding) / right : 0;
			
			FillLEDArray(ledsum);
			for (int i = 0; i < ledshift; i++)
				leds = LEDArrayShiftLeft(leds);
			
			for (int i = 0; i > ledshift; i--)
				leds = LEDArrayShiftRight(leds);
			
			// Left Regions, from bottom to top
			for (int i = 1; i < left+1; i++)
			{
				rest = i < remainder_left ? 1 : 0;

                int y = screenheight - ((avg_left + rest) * i);
                int height = avg_left + rest;
                if (i >= remainder_left)
                {
                    y -= remainder_left;
                    height++;
                }

                y -= leftBottomPadding + leftBottomMargin;

                // Take into account top margin
                y -= leftBottomMargin;
                
                if (i == 1)
                {
                    height += leftBottomPadding;
                }
                else if (i == left)
                {
                    height += leftTopPadding;
                }

                regions.Add(new Region
				            {
				            	id = idcount,
				            	led_id = leds[idcount],
				            	color = Color.Black,

				            	rect = new Rectangle(0,
				            	                     y,
				            	                     size,
				            	                     height),
				            	channel = CalculateChannel(idcount),
				            });
				idcount++;
			}
			
			// Top Regions, from left to right
			for (int i = 0; i < top; i++)
			{
				rest = i < remainder_top ? 1 : 0;
                // Compute width
                int width = avg_top + rest;
                if (i==0)
                {
                    // First top region width is affected by padding
                    width += topLeftPadding;
                }
                else if (i==top-1)
                {
                    // Last top region width is affected by padding
                    width += topRightPadding;
                }

                // Compute x position
                int x = i >= remainder_top ? ((avg_top + rest) * i) + remainder_top : (avg_top + rest) * i;
                if (i>0)
                {
                    // Take padding into account beyond first region
                    x += topLeftPadding;
                }

                regions.Add(new Region
				            {
				            	id = idcount,
				            	led_id = leds[idcount],
				            	color = Color.Black,
				            	rect = new Rectangle(x,
				            	                     0,
                                                     width,
				            	                     size),
				            	channel = CalculateChannel(idcount),
				            });
				idcount++;
			}
						
			// Right Regions, from top to bottom
			for (int i = 0; i < right; i++)
			{
                rest = i < remainder_right ? 1 : 0;

                int y =  ((avg_right + rest) * i);
                if (i >= remainder_right)
                {
                    y += remainder_right;
                }

                y += rightTopMargin;

                int height = avg_right + rest;
                if (i == 0)
                {
                    // First top region width is affected by padding
                    height += rightTopPadding;
                }
                else if (i == right - 1)
                {
                    // Last top region width is affected by padding
                    height += rightBottomPadding;
                }


                regions.Add(new Region
				            {
				            	id = idcount,
				            	led_id = leds[idcount],
				            	color = Color.Black,
				            	rect = new Rectangle(screenwidth - size,
				            	                     y,
				            	                     size,
                                                     height),
				            	channel = CalculateChannel(idcount),
				            });
				idcount++;
			}

	
			// Bottom Regions, from right to left
            // TODO: support margin and padding
			for (int i = 1; i < bottom+1; i++)
			{
				rest = i < remainder_bottom ? 1 : 0;
				regions.Add(new Region
				            {
				            	id = idcount,
				            	led_id = leds[idcount],
				            	color = Color.Black,
				            	rect = new Rectangle(i >= remainder_bottom ? screenwidth - ((avg_bottom + rest) * i) - remainder_bottom : screenwidth - ((avg_bottom + rest) * i),
				            	                     screenheight - size,
				            	                     i >= remainder_bottom ? avg_bottom + rest+1 : avg_bottom + rest,
				            	                     size),
				            	
				            	channel = CalculateChannel(idcount),
				            });
				idcount++;
			}
		}

		public static byte CalculateChannel(int ledsum)
		{ 
			if (ledsum >= 64 && ledsum < 128) {
				return 1;
			}
			if (ledsum >= 128) {
				return 2;
			}
			return 0;
		}
		
		public void FillLEDArray(int ledsum)
		{
			for (int i = 0; i < ledsum; i++)
			{
				leds[i] = i;
			}
		}
		
		public static int[] LEDArrayShiftLeft(int[] arr)
		{
			var temp = new int[arr.Length];
			for (int i = 0; i < arr.Length - 1; i++)
			{
				temp[i] = arr[i + 1];
			}
			temp[temp.Length - 1] = arr[0];
			return temp;
		}
		
		public static int[] LEDArrayShiftRight(int[] arr)
		{
			var temp = new int[arr.Length];
			for (int i = 1; i < arr.Length; i++)
			{
				temp[i] = arr[i - 1];
			}
			temp[0] = arr[temp.Length - 1];
			return temp;
		}
	}
}

