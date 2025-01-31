using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Create RDBMS Column with specified decimal scale & precision
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DecimalLengthAttribute : AttributeBase
    {
        public int Precision { get; set; }
        public int Scale { get; set; }

        public DecimalLengthAttribute(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
        }

        public DecimalLengthAttribute(int precision)
            : this(precision, 0)
        {
        }

        public DecimalLengthAttribute()
            : this(18, 0)
        {
        }
    }
}
