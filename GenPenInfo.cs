using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GenPen
{
    public class GenPenInfo : GH_AssemblyInfo
    {
        public override string Name => "GenPen";
        public override Bitmap Icon => null;
        public override string Description => "GenPenプラグインの説明";
        public override Guid Id => new Guid("b2c3d4e5-f6a5-b9c8-d7e6-f5a4b3c2d1a2");
        public override string AuthorName => "作者名";
        public override string AuthorContact => "連絡先";
    }
}