using System.Collections.Generic;

namespace GenTreesCore.Models
{
    public class TreeUpdateResult
    {
        public Dictionary<int, int> Replacements { get; set; }
        public List<int> Errors { get; set; }
    }
}
