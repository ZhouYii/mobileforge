namespace MobileForge.UIComponents
{
    /// <summary>
    /// Static service locator for UI infrastructure.
    /// Populated by TosUISetup (in Assembly-CSharp) and consumed by Pages (in this assembly).
    /// This breaks the circular reference: Pages can't reference Assembly-CSharp directly,
    /// but Assembly-CSharp can set these static references after creating the hosts.
    /// </summary>
    public static class UIServices
    {
        /// <summary>Global popup host for showing dialogs.</summary>
        public static MFPopupHost Popups { get; set; }

        /// <summary>Global toast host for showing notifications.</summary>
        public static MFToastHost Toasts { get; set; }

        /// <summary>Global overlay host for loading/combo overlays.</summary>
        public static MFOverlayHost Overlays { get; set; }
    }
}
