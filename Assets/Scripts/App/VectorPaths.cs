using UnityEngine;

namespace Core._Common
{
    /// <summary>
    /// Folders of the moddable game content in StreamingAssets. Names match the folders on disk exactly,
    /// so they also resolve on case-sensitive file systems (Linux, macOS).
    /// </summary>
    public static class VectorPaths
    {
        public static readonly string CurrentResources = Application.streamingAssetsPath;

        public static readonly string XmlRoot = CurrentResources + "/xmlroot";

        public static readonly string XmlLevels = XmlRoot + "/levels";

        public static readonly string Models = CurrentResources + "/models";

        public static readonly string Animations = CurrentResources + "/animations";

        public static readonly string AnimationBinary = Animations + "/data";

        public static readonly string Localization = CurrentResources + "/localization";

        public static readonly string Commons = CurrentResources + "/commons";

        public static readonly string Music = CurrentResources + "/music";

        public static readonly string Sounds = CurrentResources + "/sounds";

        public static readonly string Textures = CurrentResources + "/textures";

        public static readonly string AnimatedTextures = CurrentResources + "/animatedtextures";

        public static readonly string Icons = CurrentResources + "/icons";
    }
}
