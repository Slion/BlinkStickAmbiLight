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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

using SlimDX;
using SlimDX.Direct3D9;

namespace BlinkStickAmbiLight
{
	public partial class MainForm : Form
	{
		Device iDevice;
		Surface iSurface;
        Surface iSurfaceThumbnail;

        Bitmap DXScreen;
		
		private void DXInit()
		{
			try
			{
				var present_params = new PresentParameters();
				present_params.Windowed = true;
				present_params.SwapEffect = SwapEffect.Discard;
				present_params.BackBufferCount = 1;

				present_params.PresentationInterval = PresentInterval.Immediate;
				
				present_params.BackBufferHeight = Screen.AllScreens[iScreen].WorkingArea.Height;
				present_params.BackBufferWidth = Screen.AllScreens[iScreen].WorkingArea.Width;
				
				iDevice = new Device(new Direct3D(), 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, present_params);
			}
			catch (Exception ex)
			{
				log.Debug("[Init DirectX] - " + ex.Message);
			}
		}
		
		/// <summary>
        /// Get DX screen image
        /// </summary>
        /// <param name="rect">Screen rectangle</param>
        public Bitmap GetImage(Rectangle rect)
        {
            // TODO: Move those somewhere else
            pbPreview.Width = (Screen.AllScreens[iScreen].Bounds.Width) / preview_factor;
            pbPreview.Height = Screen.AllScreens[iScreen].Bounds.Height / preview_factor;


            try
        	{
        		lock (lockobj)
        		{
        			if (iSurface == null)
        			{
        				iSurface = Surface.CreateOffscreenPlain(iDevice, rect.Width, rect.Height, Format.A8R8G8B8, Pool.Scratch);
                        iSurfaceThumbnail = Surface.CreateOffscreenPlain(iDevice, pbPreview.Width, pbPreview.Height, Format.A8R8G8B8, Pool.Scratch);
                    }

                    var sw = new Stopwatch();

                    // Do our screen capture, that's the most expensive operation as it copies frame data from our GPU 
                    sw.Start();
                    iDevice.GetFrontBufferData(0, iSurface);
                    sw.Stop();
                    Debug.WriteLine("GetFrontBufferData: " + sw.ElapsedMilliseconds + "ms");

                    // Shrink our frame surface
                    sw.Restart();
                    Surface.FromSurface(iSurfaceThumbnail, iSurface, Filter.Linear, 0);
                    sw.Stop();
                    Debug.WriteLine("From surface schrink: " + sw.ElapsedMilliseconds + "ms");

                    sw.Restart();
                    DataRectangle gsx = iSurfaceThumbnail.LockRectangle(rect, LockFlags.None);                    
                    Bitmap bm = new Bitmap(pbPreview.Width, pbPreview.Height, CalculateStride(pbPreview.Width, PixelFormat.Format32bppPArgb), PixelFormat.Format32bppPArgb, gsx.Data.DataPointer);                    
                    Debug.WriteLine("Bitmap creation: " + sw.ElapsedMilliseconds + "ms");
                    iSurfaceThumbnail.UnlockRectangle();
                    sw.Stop();

                    return bm;
        		}
        	}
        	catch (Exception ex)
        	{
        		log.Debug("[DirectX GetScreenImage] - " + ex.Message);
        		return null;
        	}
        }

		private int CalculateStride(int width, PixelFormat pf)
		{
			int BitsPerPixel = ((int)pf & 0xff00) >> 8;
			return 4 * ((width * BitsPerPixel + 31) / 32);
		}
	}
}
