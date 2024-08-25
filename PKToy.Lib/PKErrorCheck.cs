namespace PKToy.Lib;
ref struct PKErrorCheck
{
    public PKErrorCheck(PK.ERROR.code_t err)
    {
        if (err != PK.ERROR.code_t.no_errors)
        {
            Console.WriteLine($"Error: {Enum.GetName(err)}");
        }
    }

    public static implicit operator PKErrorCheck(PK.ERROR.code_t err) => new(err);

}