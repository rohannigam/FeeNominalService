namespace FeeNominalService.Models;

/// <summary>
/// Represents the method used to enter card information in a transaction
/// </summary>
public enum EntryMethod
{
    /// <summary>
    /// Card details are manually entered
    /// </summary>
    KEYED,

    /// <summary>
    /// Card is swiped through a magnetic stripe reader
    /// </summary>
    SWIPED,

    /// <summary>
    /// Card is inserted (dipped) into a chip reader
    /// </summary>
    DIPPED,

    /// <summary>
    /// Card is tapped for contactless payment
    /// </summary>
    TAPPED
} 