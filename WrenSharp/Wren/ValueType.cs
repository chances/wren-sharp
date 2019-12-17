namespace Wren
{
    /// <summary>
    /// The type of an object stored in a slot.
    /// 
    /// This is not necessarily the object's *class*, but instead its low level representation
    /// type.
    /// </summary>
    public enum ValueType
    {
        /// A boolean value represents truth or falsehood. There are two boolean literals,
        /// <c>true</c> and <c>false</c>.
        WREN_TYPE_BOOL = 0,
        /// A double-precision floating point number.
        WREN_TYPE_NUM = 1,
        /// A foreign class instance
        WREN_TYPE_FOREIGN = 2,
        /// A collection of elements identified by integer index.
        WREN_TYPE_LIST = 3,
        /// The only instance of the class Null, indicating the absence of a value.
        WREN_TYPE_NULL = 4,
        /// An array of bytes. Typically, they store characters encoded in UTF-8.
        WREN_TYPE_STRING = 5,

        /// The object is of a type that isn't accessible by the C API.
        WREN_TYPE_UNKNOWN = 6
    }
}
