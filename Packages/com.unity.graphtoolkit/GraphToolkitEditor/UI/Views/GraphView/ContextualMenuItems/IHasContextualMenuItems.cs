using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor.ContextualMenuItems
{
    /// <summary>
    /// Provides the list <see cref="ContextualMenuItem"/>s for an element.
    /// </summary>
    [UnityRestricted]
    interface IHasContextualMenuItems
    {
        /// <summary>
        /// The contextual menu items.
        /// </summary>
        IReadOnlyList<ContextualMenuItem> ContextualMenuItems { get; }
    }
}
