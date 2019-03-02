using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using CuteAnt.Pool;
using CuteAnt.Reflection;
using CuteAnt.Text;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    /// <summary>Methods for dealing with connection strings.</summary>
    public class ConnectionString
    {
        private static readonly Dictionary<Type, Func<string, object>> s_translators;

        static ConnectionString()
        {
            Digits = "0123456789".ToCharArray();
            ByteSizes = new ByteSize[]
            {
                new ByteSize { Factor = 1024L * 1024L * 1024L * 1024L * 1024 * 1024L, Suffixes = new string[] { "E", "e", "Ei", "EiB", "exbibyte", "exbibytes" } },
                new ByteSize { Factor = 1000L * 1000L * 1000L * 1000L * 1000L * 1000L, Suffixes = new string[] { "EB", "exabyte", "exabytes" } },
                new ByteSize { Factor = 1024L * 1024L * 1024L * 1024L * 1024L, Suffixes = new string[] { "P", "p", "Pi", "PiB", "pebibyte", "pebibytes" } },
                new ByteSize { Factor = 1000L * 1000L * 1000L * 1000L * 1000L, Suffixes = new string[] { "PB", "petabyte", "petabytes" } },
                new ByteSize { Factor = 1024L * 1024L * 1024L * 1024L, Suffixes = new string[] { "T", "t", "Ti", "TiB", "tebibyte", "tebibytes" } },
                new ByteSize { Factor = 1000L * 1000L * 1000L * 1000L, Suffixes = new string[] { "TB", "terabyte", "terabytes" } },
                new ByteSize { Factor = 1024L * 1024L * 1024L, Suffixes = new string[] { "G", "g", "Gi", "GiB", "gibibyte", "gibibytes" } },
                new ByteSize { Factor = 1000L * 1000L * 1000L, Suffixes = new string[] { "GB", "gigabyte", "gigabytes" } },
                new ByteSize { Factor = 1024L * 1024L, Suffixes = new string[] { "M", "m", "Mi", "MiB", "mebibyte", "mebibytes" } },
                new ByteSize { Factor = 1000L * 1000L, Suffixes = new string[] { "MB", "megabyte", "megabytes" } },
                new ByteSize { Factor = 1024L, Suffixes = new string[] { "K", "k", "Ki", "KiB", "kibibyte", "kibibytes" } },
                new ByteSize { Factor = 1000L, Suffixes = new string[] { "KB", "kilobyte", "kilobytes" } },
                new ByteSize { Factor = 1, Suffixes = new string[] { "b", "B", "byte", "bytes" } }
            };

            s_translators = new Dictionary<Type, Func<string, object>>()
            {
                { typeof(int), x => ToNullableInt(GetByteSize(x)) ?? 0 },
                { typeof(int?), x => ToNullableInt(GetByteSize(x)) },
                { typeof(decimal), x => decimal.Parse(x, CultureInfo.InvariantCulture) },
                { typeof(string), x => x },
                { typeof(bool), x => ToBoolean(x) },
                { typeof(long), x => GetByteSize(x) ?? 0L },
                { typeof(byte), x => byte.Parse(x, CultureInfo.InvariantCulture) },
                { typeof(double), x => double.Parse(x, CultureInfo.InvariantCulture) },
                { typeof(float), x => float.Parse(x, CultureInfo.InvariantCulture) },
                { typeof(TimeSpan), x => GetTimeSpan(x, false) },
                { typeof(GossipSeed[]), x => x.Split(',').Select(q =>
                    {
                        try
                        {
                            var pieces = q.Trim().Split(':');
                            if (pieces.Length != 2) throw new Exception("Could not split IP address from port.");

                            return new GossipSeed(new IPEndPoint(IPAddress.Parse(pieces[0]), int.Parse(pieces[1])));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Gossip seed {q} is not in correct format", ex);
                        }
                    }).ToArray()
                },
                { typeof(UserCredentials), x =>
                    {
                        try
                        {
                            var pieces = x.Trim().Split(':');
                            if (pieces.Length != 2) throw new Exception("Could not split into username and password.");

                            return new UserCredentials(pieces[0], pieces[1]);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"User credentials {x} is not in correct format. Expected format is username:password.", ex);
                        }
                    }
                },
                { typeof(IEventAdapter), x =>
                    {
                        try
                        {
                            var type = TypeUtils.ResolveType(x);
                            var instance = ActivatorUtils.FastCreateInstance(type);
                            if (instance is IEventAdapter adapter) { return adapter; }
                            if (instance is IEventAdapterProvider adapterProvider) { return adapterProvider.Create(); }

                            throw new Exception($"EventAdapter not found for type {x}");;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Unable to find a type named {x}", ex);
                        }
                    }
                }
            };
        }

        /// <summary>Parses a connection string into its pieces represented as kv pairs.</summary>
        /// <param name="connectionString">the connection string to parse</param>
        /// <returns></returns>
        internal static IEnumerable<KeyValuePair<string, string>> GetConnectionStringInfo(string connectionString)
        {
            var builder = new DbConnectionStringBuilder() { ConnectionString = connectionString };
            //can someome mutate this builder before the enumerable is closed sure but thats the fun!
            return from object key in builder.Keys
                   select new KeyValuePair<string, string>(key.ToString(), builder[key.ToString()].ToString());
        }

        /// <summary>Returns a <see cref="ConnectionSettings"></see> for a given connection string.</summary>
        /// <param name="connectionString">The connection string to parse</param>
        /// <param name="builder">Pre-populated settings builder, optional. If not specified, a new builder will be created.</param>
        /// <returns>a <see cref="ConnectionSettings"/> from the connection string</returns>
        public static ConnectionSettings GetConnectionSettings(string connectionString, ConnectionSettingsBuilder builder = null)
        {
            var settings = (builder ?? ConnectionSettings.Create()).Build();
            var items = GetConnectionStringInfo(connectionString).ToArray();
            return Apply(items, settings);
        }

        private static string WithSpaces(string name)
        {
            var nameWithSpaces = StringBuilderManager.Allocate();
            nameWithSpaces.Append(name[0]);

            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    nameWithSpaces.Append(' ');
                }
                nameWithSpaces.Append(c);
            }

            return StringBuilderManager.ReturnAndFree(nameWithSpaces);
        }

        /// <summary>MyProperty -> my_property</summary>
        private static string ToSnakeCase(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            var sb = StringBuilderCache.Acquire();
            var state = SnakeCaseState.Start;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ' ')
                {
                    if (state != SnakeCaseState.Start)
                    {
                        state = SnakeCaseState.NewWord;
                    }
                }
                else if (char.IsUpper(s[i]))
                {
                    switch (state)
                    {
                        case SnakeCaseState.Upper:
                            bool hasNext = (i + 1 < s.Length);
                            if (i > 0 && hasNext)
                            {
                                char nextChar = s[i + 1];
                                if (!char.IsUpper(nextChar) && nextChar != '_')
                                {
                                    sb.Append('_');
                                }
                            }
                            break;
                        case SnakeCaseState.Lower:
                        case SnakeCaseState.NewWord:
                            sb.Append('_');
                            break;
                    }

                    var c = char.ToLower(s[i], CultureInfo.InvariantCulture);
                    sb.Append(c);

                    state = SnakeCaseState.Upper;
                }
                else if (s[i] == '_')
                {
                    sb.Append('_');
                    state = SnakeCaseState.Start;
                }
                else
                {
                    if (state == SnakeCaseState.NewWord)
                    {
                        sb.Append('_');
                    }

                    sb.Append(s[i]);
                    state = SnakeCaseState.Lower;
                }
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private enum SnakeCaseState
        {
            Start,
            Lower,
            Upper,
            NewWord
        }

        private static T Apply<T>(IEnumerable<KeyValuePair<string, string>> items, T obj)
        {
            var typeFields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);

            var fields = typeFields.Select(x => new Tuple<string, FieldInfo>(x.Name, x))
                .Concat(typeFields.Select(x => new Tuple<string, FieldInfo>(WithSpaces(x.Name), x)))
                .Concat(typeFields.Select(x => new Tuple<string, FieldInfo>(ToSnakeCase(x.Name), x)))
                .GroupBy(x => x.Item1)
                .ToDictionary(x => x.First().Item1, x => x.First().Item2, StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (!fields.TryGetValue(item.Key, out FieldInfo fi)) continue;
                if (!s_translators.TryGetValue(fi.FieldType, out Func<string, object> func))
                {
                    throw new Exception(string.Format("Can not map field named {0} as type {1} has no translator", item, fi.FieldType.Name));
                }
                fi.SetValue(obj, func(item.Value));
            }
            return obj;
        }

        #region ** ToBoolean **

        private static bool ToBoolean(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) { return false; }
            switch (str.ToLowerInvariant())
            {
                case "1":
                case "t":
                case "on":
                case "true":
                    return true;

                case "0":
                case "f":
                case "false":
                case "off":
                default:
                    return false;
            }
        }

        #endregion

        #region ** ToNullableInt **

        private static int? ToNullableInt(long? value) => value.HasValue && value.Value > 0L ? (int?)value.Value : null;

        #endregion

        #region ** GetTimeSpan **

        private static readonly Regex TimeSpanRegex =
            new Regex(@"^(?<value>([0-9]+(\.[0-9]+)?))\s*(?<unit>(nanoseconds|nanosecond|nanos|nano|ns|microseconds|microsecond|micros|micro|us|milliseconds|millisecond|millis|milli|ms|seconds|second|s|minutes|minute|m|hours|hour|h|days|day|d))$",
                RegexOptions.Compiled);

        private static TimeSpan GetTimeSpan(string res, bool allowInfinite = true)
        {
            var match = TimeSpanRegex.Match(res);
            if (match.Success)
            {
                var u = match.Groups["unit"].Value;
                var v = ParsePositiveValue(match.Groups["value"].Value);

                switch (u)
                {
                    case "nanoseconds":
                    case "nanosecond":
                    case "nanos":
                    case "nano":
                    case "ns":
                        return TimeSpan.FromTicks((long)Math.Round(TimeSpan.TicksPerMillisecond * v / 1000000.0));
                    case "microseconds":
                    case "microsecond":
                    case "micros":
                    case "micro":
                        return TimeSpan.FromTicks((long)Math.Round(TimeSpan.TicksPerMillisecond * v / 1000.0));
                    case "milliseconds":
                    case "millisecond":
                    case "millis":
                    case "milli":
                    case "ms":
                        return TimeSpan.FromMilliseconds(v);
                    case "seconds":
                    case "second":
                    case "s":
                        return TimeSpan.FromSeconds(v);
                    case "minutes":
                    case "minute":
                    case "m":
                        return TimeSpan.FromMinutes(v);
                    case "hours":
                    case "hour":
                    case "h":
                        return TimeSpan.FromHours(v);
                    case "days":
                    case "day":
                    case "d":
                        return TimeSpan.FromDays(v);
                }
            }

            if (allowInfinite && string.Equals("infinite", res, StringComparison.OrdinalIgnoreCase))  //Not in Hocon spec
            {
                return Timeout.InfiniteTimeSpan;
            }

            return TimeSpan.FromMilliseconds(ParsePositiveValue(res));
        }

        private static double ParsePositiveValue(string v)
        {
            var value = double.Parse(v, NumberFormatInfo.InvariantInfo);
            if (value < 0)
            {
                throw new FormatException($"Expected a positive value instead of {value}");
            }
            return value;
        }

        #endregion

        #region ** GetByteSize **

        private struct ByteSize
        {
            public long Factor { get; set; }
            public string[] Suffixes { get; set; }
        }

        private static ByteSize[] ByteSizes { get; }

        private static char[] Digits { get; }

        private static long? GetByteSize(string res)
        {
            if (string.IsNullOrWhiteSpace(res)) { return null; }
            res = res.Trim();
            var index = res.LastIndexOfAny(Digits);
            if (index == -1 || index + 1 >= res.Length) { return long.Parse(res); }

            var value = res.Substring(0, index + 1);
            var unit = res.Substring(index + 1).Trim();

            for (var byteSizeIndex = 0; byteSizeIndex < ByteSizes.Length; byteSizeIndex++)
            {
                var byteSize = ByteSizes[byteSizeIndex];
                for (var suffixIndex = 0; suffixIndex < byteSize.Suffixes.Length; suffixIndex++)
                {
                    var suffix = byteSize.Suffixes[suffixIndex];
                    if (string.Equals(unit, suffix, StringComparison.Ordinal))
                    {
                        return byteSize.Factor * long.Parse(value);
                    }
                }
            }

            throw new FormatException($"{unit} is not a valid byte size suffix");
        }

        #endregion
    }
}
