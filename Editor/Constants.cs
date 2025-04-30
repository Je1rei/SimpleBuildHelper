#if UNITY_EDITOR
namespace SimpleBuildHelper.Editor
{
    public static class Constants
    {
        public const string BuildOutputPathKey = "CustomBuildOutputPath";
        public const string CreateZipKey = "CreateZipArchive";
        public const string GenerateLogsKey = "GenerateLogs";
        public const string GenerateHeavyKey = "GenerateHeavyFilesLog";
        public const string GenerateUnityLogKey = "GenerateUnityLog";
        public const string GenerateBuildLogKey = "GenerateBuildLog";
        public const string HistoryKey = "BuildHistory";
        public const string SuppressDeleteKey = "SuppressDeleteConfirm";

        public const string DateFormat = "dd.MM.yyyy";
        public const string BuildsRootFolder = "Builds";

        public const string WindowTitle = "Build → Simple Helper";
        public const string MenuPath = "Tools/Build → Simple Helper";
        public const string TabBuild = "Build";
        public const string TabHistory = "History";
        public const string ButtonBuildNow = "Build Now";
        public const string LabelAdvanced = "Advanced Options";
        public const string LabelGenerateLogs = "Generate Logs";
        public const string LabelGenerateHeavy = "Generate Heavy files log";
        public const string LabelGenerateUnity = "Generate Unity Log";
        public const string LabelGenerateBuild = "Generate Build Log";
        public const string LabelCreateZip = "Create ZIP archive";

        public const string SectionProjectInfo = "Project Info";

        public const string ErrorNoScenes = "❌ No scenes selected!";
        public const string StatusBuildComplete = "Build Complete";
        public const string StatusBuildFailedFmt = "❌ Build Failed: {0} errors, {1} warnings";

        public const string DeleteConfirmTitle = "Confirm Delete";
        public const string DeleteTooltip = "Delete this build folder";

        public const int HistoryColStatusW = 20;
        public const int HistoryColDateW = 130;
        public const int HistoryColNameW = 200;
        public const int HistoryColSizeW = 80;
        public const int HistoryColTimeW = 60;
        public const int HistoryColLogsW = 40;
        public const int HistoryColOpenW = 60;
        public const int HistoryColDeleteW = 60;
    }
}
#endif