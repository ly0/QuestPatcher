using Avalonia.Controls;
using QuestPatcher.Views;
using Serilog.Core;
using System;
using ReactiveUI;
using System.Diagnostics;
using QuestPatcher.Models;
using QuestPatcher.Core.Models;
using QuestPatcher.Core.Patching;
using QuestPatcher.Core;

namespace QuestPatcher.ViewModels
{
    public class PatchingViewModel : ViewModelBase
    {
        public bool IsPatchingInProgress { get => _isPatchingInProgress; set { if(_isPatchingInProgress != value) { this.RaiseAndSetIfChanged(ref _isPatchingInProgress, value); } } }
        private bool _isPatchingInProgress;

        public string PatchingStageText { get; private set; } = "";

        public Config Config { get; }

        public OperationLocker Locker { get; }

        public ProgressViewModel ProgressBarView { get; }

        public ExternalFilesDownloader FilesDownloader { get; }

        private readonly PatchingManager _patchingManager;
        private readonly Window _mainWindow;
        private readonly Logger _logger;

        public PatchingViewModel(Config config, OperationLocker locker, PatchingManager patchingManager, Window mainWindow, Logger logger, ProgressViewModel progressBarView, ExternalFilesDownloader filesDownloader)
        {
            Config = config;
            Locker = locker;
            ProgressBarView = progressBarView;
            FilesDownloader = filesDownloader;

            _patchingManager = patchingManager;
            _mainWindow = mainWindow;
            _logger = logger;

            _patchingManager.PropertyChanged += (_, args) =>
            {
                if(args.PropertyName == nameof(_patchingManager.PatchingStage))
                {
                    OnPatchingStageChange(_patchingManager.PatchingStage);
                }
            };
        }

        public async void StartPatching()
        {
            IsPatchingInProgress = true;
            Locker.StartOperation();
            try
            {
                await _patchingManager.PatchApp();
            }
            catch (Exception ex)
            {
                // Print troubleshooting information for debugging
                _logger.Error($"Patching failed!: {ex}");
                DialogBuilder builder = new()
                {
                    Title = "完蛋!出错了",
                    Text = "在给游戏打补丁的过程中出现了一个意料外的错误。",
                    HideCancelButton = true
                };
                builder.WithException(ex);

                await builder.OpenDialogue(_mainWindow);
            }   finally
            {
                IsPatchingInProgress = false;
                Locker.FinishOperation();
            }

            Debug.Assert(_patchingManager.InstalledApp != null); // Cannot get to this screen without having loaded the installed app
            if (_patchingManager.InstalledApp.IsModded)
            {
                // Display a dialogue to give the user some info about what to expect next, and to avoid them pressing restore app by mistake
                _logger.Debug("Patching completed successfully, displaying info dialogue");
                DialogBuilder builder = new()
                {
                    Title = "完工!",
                    Text = "你的游戏现在已经打完补丁啦\n现在你可以安装Mod了！" +
                    "\n\n提示：如果你在头显里面看到了一个“恢复的应用”窗口，不必惊慌，只用点击取消即可。Oculus不会因为打Mod封号，所以没啥好担心的。",
                    HideCancelButton = true
                };
                await builder.OpenDialogue(_mainWindow);
            }
        }

        /// <summary>
        /// Updates the patching stage text in the view
        /// </summary>
        /// <param name="stage">The new patching stage</param>
        private void OnPatchingStageChange(PatchingStage stage)
        {
            PatchingStageText = stage switch
            {
                PatchingStage.NotStarted => "未开始",
                PatchingStage.MovingToTemp => "将APK移动至指定位置 (1/5)",
                PatchingStage.Patching => "更改APK文件来使其支持安装mod&添加中文 (2/5)",
                PatchingStage.Signing => "给APK签名 (3/5)",
                PatchingStage.UninstallingOriginal => "卸载原有的APK (4/5)",
                PatchingStage.InstallingModded => "安装改过的APK (5/5)",
                _ => throw new NotImplementedException()
            };
            this.RaisePropertyChanged(nameof(PatchingStageText));
        }
    }
}
