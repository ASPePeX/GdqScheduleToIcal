using CommandLine;
using CommunityToolkit.Diagnostics;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using VintedImagegrabber;

namespace GdqScheduleToIcal
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var gsti = new GSTI(args);
            gsti.Run();
        }

        class GSTI
        {
            public class Options
            {
                [Option('u', "scheduleurl", Required = true, HelpText = "GDQ Schedule URL")]
                public string? ScheduleUrl { get; set; }
            }

            readonly ParserResult<Options> options;
            readonly HttpHelper httpHelper;
            readonly MD5 md5;

            public GSTI(string[] args)
            {
                options = Parser.Default.ParseArguments<Options>(args);
                httpHelper = new HttpHelper();
                md5= MD5.Create();
            }

            public void Run()
            {
                options?.WithParsed(o =>
                {
                    if (!string.IsNullOrWhiteSpace(o.ScheduleUrl))
                    {
                        var schedulePageSource = HttpUtility.HtmlDecode(httpHelper.GetUrl(o.ScheduleUrl).Result);
                        //var schedulePageSource = HttpUtility.HtmlDecode(File.ReadAllText("schedulePageSource.html"));

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

                        schedulePageSource = Regex.Replace(schedulePageSource, @"^\s", @"^");
                        schedulePageSource = Regex.Replace(schedulePageSource, @"\s$", @"$");


                        var calendar = ParseSchedulePage(schedulePageSource);

                        calendar.Name = "AGDQ 2023";

                        var serializer = new CalendarSerializer();
                        var serializedCalendar = serializer.SerializeToString(calendar);

                        serializedCalendar = Regex.Replace(serializedCalendar, @"^DTSTAMP:.*?$\n", "");

#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'

                        Directory.CreateDirectory("docs");
                        File.WriteAllText("docs/agdq2023.ical", serializedCalendar);
                    }
                    else
                    {
                        Console.WriteLine("ERROR: 1");
                    }
                });
            }

            private Calendar ParseSchedulePage(string schedulePageSource)
            {
                // snipping out the whole schedule using <tbody>

                string patternSchedule = @"<tbody>(.*?)</tbody>";

                MatchCollection matchListSchedule = Regex.Matches(schedulePageSource, patternSchedule, RegexOptions.Singleline);

                var calendar = new Calendar();

                calendar.AddTimeZone(TimeZoneInfo.Utc);

                if (matchListSchedule.Count > 0 && matchListSchedule[0].Groups.Count > 1)
                {
                    var scheduleSource = matchListSchedule[0].Groups[1].Value;

                    string patternEvent = @"<tr>\s*?<td class=""start-time text-right"">(.*?)</td>\s*?<td>(.*?)</td>\s*?<td>(.*?)</td>\s*?<td rowspan=""2"" class=""visible-lg text-center"">.*?(\d{1,2}:\d{2}:\d{2}).*?</td>\s*?</tr>\s*?<tr class=""second-row "">\s*?<td class=""text-right "">.*?(\d{1,2}:\d{2}:\d{2}).*?</td>\s*?<td>(.*?)</td>\s*?<td><i class=""fa fa-microphone""></i> (.*?)</td>";

                    MatchCollection matchListEvents = Regex.Matches(scheduleSource, patternEvent, RegexOptions.Singleline, new TimeSpan(0, 1, 0));

                    foreach (Match match in matchListEvents.Cast<Match>())
                    {
                        if (match != null && match.Groups.Count == 8)
                        {
                            var dtStart = new CalDateTime(DateTime.Parse(match.Groups[1].Value).ToUniversalTime());
                            var game = match.Groups[2].Value;
                            var runner = match.Groups[3].Value;
                            var setupRaw = match.Groups[4].Value;
                            var durationRaw = match.Groups[5].Value.ToString().Split(':');
                            var rundesc = match.Groups[6].Value;
                            var host = match.Groups[7].Value;

                            //if (game.Contains("Dust"))
                            //{ }

                            if (durationRaw.Length != 3)
                            { ThrowHelper.ThrowArgumentOutOfRangeException(); }

                            var dtEnd = new CalDateTime(dtStart).AddHours(int.Parse(durationRaw[0])).AddMinutes(int.Parse(durationRaw[1])).AddSeconds(int.Parse(durationRaw[2]));

                            var run = new CalendarEvent
                            {
                                Start = dtStart,
                                End = dtEnd,
                                Summary = game,
                                Location = @"https://www.twitch.tv/gamesdonequick",
                                Description = $"Runner: {runner}\nRun: {rundesc}\nHost: {host}\nSetup Lenght: {setupRaw}",
                                Uid = Guid.Parse(ToHex(md5.ComputeHash(Encoding.UTF8.GetBytes(game + runner)), false)).ToString()
                            };

                            //if (run.Summary == "Jak II")
                            //{ }

                            calendar.Events.Add(run);
                        }
                        else
                        {
                            Console.WriteLine("Warning: match is null or doesn't have all arguments!");
                        }
                    }
                }

                return calendar;
            }

            private static string ToHex(byte[] bytes, bool upperCase)
            {
                StringBuilder result = new(bytes.Length * 2);

                for (int i = 0; i < bytes.Length; i++)
                    result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

                return result.ToString();
            }
        }
    }
}