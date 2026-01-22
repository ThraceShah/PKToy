namespace PKToy.Lib;

readonly ref struct PKErrorCheck
{
    private readonly PK_ERROR_code_t _err;
    public PKErrorCheck(PK_ERROR_code_t err)
    {
        _err = err;
        if (err != PK_ERROR_code_no_errors)
        {
            // find in parasolid consts, use reflection to get the name
            
        }
    }

    public readonly PK_ERROR_code_t Error => _err;

    public static implicit operator PKErrorCheck(PK_ERROR_code_t err) => new(err);

}