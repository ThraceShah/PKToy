namespace PKToy.Lib;

readonly ref struct PKErrorCheck
{
    private readonly PK.ERROR.code_t _err;
    public PKErrorCheck(PK.ERROR.code_t err)
    {
        _err = err;
        if (err != PK.ERROR.code_t.no_errors)
        {
            Console.WriteLine($"Error: {Enum.GetName(err)}");
        }
    }

    public readonly PK.ERROR.code_t Error => _err;

    public static implicit operator PKErrorCheck(PK.ERROR.code_t err) => new(err);

}