#r "pskernel_net.dll"
using PK;
unsafe
{
    PK.BODY_t body;
    double radius = 1.0;
    int nSides = 3;
    AXIS2_sf_t basisSet = new()
    {
        location = new(0, 0, 0),
        axis = new(0, 0, 1),
        ref_direction = new(1, 0, 0),
    };
    var err = PK.BODY.create_sheet_polygon(radius, nSides, &basisSet, &body);
    if (err != ERROR.code_t.no_errors)
    {
        Console.WriteLine($"create body error:{err}");
        return false;
    }
    Console.WriteLine($"create body:#{body.Value}");
    return true;
}
