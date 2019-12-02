namespace Wren
{
    public enum ValueType
    {
        WREN_TYPE_BOOL = 0,
        WREN_TYPE_NUM = 1,
        WREN_TYPE_FOREIGN = 2,
        WREN_TYPE_LIST = 3,
        WREN_TYPE_NULL = 4,
        WREN_TYPE_STRING = 5,

        // The object is of a type that isn't accessible by the C API.
        WREN_TYPE_UNKNOWN = 6
    }
}
