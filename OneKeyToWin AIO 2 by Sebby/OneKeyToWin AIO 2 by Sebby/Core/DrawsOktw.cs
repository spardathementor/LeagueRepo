using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using SharpDX;
using SharpDX.Direct3D9;
using SebbyLib;

namespace OneKeyToWin_AIO_2_by_Sebby.Core
{
    class DrawsOktw : Program
    {
        public static Font Tahoma13, Tahoma13B, TextBold;

        public DrawsOktw()
        {
            Tahoma13B = new Font(Drawing.Direct3DDevice, new FontDescription
            { FaceName = "Tahoma", Height = 14, Weight = FontWeight.Bold, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });

            Tahoma13 = new Font(Drawing.Direct3DDevice, new FontDescription
            { FaceName = "Tahoma", Height = 14, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });

            TextBold = new Font(Drawing.Direct3DDevice, new FontDescription
            { FaceName = "Impact", Height = 30, Weight = FontWeight.Normal, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });

            
        }
    }
}
