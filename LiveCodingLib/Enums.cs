using System;

namespace LiveCodingLib
{
    [Flags()]
    public enum NotifyMode
    {
        Never,
        OnMention,
        Always
    }
}
