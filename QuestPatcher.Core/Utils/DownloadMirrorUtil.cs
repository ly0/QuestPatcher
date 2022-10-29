using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace QuestPatcher.Core.Utils;

public class DownloadMirrorUtil
{
    public static readonly DownloadMirrorUtil Instance = new ();
    
    private const string MirrorUrl = @"https://bs.wgzeyu.com/localization/mods.json";

    private long _lastRefreshTime = 0;
    
    private readonly HttpClient _client = new();
    
    private readonly Dictionary<string, string> _mirrorUrls = new ();
    private string _coreModPackageId = "";

    private CancellationTokenSource? _cancellationTokenSource;
    
    private DownloadMirrorUtil()
    {
        
    }

    public async Task Refresh()
    {
        Log.Information("Refreshing download mirror URL ");
        
        // Cancel a previous refresh attempt in case it is not finished
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            using var res = await _client.GetAsync(MirrorUrl, _cancellationTokenSource.Token);
            res.EnsureSuccessStatusCode();

            var jObject = JObject.Parse(await res.Content.ReadAsStringAsync());
            _mirrorUrls.Clear();

            foreach (var pair in jObject)
            {
                var mirror = pair.Value?["mirrorUrl"]?.ToString();
                if (mirror != null)
                {
                    _mirrorUrls.Add(pair.Key, mirror);
                }
            }

            _lastRefreshTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        catch(Exception e)
        {
            Log.Error(e, "Cannot fetch mirror download url");
            // we don't want to overwrite what we previously have 
        }
    }
    
    public async Task<string> GetMirrorUrl(string original)
    {
        if (DateTimeOffset.Now.ToUnixTimeSeconds() - _lastRefreshTime > 300)
        {
            await Refresh();
        }

        if (_mirrorUrls.ContainsKey(original))
        {
            Log.Information($"Mirror Url found: {_mirrorUrls[original]}");
            return _mirrorUrls[original];
        }
        Log.Warning($"Mirror Url not found for {original}");
        return original;
    }

}