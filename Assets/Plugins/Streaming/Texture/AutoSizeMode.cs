using BeauUtil;

namespace StreamingAssets {

    [LabeledEnum(false)]
    public enum AutoSizeMode {
        [Label("Disabled")]
        Disabled,

        [Label("Stretch X")]
        StretchX,
        [Label("Stretch Y")]
        StretchY,

        [Label("Fit to Parent")]
        Fit,
        [Label("Fill Parent")]
        Fill,
        [Label("Fill Parent (Clipped)")]
        FillWithClipping
    }
}