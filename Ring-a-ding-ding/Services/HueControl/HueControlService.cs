using HueApi;
using HueApi.Models.Requests;
using HueApi.ColorConverters;
using System.Text.Json;
using HueApi.ColorConverters.Original.Extensions;

namespace Ring_a_ding_ding.Services.HueControl
{
    public class HueControlService
    {
        private readonly ILogger<HueControlService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HueControlServiceConfiguration _hueControlServiceConfiguration = new HueControlServiceConfiguration();
        private readonly HttpClient _httpClient;
        private LocalHueApi? _localHueApi = null;
        public bool Connected { get; private set; } = false;
        public HueControlService(ILogger<HueControlService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _configuration.Bind("HueControl", _hueControlServiceConfiguration);
            _httpClient = httpClient;
            if (string.IsNullOrEmpty(_hueControlServiceConfiguration.BridgeIP))
            {
                _logger.LogError("HueControlServiceConfiguration.BridgeIP is null or empty.");
            }
            if (string.IsNullOrEmpty(_hueControlServiceConfiguration.BridgeKey))
            {
                _logger.LogError("HueControlServiceConfiguration.BridgeKey is null or empty. You should register first.");
            }
        }
        public async Task<(bool, string?)> TryRegisterHueHub()
        {
            if (string.IsNullOrEmpty(_hueControlServiceConfiguration.BridgeIP))
            {
                _logger.LogError("HueControlServiceConfiguration.BridgeIP is null or empty.");
                return (false, "HueControlServiceConfiguration.BridgeIP is null or empty.");
            }

            try
            {
                var appKey = await LocalHueApi.RegisterAsync(_hueControlServiceConfiguration.BridgeIP, _hueControlServiceConfiguration.AppName, Environment.MachineName);
                if (appKey == null)
                {
                    _logger.LogError("Failed To Register Hue Hub.");
                    return (false, "Failed To Register Hue Hub.");
                }
                else
                {
                    if (appKey.Username == null)
                    {
                        _logger.LogError("Failed to get Key from Hue Hub.");
                        return (false, "Failed to get Key from Hue Hub.");
                    }
                    _hueControlServiceConfiguration.BridgeKey = appKey.Username;
                    // Write the HueControl section to huecontrol.json using System.Text.Json
                    var appSettings = new Dictionary<string, HueControlServiceConfiguration>();
                    appSettings.Add("HueControl", _hueControlServiceConfiguration);
                    var appSettingsJson = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText("huecontrol.json", appSettingsJson);
                    return (true, null);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register with Hue Hub");
                return (false, "Failed to register with Hue Hub");
            }
        }
        public async Task<(bool, string?)> TryConnectHueHub()
        {
            if (string.IsNullOrEmpty(_hueControlServiceConfiguration.BridgeIP))
            {
                _logger.LogError("HueControlServiceConfiguration.BridgeIP is null or empty");
                return (false, "HueControlServiceConfiguration.BridgeIP is null or empty");
            }
            if (string.IsNullOrEmpty(_hueControlServiceConfiguration.BridgeKey))
            {
                _logger.LogError("HueControlServiceConfiguration.BridgeKey is null or empty. You should register first.");
                return (false, "HueControlServiceConfiguration.BridgeKey is null or empty. You should register first.");
            }
            try
            {
                _localHueApi = new LocalHueApi(_hueControlServiceConfiguration.BridgeIP, _hueControlServiceConfiguration.BridgeKey);
                var lights = await _localHueApi.GetLightsAsync(); // Just to test that its working.
                _logger.LogInformation("Connected to Hue Hub");
                Connected = true;
                return (true, null);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Hue Hub");
                return (false, "Failed to connect to Hue Hub");
            }
        }
        // Get the list of rooms from the Hue Hub
        public async Task<Dictionary<string, Guid>> GetRoomIdPairs()
        {
            if (_localHueApi == null)
            {
                throw new Exception("Hue Hub is not connected.");
            }
            else
            {
                var roomsResp = await _localHueApi.GetRoomsAsync();
                if (roomsResp.HasErrors)
                {
                    throw new Exception($"Error(s) getting rooms: {string.Join(',', roomsResp.Errors)}");
                }
                var roomIdPairs = new Dictionary<string, Guid>();
                foreach (var room in roomsResp.Data)
                {
                    if (room.Metadata != null)
                        roomIdPairs.Add(room.Metadata.Name, room.Id);
                }
                return roomIdPairs;
            }
        }

        public async Task SetNewRoomId(Guid id)
        {
            if (_localHueApi == null)
            {
                throw new Exception("Hue Hub is not connected.");
            }
            else
            {
                var roomsResp = await _localHueApi.GetRoomsAsync();
                if (roomsResp.HasErrors)
                {
                    throw new Exception($"Error(s) getting rooms: {string.Join(',', roomsResp.Errors)}");
                }
                var room = roomsResp.Data.FirstOrDefault(r => r.Id == id);
                if (room == null)
                {
                    throw new Exception($"Room with id {id} not found.");
                }
                else
                {
                    _hueControlServiceConfiguration.LivingRoomId = id.ToString();
                    // Write the HueControl section to huecontrol.json using System.Text.Json
                    var appSettings = new Dictionary<string, HueControlServiceConfiguration>();
                    appSettings.Add("HueControl", _hueControlServiceConfiguration);
                    var appSettingsJson = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText("huecontrol.json", appSettingsJson);
                }
            }
        }

        // Set lights to 15% power green
        public async Task GoblinModeLighting()
        {
            if (_localHueApi == null)
            {
                _logger.LogError("Hue Hub is not connected.");
                throw new Exception("Hue Hub is not connected.");
            }
            else
            {
                var roomsResp = await _localHueApi.GetRoomsAsync();
                if (roomsResp.HasErrors)
                {
                    _logger.LogError($"Error(s) getting rooms: {string.Join(',', roomsResp.Errors.Select(e => e.Description))}");
                    throw new Exception($"Error(s) getting rooms: {string.Join(',', roomsResp.Errors.Select(e => e.Description))}");
                }
                var livingGuid = new Guid(_hueControlServiceConfiguration.LivingRoomId);
                var room = roomsResp.Data.FirstOrDefault(r => r.Id == livingGuid);
                if (room == null)
                {
                    _logger.LogError($"Room with id {_hueControlServiceConfiguration.LivingRoomId} not found.");
                    throw new Exception($"Room with id {_hueControlServiceConfiguration.LivingRoomId} not found.");
                }
                var request = new UpdateLight()
                    .TurnOn()
                    .SetColor(new RGBColor("00ff2a"))
                    .SetBrightness(50d);
                // get all lights in the room
                var lightResourceIdentifiers = room.Children;//.Where(rid => rid.Rtype == "light");
                foreach (var lightDeviceResourceIdentifier in lightResourceIdentifiers)
                {
                    try
                    {
                        // Get light from device
                        var device = (await _localHueApi.GetDeviceAsync(lightDeviceResourceIdentifier.Rid)).Data.First();
                        var lightId = device?.Services?.First(s => s.Rtype == "light")?.Rid;
                        if (lightId != null)
                        {
                            _logger.LogInformation($"Updating Light {lightId}.");
                            var result = await _localHueApi.UpdateLightAsync((Guid)lightId, request);
                            if (result.HasErrors)
                            {
                                _logger.LogInformation($"{lightId}: {string.Join(',', result.Errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            _logger.LogError($"Failed to get light ID for device {lightDeviceResourceIdentifier.Rid}");
                        }
                    }
                    catch
                    {
                        //Nothing :)
                    }
                }
            }
        }
    }
}
