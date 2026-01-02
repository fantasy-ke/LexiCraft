using System.Collections.Generic;
using BuildingBlocks.OSS.Models.Policy;

namespace BuildingBlocks.OSS.Models
{
    public class PolicyInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<StatementItem> Statement { get; set; }
    }
}
