#r "PskernelSharp.dll"
using static parasolid;
unsafe
{
    int body;
    Span<double> size = [1.0, 1.0, 1.0];
    PK_AXIS2_sf_t basisSet = new()
    {
        location = new(0, 0, 0),
        axis = new(0, 0, 1),
        ref_direction = new(1, 0, 0),
    };
    var err = PK_BODY_create_solid_block(size[0], size[1], size[2], &basisSet, &body);
    if (err != PK_ERROR_no_errors)
    {
        Console.WriteLine($"create body error:{err}");
        return false;
    }
    Console.WriteLine($"create body:#{body}");
    return true;
}
