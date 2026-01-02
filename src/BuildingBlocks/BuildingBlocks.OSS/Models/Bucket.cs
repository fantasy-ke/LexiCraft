using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BuildingBlocks.OSS.Models
{
    public class Bucket
    {

        /// <summary>
        /// Bucket location getter/setter
        /// </summary>
        public string Location { get; internal set; }

        /// <summary>
        /// Bucket name getter/setter
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Bucket <see cref="Owner" /> getter/setter
        /// </summary>
        public Owner Owner { get; internal set; }

        private DateTime _creationDate = DateTime.MinValue;

        /// <summary>
        /// Bucket creation time getter/setter
        /// </summary>
        public string CreationDate
        {
            get => _creationDate.ToString("yyyy-MM-dd HH:mm:ss");
            internal set
            {
                if (DateTime.TryParse(value, out DateTime dt))
                {
                    _creationDate = dt;
                }
                _creationDate = DateTime.MinValue;
            }
        }
    }
}
