﻿using Avalonia.Controls;
using QuestPatcher.Models;
using QuestPatcher.Services;
using QuestPatcher.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ReactiveUI;
using QuestPatcher.Core;
using QuestPatcher.Core.Modding;
using QuestPatcher.Core.Models;
using QuestPatcher.Core.Patching;
using Serilog;

namespace QuestPatcher.ViewModels
{
    public class ToolsViewModel : ViewModelBase
    {
      
        public Config Config { get; }

        public ProgressViewModel ProgressView { get; }

        public OperationLocker Locker { get; }
        
        public ThemeManager ThemeManager { get; }

        public string AdbButtonText => _isAdbLogging ? "Stop ADB Log" : "Start ADB Log";

        private bool _isAdbLogging;

        private readonly Window _mainWindow;
        private readonly SpecialFolders _specialFolders;
        private readonly PatchingManager _patchingManager;
        private readonly AndroidDebugBridge _debugBridge;
        private readonly QuestPatcherUiService _uiService;
        private readonly InfoDumper _dumper;
        private readonly BrowseImportManager _browseManager;
        private readonly ModManager _modManager;

        public ToolsViewModel(Config config, ProgressViewModel progressView, OperationLocker locker, 
            Window mainWindow, SpecialFolders specialFolders, PatchingManager patchingManager, 
            AndroidDebugBridge debugBridge, QuestPatcherUiService uiService, InfoDumper dumper, ThemeManager themeManager, 
            BrowseImportManager browseManager, ModManager modManager)
        {
            Config = config;
            ProgressView = progressView;
            Locker = locker;
            ThemeManager = themeManager;
            _browseManager = browseManager;
            _modManager = modManager;

            _mainWindow = mainWindow;
            _specialFolders = specialFolders;
            _patchingManager = patchingManager;
            _debugBridge = debugBridge;
            _uiService = uiService;
            _dumper = dumper;

            _debugBridge.StoppedLogging += (_, _) =>
            {
                Log.Information("ADB log exited");
                _isAdbLogging = false;
                this.RaisePropertyChanged(nameof(AdbButtonText));
            };
        }
        public async void UninstallAndInstall()
        {
            await _browseManager.UninstallAndInstall();
        }

        public async void InstallServerSwitcher()
        {
            await _browseManager.InstallApkFromUrl("https://ganbei-hot-update-1258625969.file.myqcloud.com/questpatcher_mirror/Icey-latest.apk");
        }

        public async void UninstallApp()
        {
            try
            {
                DialogBuilder builder = new()
                {
                    Title = "你确定吗？",
                    Text = "游戏卸载完成后本软件将自动退出。如果你今后又安装了回来，重新打开本软件就可以再次打补丁"
                };
                builder.OkButton.Text = "好的，卸载";
                builder.CancelButton.Text = "算了，我再想想";
                if (await builder.OpenDialogue(_mainWindow))
                {
                    Locker.StartOperation();
                    try
                    {
                        Log.Information("正在卸载 . . .");
                        await _patchingManager.UninstallCurrentApp();
                    }
                    finally
                    {
                        Locker.FinishOperation();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"卸载 {ex} 失败！");
            }
        }

        public async void DeleteAllMods()
        {
            DialogBuilder builder = new()
            {
                Title = "你确定要删除所有Mod吗？此操作不可恢复!",
                Text = "删除后，你可以通过Mod管理页面的“检查核心Mod”按钮来重新安装核心Mod，\n 歌曲、模型等资源不会受到任何影响，在装好版本匹配的Mod之后即可继续使用。"
            };
            builder.OkButton.Text = "好的，删掉";
            builder.CancelButton.Text = "算了，我再想想";
            if (!await builder.OpenDialogue(_mainWindow)) return;
            
            Locker.StartOperation();
            try
            {
                Log.Information("开始删除所有MOD！");
                await _modManager.DeleteAllMods();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "删除所有Mod竟然失败了！");
                await _modManager.SaveMods();
            }
            finally
            {
                Locker.FinishOperation();
            }
        }

        public void OpenLogsFolder()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _specialFolders.LogsFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public async void QuickFix()
        {
            Locker.StartOperation(true); // ADB is not available during a quick fix, as we redownload platform-tools
            try
            {
                await _uiService.QuickFix();
                Log.Information("Done!");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to clear cache: {ex}");
                DialogBuilder builder = new()
                {
                    Title = "Failed to clear cache",
                    Text = "Running the quick fix failed due to an unhandled error",
                    HideCancelButton = true
                };
                builder.WithException(ex);

                await builder.OpenDialogue(_mainWindow);
            }   finally
            {
                Locker.FinishOperation();
            }
        }

        public async void ToggleAdbLog()
        {
            if(_isAdbLogging)
            {
                _debugBridge.StopLogging();
            }
            else
            {
                Log.Information("Starting ADB log");
                await _debugBridge.StartLogging(Path.Combine(_specialFolders.LogsFolder, "adb.log"));

                _isAdbLogging = true;
                this.RaisePropertyChanged(nameof(AdbButtonText));
            }
        }

        public async void CreateDump()
        {
            Locker.StartOperation();
            try
            {
                // Create the dump in the default location (the data directory)
                string dumpLocation = await _dumper.CreateInfoDump();

                string? dumpFolder = Path.GetDirectoryName(dumpLocation);
                if (dumpFolder != null)
                {
                    // Open the dump's directory for convenience
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dumpFolder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            catch (Exception ex)
            {
                // Show a dialog with any errors
                Log.Error($"Failed to create dump: {ex}");
                DialogBuilder builder = new()
                {
                    Title = "Failed to create dump",
                    Text = "Creating the dump failed due to an unhandled error",
                    HideCancelButton = true
                };
                builder.WithException(ex);

                await builder.OpenDialogue(_mainWindow);
            }
            finally
            {
                Locker.FinishOperation();
            }
        }

        public async void ChangeApp()
        {
            await _uiService.OpenChangeAppMenu(false);
        }

        public void OpenThemesFolder()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ThemeManager.ThemesDirectory,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
