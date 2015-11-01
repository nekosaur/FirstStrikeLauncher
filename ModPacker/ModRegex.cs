using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ModPacker
{
    public static class ModRegex
    {
        public static Regex IsZipFile = new Regex(@"(firststrike/(?:common|sound|menu|objects/[a-z0-9_]*|levels/[a-z0-9_]*))/((?!info)[a-z0-9-_/ ()=\.]{0,})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex IsServerFile = new Regex(@"\.(con|tweak|ske|baf|inc|collisionmesh|tai|emi|dat|cfg|ahm|qtr|ai|mat|clb|(?<!terraindata\.)raw)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
