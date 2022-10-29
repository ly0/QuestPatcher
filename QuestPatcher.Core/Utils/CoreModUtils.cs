using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace QuestPatcher.Core.Utils;

public class CoreModUtils
{
    public static readonly CoreModUtils Instance = new CoreModUtils();
    
    private const string BeatSaberCoreModsUrl = @"https://beatmods.wgzeyu.com/github/BMBFresources/com.beatgames.beatsaber/core-mods.json";
    public const string BeatSaberPackageID = @"com.beatgames.beatsaber";
    private readonly HttpClient _client = new();
    private CoreModUtils()
    {
        
    }

    public string PackageId
    {
        get => _coreModPackageId;
        set
        {
            if(value != _coreModPackageId)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _coreMods = null;
                _coreModPackageId = value;
            }
        }
    }

    private JObject? _coreMods;
    private string _coreModPackageId = "";

    private CancellationTokenSource? _cancellationTokenSource;
    
    public async Task RefreshCoreMods()
    {
        Log.Information("Refreshing Core Mods");
        
        // Cancel a previous refresh attempt in case it is not finished
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        if(PackageId == BeatSaberPackageID)
        {
            try
            {
                using var res = await _client.GetAsync(BeatSaberCoreModsUrl, _cancellationTokenSource.Token);
                res.EnsureSuccessStatusCode();
                _coreMods = JObject.Parse(await res.Content.ReadAsStringAsync());
            }
            catch(Exception e)
            {
                Log.Error(e, "Cannot fetch core mods");
                // we don't want to overwrite what we previously have 
            }
        }
        else
        {
            Log.Warning("There are no core mods known for this game");
            // currently there are only core mods organized for Beat Saber.
            _coreMods = new JObject();
        }
    }

    public List<JToken> GetCoreMods(string packageVersion)
    {
        try
        {
            return _coreMods?[packageVersion]?["mods"]?.ToList() ?? new List<JToken>();
        }
        catch(Exception e)
        {
            Log.Error(e, "Unexpected Error while finding core mods for {}",packageVersion);
            return new List<JToken>();
        }
    }

}
