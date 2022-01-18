using Avalonia.Controls;
using QuestPatcher.Models;
using QuestPatcher.Services;
using QuestPatcher.Views;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.IO;
using ReactiveUI;
using QuestPatcher.Core;
using QuestPatcher.Core.Models;
using QuestPatcher.Core.Patching;

namespace QuestPatcher.ViewModels
{
    public class ToolsViewModel : ViewModelBase
    {
        public async void ShowTutorial()
        {
            ProcessStartInfo psi = new()
            {
                FileName = "https://bs.wgzeyu.com/oq-guide-qp/",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        public async void OpenSourceAddr()
        {
            ProcessStartInfo psi = new()
            {
                FileName = "https://github.com/MicroCBer/QuestPatcher",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        public async void OpenSourceFKAddr()
        {
            ProcessStartInfo psi = new()
            {
                FileName = "https://github.com/Lauriethefish/QuestPatcher",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        public async void WGZEYUAddr()
        {
            ProcessStartInfo psi = new()
            {
                FileName = "https://space.bilibili.com/557131",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        public async void MBAddr()
        {
            ProcessStartInfo psi = new()
            {
                FileName = "https://space.bilibili.com/413164365",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        public Config Config { get; }

        public ProgressViewModel ProgressView { get; }

        public OperationLocker Locker { get; }
        
        public ThemeManager ThemeManager { get; }

        public string AdbButtonText => _isAdbLogging ? "Stop ADB Log" : "Start ADB Log";

        private bool _isAdbLogging;

        private readonly Window _mainWindow;
        private readonly SpecialFolders _specialFolders;
        private readonly Logger _logger;
        private readonly PatchingManager _patchingManager;
        private readonly AndroidDebugBridge _debugBridge;
        private readonly QuestPatcherUIService _uiService;
        private readonly InfoDumper _dumper;

        public ToolsViewModel(Config config, ProgressViewModel progressView, OperationLocker locker, Window mainWindow, SpecialFolders specialFolders, Logger logger, PatchingManager patchingManager, AndroidDebugBridge debugBridge, QuestPatcherUIService uiService, InfoDumper dumper, ThemeManager themeManager)
        {
            Config = config;
            ProgressView = progressView;
            Locker = locker;
            ThemeManager = themeManager;

            _mainWindow = mainWindow;
            _specialFolders = specialFolders;
            _logger = logger;
            _patchingManager = patchingManager;
            _debugBridge = debugBridge;
            _uiService = uiService;
            _dumper = dumper;

            _debugBridge.StoppedLogging += (_, _) =>
            {
                _logger.Information("ADB log exited");
                _isAdbLogging = false;
                this.RaisePropertyChanged(nameof(AdbButtonText));
            };
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
                        _logger.Information("正在卸载 . . .");
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
                _logger.Error($"卸载 {ex} 失败！");
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
                _logger.Information("Done!");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to clear cache: {ex}");
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
                _logger.Information("Starting ADB log");
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
                _logger.Error($"Failed to create dump: {ex}");
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
