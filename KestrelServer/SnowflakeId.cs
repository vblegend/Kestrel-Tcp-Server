using System;

namespace KestrelServer
{
    public readonly struct SnowflakeId : IEquatable<SnowflakeId>
    {
        private readonly Int64 m_value;

        public static Func<DateTime> UtcNowFunc = () => DateTime.UtcNow;

        public SnowflakeId()
        {
            this.m_value = 0;
        }
        private SnowflakeId(Int64 value)
        {
            this.m_value = value;
        }

        public override string ToString()
        {
            return this.m_value.ToString("X").PadLeft(16, '0').ToUpper();
        }

        public Boolean IsEmpty
        {
            get
            {
                return this.m_value == 0;
            }
        }

        public Int32 Timestamp
        {
            get
            {
                return (Int32)((this.m_value >> 16) & 0xFFFFFFFFFFFFF);
            }
        }

        public DateTime UTCTime
        {
            get
            {
                return DateTime.UnixEpoch.AddMilliseconds((this.m_value >> 16) & 0xFFFFFFFFFFFFF);
            }
        }

        public DateTime LocalTime
        {
            get
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UnixEpoch.AddMilliseconds((this.m_value >> 16) & 0xFFFFFFFFFFFFF), TimeZoneInfo.Local);
            }
        }

        public UInt16 Sequence
        {
            get
            {
                return (UInt16)(this.m_value & 0xFFF);
            }
        }


        public Int64 Value
        {
            get
            {
                return this.m_value;
            }
        }

        public static bool operator ==(SnowflakeId s1, SnowflakeId s2)
        {
            return s1.m_value == s2.m_value;
        }


        public static bool operator !=(SnowflakeId s1, SnowflakeId s2)
        {
            return s1.m_value != s2.m_value;
        }

        public bool Equals(SnowflakeId other)
        {
            return other.m_value == this.m_value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)((long)m_value)) ^ (int)(m_value >> 32);
        }

        public override bool Equals(object obj)
        {
            if (obj is SnowflakeId id)
            {
                return id.m_value == this.m_value;
            }
            return false;
        }

        public static implicit operator SnowflakeId(String value)
        {
            long result = Convert.ToInt64(value, 16);
            return new SnowflakeId(result);
        }

        public static implicit operator SnowflakeId(Int64 value)
        {
            return new SnowflakeId(value);
        }


        #region Static 


        private static readonly DateTime STARTDATE_DEFINE = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static long _lastTimestamp = -1L;
        private static long _sequence = 1L;
        private static readonly object _lock = new object();


        public static SnowflakeId Generate()
        {
            lock (_lock)
            {
                var timestamp = GetCurrentTimestamp();
                if (_lastTimestamp == timestamp)
                {
                    _sequence = (_sequence + 1) & 0xFFF;
                    if (_sequence == 0L)
                    {
                        timestamp = WaitNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    _sequence = 1L;
                }
                _lastTimestamp = timestamp;
                long id = (timestamp << 12) | _sequence;
                return new SnowflakeId(id);
            }
        }


        private static long GetCurrentTimestamp()
        {
            return (long)(UtcNowFunc() - STARTDATE_DEFINE).TotalMilliseconds;
        }

        private static long WaitNextMillis(long lastTimestamp)
        {
            var timestamp = GetCurrentTimestamp();
            while (timestamp <= lastTimestamp)
            {
                timestamp = GetCurrentTimestamp();
            }
            return timestamp;
        }

        #endregion

    }
}
