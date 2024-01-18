namespace MauiFeed.Adapters;

public interface IVirtualListViewAdapter<T, U>
{
    int GetNumberOfSections();

    U GetSection(int sectionIndex);

    /// <summary>
    /// Gets the number of items in the specified section.
    /// </summary>
    /// <param name="sectionIndex">Section Index.</param>
    /// <returns>Number of items in section.</returns>
    int GetNumberOfItemsInSection(int sectionIndex);

    /// <summary>
    /// Gets the item at the specified section and item index.
    /// </summary>
    /// <param name="sectionIndex">Section Index.</param>
    /// <param name="itemIndex">Item Index.</param>
    /// <returns>Item.</returns>
    T GetItem(int sectionIndex, int itemIndex);

    /// <summary>
    /// Fires when the data in the view is invalidated.
    /// </summary>
    event EventHandler OnDataInvalidated;

    /// <summary>
    /// Invalidates the data in the view.
    /// </summary>
    void InvalidateData();
}