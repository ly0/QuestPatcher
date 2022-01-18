using Avalonia.Controls;
using QuestPatcher.Core.Modding;
using QuestPatcher.Core.Patching;
using QuestPatcher.Models;

namespace QuestPatcher.ViewModels.Modding
{
    public class ManageModsViewModel : ViewModelBase
    {
        public ModListViewModel ModsList { get; }

        public ModListViewModel LibrariesList { get; }

        public ProgressViewModel ProgressView { get; }

        public ManageModsViewModel(ModManager modManager, PatchingManager patchingManager, Window mainWindow, OperationLocker locker, ProgressViewModel progressView, BrowseImportManager browseManager)
        {
            ProgressView = progressView;
            ModsList = new ModListViewModel("模组", true, modManager.Mods, modManager, patchingManager, mainWindow, locker, browseManager);
            LibrariesList = new ModListViewModel("支持库", false, modManager.Libraries, modManager, patchingManager, mainWindow, locker, browseManager);
        }
    }
}
