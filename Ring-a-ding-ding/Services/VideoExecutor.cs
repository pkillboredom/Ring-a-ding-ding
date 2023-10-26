using Microsoft.Extensions.Logging;
using Ring_a_ding_ding.Services.HueControl;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace Ring_a_ding_ding.Services
{
    public class VideoExecutor
    {
        private readonly ILogger<VideoExecutor> logger;
        private readonly HttpClient client;
        private readonly Uri mpcHcUri = new Uri("http://127.0.0.1:13579/info.html");
        private readonly HueControlService _hueControlService;

        public VideoExecutor(ILogger<VideoExecutor> logger, HttpClient client, HueControlService hueControlService)
        {
            this.logger = logger;
            this.client = client;
            _hueControlService = hueControlService;
        }

        public async Task RunVideoScript(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Launch sequence initiated!");
            // Wait ten seconds and log the countdown every second.
            for (int i = 10; i > 0; i--)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Cancellation requested. Aborting video script...");
                    return;
                }
                logger.LogInformation("T minus {0} seconds", i);
                await Task.Delay(1000, cancellationToken);
            }
            //logger.LogInformation("Killing Discord...");
            //await KillDiscord(cancellationToken);
            // Launch MPC-HC fullscreen, playing "F:\Goblin Film\Final_Party_Edition.mp4"
            logger.LogInformation("Launching MPC-HC...");
            // dont await
            _ = LaunchMPCHCWithVideo(@"F:\Goblin Film\Final_Party_Edition.mp4");
            // Wait 0.5 seconds to ensure MPC-HC is running, for best-effort seamlessness.
            await Task.Delay(500, cancellationToken);
            logger.LogInformation("Killing GrabTheGoblins...");
            await KillGrabTheGoblins(cancellationToken);
            await Task.Delay(2000, cancellationToken);
            // Wait until at least 1:03 into the video, then ring phone.
            logger.LogInformation("Waiting for 0:26 in video to hit lights...");
            logger.LogInformation("Waiting for 1:03 in video to ring phone...");
            bool RingTimeHit = false;
            bool LightTimeHit = false;
            while (!RingTimeHit || !LightTimeHit)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Cancellation requested. Aborting video script...");
                    return;
                }
                //logger.LogInformation($"Loop Start Time: {DateTime.Now}");
                var currentTime = await GetCurrentTime();
                //logger.LogInformation($"Return Time: {DateTime.Now}");
                if (!RingTimeHit && currentTime >= TimeSpan.Parse("00:01:03"))
                {
                    RingTimeHit = true;
                    logger.LogInformation("Ringing phone...");
                    CallService.RingHandsfreeDevice();
                }
                if (!LightTimeHit && currentTime >= TimeSpan.Parse("00:00:26"))
                {
                    LightTimeHit = true;
                    logger.LogInformation("Hitting Lights...");
                    _ = _hueControlService.GoblinModeLighting();
                }
                await Task.Delay(500, cancellationToken);
                //logger.LogInformation($"Delayed Time: {DateTime.Now}");
            }
        }

        public async Task KillDiscord(CancellationToken cancellationToken = default)
        {
            // Stop Discord Process
            var processes = Process.GetProcessesByName("Discord");
            foreach (var process in processes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Cancellation requested. Aborting video script...");
                    return;
                }
                process.Kill();
            }
        }

        public async Task KillGrabTheGoblins(CancellationToken cancellationToken = default)
        {
            // Stop GrabTheGoblins Process
            var processes = Process.GetProcessesByName("GrabTheGoblins");
            foreach (var process in processes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Cancellation requested. Aborting video script...");
                    return;
                }
                process.Kill();
            }
        }

        public async Task LaunchMPCHCWithVideo(string videoPath, CancellationToken cancellationToken = default)
        {
            Process.Start("C:\\Program Files\\MPC-HC\\mpc-hc64.exe", $"/play /viewpreset 1 /fullscreen /volume 100 \"{videoPath}\"");
        }

        public async Task<TimeSpan> GetCurrentTime()
        {
            // GET http://localhost:13579/info.html
            // Parse the response for the current time
            // Pattern is "« MPC-HC [VersionString] • [FileName] • [CurrentHours]:[CurrentMinutes]:[CurrentSeconds]/[TotalHours]:[TotalMinutes]:[TotalSeconds] • FileSize »"
            // Example: « MPC-HC v1.9.24.0 • Final_Party_Edition.mp4 • 00:00:42/00:01:55 • 631 MB »
            // Text is found in <p id="mpchc_np"> tag.

            //logger.LogInformation($"PreGET Time: {DateTime.Now}");
            var response = await client.GetAsync(mpcHcUri);
            var responseString = await response.Content.ReadAsStringAsync();
            //logger.LogInformation($"GET Time: {DateTime.Now}");
            var mpchc_npContents = Regex.Match(responseString, @"<p id=""mpchc_np"">(.*)</p>").Groups[1].Value;
            //logger.LogInformation($"<p> Time: {DateTime.Now}");
            var mpchc_npContentsHtmlDecoded = WebUtility.HtmlDecode(mpchc_npContents);
            var matchPattern = @"(\d+):(\d+):(\d+)/(\d+):(\d+):(\d+)";
            var matchMatch = Regex.Match(mpchc_npContentsHtmlDecoded, matchPattern);
            //logger.LogInformation($"Time Time: {DateTime.Now}");
            if (matchMatch.Success)
            {
                var currentHours = int.Parse(matchMatch.Groups[1].Value);
                var currentMinutes = int.Parse(matchMatch.Groups[2].Value);
                var currentSeconds = int.Parse(matchMatch.Groups[3].Value);
                var totalHours = int.Parse(matchMatch.Groups[4].Value);
                var totalMinutes = int.Parse(matchMatch.Groups[5].Value);
                var totalSeconds = int.Parse(matchMatch.Groups[6].Value);
                var currentTime = new TimeSpan(currentHours, currentMinutes, currentSeconds);
                var totalTime = new TimeSpan(totalHours, totalMinutes, totalSeconds);
                logger.LogInformation("Current time: {0} / {1}", currentTime, totalTime);
                //logger.LogInformation($"Object Time: {DateTime.Now}");
                return currentTime;
            }
            else
            {
                logger.LogInformation("Could not parse MPC-HC info.html response. MatchMatch.");
                return TimeSpan.Zero;
            }
        }
    }
}
