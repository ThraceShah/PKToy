#r "pskernel_net.dll"
using PK;
unsafe
{
    PK.BODY_t body;
    Span<double> size = [1.0, 1.0, 1.0];
    AXIS2_sf_t basisSet = new()
    {
        location = new(0, 0, 0),
        axis = new(0, 0, 1),
        ref_direction = new(1, 0, 0),
    };
    var err = PK.BODY.create_solid_block(size[0], size[1], size[2], &basisSet, &body);
    if (err != ERROR.code_t.no_errors)
    {
        Console.WriteLine($"create body error:ERROR.code_t.{err}");
        return false;
    }
    Console.WriteLine($"create body:#{body.Value}");
    return true;
}
